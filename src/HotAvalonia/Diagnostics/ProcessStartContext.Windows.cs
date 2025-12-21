using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using HotAvalonia.Interop;
using Microsoft.Win32.SafeHandles;

namespace HotAvalonia.Diagnostics;

internal sealed partial class ProcessStartContext
{
    /// <summary>
    /// Starts a background process on Windows without waiting for its completion.
    /// </summary>
    /// <param name="fileName">The name of the executable to start.</param>
    /// <param name="arguments">The command-line arguments to pass to the process.</param>
    /// <param name="environmentVariables">The environment variables to apply to the process.</param>
    private static void StartWindowsBackgroundProcess(string fileName, IEnumerable<string?> arguments, IEnumerable<KeyValuePair<string, string?>> environmentVariables)
    {
        // If you search the web for how to start a background process on Windows, you will
        // quickly end up finding the dreaded `cmd.exe /C START "" /B foo` "solution" that
        // absolutely EVERYBODY recommends in this situation. And uh oh, spagwettios!
        // People on the internet don't know what they're talking about. Who would have thunk?
        //
        // `START /B` on its own only starts a process that is considered "background" within
        // the current session, preventing that session from exiting even if there are no more
        // tasks to perform. And if that session is killed, so is the "background" process,
        // since it never actually detaches from it. That's clearly not what we want.
        //
        // To start a new session (or rather, a process group, as it's called in the docs)
        // completely detached from its parent, we have a few options:
        // 1) We could call CreateProcessW from kernel32.dll with dwCreationFlags set to
        //    CREATE_NEW_PROCESS_GROUP | DETACHED_PROCESS.
        //    This works well in principle, however, this approach does not preallocate
        //    a console window. As a result, if (or when) something in that process group
        //    eventually requests a console, one will suddenly pop up, jumpscaring the user,
        //    and we won't be able to hide it, since we stopped monitoring the process long
        //    time ago.
        // 2) Instead, we can set dwCreationFlags to CREATE_NEW_CONSOLE (0x00000010).
        //    This appears to achieve the same effect as 'CREATE_NEW_PROCESS_GROUP |
        //    DETACHED_PROCESS', but with the added benefit of preallocating a console window
        //    so we can hide it preemptively. This is the approach we're going with here.
        // In addition, to remain completely independent of the parent process, it's a good
        // idea not to inherit any of its handles, i.e., set bInheritHandles to FALSE.
        //
        // That about covers what's happening here. Also note that the process we start won't
        // immediately exit, as it happens on the Unix side of things.
        // This isn't a "jumpstart" process that kicks everything in motion and then exits,
        // leaving the real program running in the background. Instead, this process **is**
        // our program. However, at least in theory, it should be fully detached from its
        // parent and able to outlive its process group.
        //
        // Unfortunately, this also means we can't use managed APIs to start such a process.
        // ProcessStartInfo doesn't provide a way to specify additional dwCreationFlags
        // beyond CREATE_NO_WINDOW and CREATE_NEW_PROCESS_GROUP - at least not at the moment.
        // As a result, we have to implement the entire process-creation routine FROM SCRATCH
        // using P/Invoke, which is why this file is a couple of hundred lines longer than
        // it really ought to be.
        ProcessStartInfo info = CreateWindowsBackgroundProcessStartInfo(fileName, arguments, environmentVariables);
        Process? proxyProcess = StartWindowsProcess(info, creationFlags: 0x00000010, inheritHandles: false);
        if (proxyProcess is null or { HasExited: true, ExitCode: not 0 })
            ThrowProcessNotStartedException(proxyProcess?.ExitCode ?? 1, fileName, info.WorkingDirectory);
    }

