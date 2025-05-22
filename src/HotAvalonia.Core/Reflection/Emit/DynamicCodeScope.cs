using System.Reflection;
using System.Reflection.Emit;
using HotAvalonia.Helpers;

namespace HotAvalonia.Reflection.Emit;

/// <summary>
/// Represents a scope that temporarily enables dynamic code generation,
/// regardless of the current runtime configuration.
/// </summary>
internal readonly struct DynamicCodeScope : IDisposable
{
    /// <summary>
    /// A delegate function used to temporarily allow dynamic code generation
    /// even when <c>RuntimeFeature.IsDynamicCodeSupported</c> is <c>false</c>.
    /// </summary>
    private static readonly Func<IDisposable>? s_forceAllowDynamicCode = typeof(AssemblyBuilder)
        .GetStaticMethod("ForceAllowDynamicCode", []) is MethodInfo f && typeof(IDisposable).IsAssignableFrom(f.ReturnType)
            ? f.CreateDelegate<Func<IDisposable>>()
            : null;

    /// <summary>
    /// The name associated with this scope, if any.
    /// </summary>
    private readonly string? _name;

    /// <summary>
    /// The disposable object used to revert the environment to its
    /// previous state regarding support for dynamic code generation.
    /// </summary>
    private readonly IDisposable? _scope;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicCodeScope"/> struct.
    /// </summary>
    /// <param name="name">The name associated with this scope, if any.</param>
    /// <param name="scope">
    /// The disposable object used to revert the environment to its
    /// previous state regarding support for dynamic code generation.
    /// </param>
    private DynamicCodeScope(string? name, IDisposable scope)
    {
        _name = name;
        _scope = scope;

        if (name is null)
        {
            LoggingHelper.LogInfo("Entered dynamic code scope (Thread#{ThreadId})", Thread.CurrentThread.ManagedThreadId);
        }
        else
        {
            LoggingHelper.LogInfo("Entered dynamic code scope: {ScopeName} (Thread#{ThreadId})", name, Thread.CurrentThread.ManagedThreadId);
        }
    }

    /// <summary>
    /// Temporarily allows dynamic code generation even when <c>RuntimeFeature.IsDynamicCodeSupported</c> is <c>false</c>.
    /// </summary>
    /// <remarks>
    /// This is particularly useful in scenarios where the runtime can support emitting dynamic code,
    /// but a feature switch or configuration has disabled it (e.g., <c>PublishAot=true</c> during debugging).
    /// </remarks>
    /// <param name="name">An optional name to associate with the scope.</param>
    /// <param name="groupName">An optional group name used to qualify the <paramref name="name"/>.</param>
    /// <returns>
    /// A <see cref="DynamicCodeScope"/> object that, when disposed, will revert the environment
    /// to its previous state regarding support for dynamic code generation.
    /// </returns>
    public static DynamicCodeScope Create(string? name = null, string? groupName = null)
    {
        IDisposable? scope = s_forceAllowDynamicCode?.Invoke();
        if (scope is null)
            return default;

        string? scopeName = groupName is null ? name : $"{groupName}({name})";
        return new(scopeName, scope);
    }

    /// <summary>
    /// Reverts the environment to its previous state regarding support for dynamic code generation.
    /// </summary>
    public void Dispose()
    {
        if (_scope is null)
            return;

        _scope.Dispose();
        if (_name is null)
        {
            LoggingHelper.LogInfo("Exited dynamic code scope (Thread#{ThreadId})", Thread.CurrentThread.ManagedThreadId);
        }
        else
        {
            LoggingHelper.LogInfo("Exited dynamic code scope: {ScopeName} (Thread#{ThreadId})", _name, Thread.CurrentThread.ManagedThreadId);
        }
    }
}
