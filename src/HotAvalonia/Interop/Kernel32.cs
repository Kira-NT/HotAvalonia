using System.Runtime.InteropServices;
using System.Text;

namespace HotAvalonia.Interop;

internal static class Kernel32
{
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern nint GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseHandle(nint handle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CreateProcess(
        string? lpApplicationName,
        StringBuilder? lpCommandLine,
        ref SECURITY_ATTRIBUTES lpProcessAttributes,
        ref SECURITY_ATTRIBUTES lpThreadAttributes,
        [In, MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
        int dwCreationFlags,
        StringBuilder? lpEnvironment,
        string? lpCurrentDirectory,
        [In] ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation
    );

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct STARTUPINFO
    {
        public int cb;

        public string? lpReserved;

        public string? lpDesktop;

        public string? lpTitle;

        public int dwX;

        public int dwY;

        public int dwXSize;

        public int dwYSize;

        public int dwXCountChars;

        public int dwYCountChars;

        public int dwFillAttribute;

        public int dwFlags;

        public short wShowWindow;

        public short cbReserved2;

        public nint lpReserved2;

        public nint hStdInput;

        public nint hStdOutput;

        public nint hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SECURITY_ATTRIBUTES
    {
        public int nLength;

        public nint lpSecurityDescriptor;

        public int bInheritHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PROCESS_INFORMATION
    {
        public nint hProcess;

        public nint hThread;

        public int dwProcessId;

        public int dwThreadId;
    }
}