    /// <summary>
    /// Creates a <see cref="ProcessStartInfo"/> configured to start
    /// a background process on Windows.
    /// </summary>
    /// <param name="fileName">The name of the executable to start.</param>
    /// <param name="arguments">The command-line arguments to pass to the process.</param>
    /// <param name="environmentVariables">The environment variables to apply to the process.</param>
    /// <returns>A configured <see cref="ProcessStartInfo"/> instance.</returns>
    private static ProcessStartInfo CreateWindowsBackgroundProcessStartInfo(string fileName, IEnumerable<string?> arguments, IEnumerable<KeyValuePair<string, string?>> environmentVariables)
    {
        ProcessStartInfo info = CreateProcessStartInfo("cmd.exe", ["/C", "START", "", "/B", fileName, .. arguments], environmentVariables);
        info.WindowStyle = ProcessWindowStyle.Hidden;
        info.RedirectStandardInput = info.RedirectStandardOutput = info.RedirectStandardError = false;
        return info;
    }

    /// <summary>
    /// Builds a command-line string from the specified <see cref="ProcessStartInfo"/>.
    /// </summary>
    /// <param name="startInfo">The process start information.</param>
    /// <returns>A <see cref="StringBuilder"/> containing the combined command line.</returns>
    private static StringBuilder BuildCommandLine(ProcessStartInfo startInfo)
    {
        string fileName = startInfo.FileName.Trim();
        string arguments = startInfo.Arguments;
        StringBuilder commandLine = new(fileName.Length + arguments.Length + 4);
        if (fileName.StartsWith("\"") && fileName.EndsWith("\""))
        {
            commandLine.Append(fileName);
        }
        else
        {
            commandLine.Append('"').Append(fileName).Append('"');
        }
        if (!string.IsNullOrEmpty(arguments))
        {
            commandLine.Append(' ').Append(arguments);
        }
        return commandLine;
    }

    /// <summary>
    /// Builds a native environment variable block suitable for process creation APIs.
    /// </summary>
    /// <param name="env">A dictionary containing environment variable names and values.</param>
    /// <returns>
    /// A <see cref="StringBuilder"/> containing a null-terminated sequence of
    /// null-terminated 'key=value' entries.
    /// </returns>
    private static StringBuilder BuildEnvironmentVariablesBlock(IDictionary<string, string?> env)
    {
        string[] keys = new string[env.Count];
        env.Keys.CopyTo(keys, 0);
        Array.Sort(keys, StringComparer.OrdinalIgnoreCase);

        StringBuilder result = new(8 * keys.Length);
        foreach (string key in keys)
        {
            if (env[key] is string value)
                result.Append(key).Append('=').Append(value).Append('\0');
        }

        return result.Append('\0');
    }

    /// <summary>
    /// Starts a process on Windows using <c>kernel32.dll</c>'s <c>CreateProcessW</c> method.
    /// </summary>
    /// <remarks>
    /// https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw
    /// </remarks>
    /// <param name="startInfo">The process start information used to configure the new process.</param>
    /// <param name="creationFlags">The flags that control the priority class and the creation of the process.</param>
    /// <param name="inheritHandles"><c>true</c> to inherit handles from the calling process; otherwise, <c>false</c>.</param>
    /// <returns>
    /// A <see cref="Process"/> instance representing the started process,
    /// or <c>null</c> if process creation fails.
    /// </returns>
    private static Process? StartWindowsProcess(ProcessStartInfo startInfo, int creationFlags = 0, bool inheritHandles = true)
    {
        if (startInfo.UseShellExecute)
            return Process.Start(startInfo);

        if (startInfo.UserName is { Length: > 0 })
            throw new NotImplementedException();

        Process process = new() { StartInfo = startInfo };
        StringBuilder commandLine = BuildCommandLine(startInfo);
        StringBuilder environmentVariables = BuildEnvironmentVariablesBlock(startInfo.Environment);
        string? workingDirectory = startInfo.WorkingDirectory;
        if (string.IsNullOrEmpty(workingDirectory))
            workingDirectory = Directory.GetCurrentDirectory();

        Kernel32.STARTUPINFO startupInfo = new() { cb = Unsafe.SizeOf<Kernel32.STARTUPINFO>() };
        Kernel32.PROCESS_INFORMATION processInfo = default;
        Kernel32.SECURITY_ATTRIBUTES securityAttributes = default;

        if (startInfo.WindowStyle != ProcessWindowStyle.Normal)
        {
            startupInfo.wShowWindow = startInfo.WindowStyle switch
            {
                ProcessWindowStyle.Hidden => 0,     // SW_HIDE
                ProcessWindowStyle.Minimized => 2,  // SW_SHOWMINIMIZED
                ProcessWindowStyle.Maximized => 3,  // SW_SHOWMAXIMIZED
                _ => 1,                             // SW_SHOWNORMAL
            };
            startupInfo.dwFlags |= 0x00000001;      // STARTF_USESHOWWINDOW
        }
        creationFlags |= 0x00000400;                // CREATE_UNICODE_ENVIRONMENT
        if (startInfo.CreateNoWindow)
            creationFlags |= 0x08000000;            // CREATE_NO_WINDOW

        BindingFlags staticMember = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        BindingFlags instanceMember = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        SafeProcessHandle? processHandle = null;
        object processLock = typeof(Process).GetFields(staticMember).FirstOrDefault(x => x.Name.EndsWith("Lock", StringComparison.Ordinal))?.GetValue(null) ?? new object();

        SafeFileHandle? parentInputPipeHandle = null;
        SafeFileHandle? childInputPipeHandle = null;
        SafeFileHandle? parentOutputPipeHandle = null;
        SafeFileHandle? childOutputPipeHandle = null;
        SafeFileHandle? parentErrorPipeHandle = null;
        SafeFileHandle? childErrorPipeHandle = null;

        lock (processLock)
        {
            try
            {
                if (startInfo.RedirectStandardInput || startInfo.RedirectStandardOutput || startInfo.RedirectStandardError)
                {
                    Type[] createPipeArgs = [typeof(SafeFileHandle).MakeByRefType(), typeof(SafeFileHandle).MakeByRefType(), typeof(bool)];
                    MethodInfo createPipeMethod = typeof(Process).GetMethod("CreatePipe", instanceMember | staticMember, null, createPipeArgs, null) ?? throw new MissingMethodException(typeof(Process).FullName, "CreatePipe");
                    CreatePipeDelegate createPipe = (CreatePipeDelegate)createPipeMethod.CreateDelegate(typeof(CreatePipeDelegate), createPipeMethod.IsStatic ? null : process);

                    if (startInfo.RedirectStandardInput)
                    {
                        createPipe(out parentInputPipeHandle, out childInputPipeHandle, true);
                    }
                    else
                    {
                        childInputPipeHandle = new SafeFileHandle(Kernel32.GetStdHandle(-10), false);
                    }

                    if (startInfo.RedirectStandardOutput)
                    {
                        createPipe(out parentOutputPipeHandle, out childOutputPipeHandle, false);
                    }
                    else
                    {
                        childOutputPipeHandle = new SafeFileHandle(Kernel32.GetStdHandle(-11), false);
                    }

                    if (startInfo.RedirectStandardError)
                    {
                        createPipe(out parentErrorPipeHandle, out childErrorPipeHandle, false);
                    }
                    else
                    {
                        childErrorPipeHandle = new SafeFileHandle(Kernel32.GetStdHandle(-12), false);
                    }

                    startupInfo.hStdInput = childInputPipeHandle.DangerousGetHandle();
                    startupInfo.hStdOutput = childOutputPipeHandle.DangerousGetHandle();
                    startupInfo.hStdError = childErrorPipeHandle.DangerousGetHandle();
                    startupInfo.dwFlags |= 0x00000100; // STARTF_USESTDHANDLES
                }

                int errorCode = 0;
                bool success = Kernel32.CreateProcess(
                    null,
                    commandLine,
                    ref securityAttributes,
                    ref securityAttributes,
                    inheritHandles,
                    creationFlags,
                    environmentVariables,
                    workingDirectory,
                    ref startupInfo,
                    out processInfo
                );
                if (!success)
                    errorCode = Marshal.GetLastWin32Error();

                if (processInfo.hProcess is not (-1 or 0))
                    processHandle = new SafeProcessHandle(processInfo.hProcess, true);

                if (processInfo.hThread is not (-1 or 0))
                    Kernel32.CloseHandle(processInfo.hThread);

                if (!success)
                    ThrowProcessNotStartedException(errorCode, startInfo.FileName, workingDirectory);
            }
            catch
            {
                parentInputPipeHandle?.Dispose();
                parentOutputPipeHandle?.Dispose();
                parentErrorPipeHandle?.Dispose();
                processHandle?.Dispose();
                throw;
            }
            finally
            {
                childInputPipeHandle?.Dispose();
                childOutputPipeHandle?.Dispose();
                childErrorPipeHandle?.Dispose();
            }
        }

        if (startInfo.RedirectStandardInput)
        {
            Encoding enc = Console.InputEncoding;
            StreamWriter writer = new(new FileStream(parentInputPipeHandle!, FileAccess.Write, 4096, false), enc, 4096) { AutoFlush = true };
            (typeof(Process).GetField("_standardInput") ?? typeof(Process).GetField("standardInput"))?.SetValue(process, writer);
        }
        if (startInfo.RedirectStandardOutput)
        {
            Encoding enc = startInfo.StandardOutputEncoding ?? Console.OutputEncoding;
            StreamReader reader = new(new FileStream(parentOutputPipeHandle!, FileAccess.Read, 4096, false), enc, true, 4096);
            (typeof(Process).GetField("_standardOutput") ?? typeof(Process).GetField("standardOutput"))?.SetValue(process, reader);
        }
        if (startInfo.RedirectStandardError)
        {
            Encoding enc = startInfo.StandardErrorEncoding ?? Console.OutputEncoding;
            StreamReader reader = new(new FileStream(parentErrorPipeHandle!, FileAccess.Read, 4096, false), enc, true, 4096);
            (typeof(Process).GetField("_standardError") ?? typeof(Process).GetField("standardError"))?.SetValue(process, reader);
        }

        if (processHandle is not { IsInvalid: false })
        {
            processHandle?.Dispose();
            return null;
        }

        typeof(Process).GetMethod("SetProcessHandle", instanceMember, null, [typeof(SafeProcessHandle)], null)?.Invoke(process, [processHandle]);
        typeof(Process).GetMethod("SetProcessId", instanceMember, null, [typeof(int)], null)?.Invoke(process, [processInfo.dwProcessId]);
        return process;
    }

    /// <summary>
    /// Throws a <see cref="Win32Exception"/> indicating that a process could not be started.
    /// </summary>
    /// <param name="errorCode">The error code associated with the failure to start the process.</param>
    /// <param name="fileName">The name or path of the executable that failed to start.</param>
    /// <param name="workingDirectory">The working directory that was specified when attempting to start the process.</param>
    /// <exception cref="Win32Exception"/>
    [DoesNotReturn]
    private static void ThrowProcessNotStartedException(int errorCode, string fileName, string? workingDirectory)
    {
        if (string.IsNullOrEmpty(workingDirectory))
            workingDirectory = Directory.GetCurrentDirectory();

        throw new Win32Exception(errorCode, $"An error occurred trying to start process '{fileName}' with working directory '{workingDirectory}'.");
    }
}

/// <summary>
/// Creates a pipe used for process I/O redirection.
/// </summary>
/// <param name="parentHandle">When this method returns, contains the handle used by the parent process.</param>
/// <param name="childHandle">When this method returns, contains the handle used by the child process.</param>
/// <param name="parentInputs"><c>true</c> if the parent process provides input to the pipe; otherwise, <c>false</c>.</param>
file delegate void CreatePipeDelegate(out SafeFileHandle parentHandle, out SafeFileHandle childHandle, bool parentInputs);
