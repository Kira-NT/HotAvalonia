using System.Reflection;
using System.Reflection.Emit;
using HotAvalonia.Helpers;

namespace HotAvalonia.Reflection.Emit;

/// <summary>
/// Provides methods for building dynamic methods.
/// </summary>
internal sealed class DynamicMethodBuilder : IDisposable
{
    /// <summary>
    /// The dynamic method being built.
    /// </summary>
    private readonly DynamicMethod _method;

    /// <summary>
    /// The dynamic code scope.
    /// </summary>
    private readonly DynamicCodeScope _scope;

    /// <summary>
    /// Defines a new dynamic method.
    /// </summary>
    /// <param name="name">The name of the dynamic method.</param>
    /// <param name="returnType">The return type of the dynamic method.</param>
    /// <param name="parameterTypes">The types of the parameters of the dynamic method.</param>
    /// <param name="owner">The logical owner of the dynamic method.</param>
    /// <param name="skipVisibility">
    /// <c>true</c> to skip JIT visibility checks on types and members accessed by the MSIL of the dynamic method;
    /// otherwise, <c>false</c>.
    /// </param>
    private DynamicMethodBuilder(string name, Type? returnType, Type[]? parameterTypes, ICustomAttributeProvider? owner, bool skipVisibility)
    {
        _ = name ?? throw new ArgumentNullException(nameof(name));
        returnType ??= typeof(void);
        parameterTypes ??= [];

        _scope = DynamicCodeScope.Create(name, nameof(DynamicMethodBuilder));
        _method = owner switch
        {
#pragma warning disable RS0030 // Do not use banned APIs
            Type ownerType => new(name, returnType, parameterTypes, ownerType, skipVisibility),
            Module ownerModule => new(name, returnType, parameterTypes, ownerModule, skipVisibility),
            null => new(name, returnType, parameterTypes, skipVisibility),
            _ => throw new ArgumentException(message: null, nameof(owner)),
#pragma warning restore RS0030 // Do not use banned APIs
        };
    }

    /// <inheritdoc cref="DynamicMethodBuilder(string, Type, Type[], ICustomAttributeProvider, bool)"/>
    public DynamicMethodBuilder(string name, Type? returnType, Type[]? parameterTypes)
        : this(name, returnType, parameterTypes, owner: (ICustomAttributeProvider?)null, skipVisibility: false)
    {
    }

    /// <inheritdoc cref="DynamicMethodBuilder(string, Type, Type[], ICustomAttributeProvider, bool)"/>
    public DynamicMethodBuilder(string name, Type? returnType, Type[]? parameterTypes, bool skipVisibility)
        : this(name, returnType, parameterTypes, owner: (ICustomAttributeProvider?)null, skipVisibility)
    {
    }

    /// <inheritdoc cref="DynamicMethodBuilder(string, Type, Type[], ICustomAttributeProvider, bool)"/>
    public DynamicMethodBuilder(string name, Type? returnType, Type[]? parameterTypes, Type? owner)
        : this(name, returnType, parameterTypes, (ICustomAttributeProvider?)owner, skipVisibility: false)
    {
    }

    /// <inheritdoc cref="DynamicMethodBuilder(string, Type, Type[], ICustomAttributeProvider, bool)"/>
    public DynamicMethodBuilder(string name, Type? returnType, Type[]? parameterTypes, Type? owner, bool skipVisibility)
        : this(name, returnType, parameterTypes, (ICustomAttributeProvider?)owner, skipVisibility)
    {
    }

    /// <inheritdoc cref="DynamicMethodBuilder(string, Type, Type[], ICustomAttributeProvider, bool)"/>
    public DynamicMethodBuilder(string name, Type? returnType, Type[]? parameterTypes, Module? owner)
        : this(name, returnType, parameterTypes, (ICustomAttributeProvider?)owner, skipVisibility: false)
    {
    }

    /// <inheritdoc cref="DynamicMethodBuilder(string, Type, Type[], ICustomAttributeProvider, bool)"/>
    public DynamicMethodBuilder(string name, Type? returnType, Type[]? parameterTypes, Module? owner, bool skipVisibility)
        : this(name, returnType, parameterTypes, (ICustomAttributeProvider?)owner, skipVisibility)
    {
    }

    /// <inheritdoc cref="DynamicMethod.DefineParameter(int, ParameterAttributes, string?)"/>
    public void DefineParameter(int position, ParameterAttributes attributes, string? parameterName = null)
        => _method.DefineParameter(position, attributes, parameterName);

    /// <inheritdoc cref="DynamicMethod.GetILGenerator()"/>
    public ILGenerator GetILGenerator()
        => _method.GetILGenerator();

    /// <inheritdoc cref="DynamicMethod.GetILGenerator(int)"/>
    public ILGenerator GetILGenerator(int streamSize)
        => _method.GetILGenerator(streamSize);

    /// <inheritdoc cref="DynamicMethod.CreateDelegate(Type)"/>
    public Delegate CreateDelegate(Type delegateType)
        => CreateDelegate(delegateType, target: null);

    /// <inheritdoc cref="DynamicMethod.CreateDelegate(Type, object?)"/>
    public Delegate CreateDelegate(Type delegateType, object? target)
    {
        Delegate func = _method.CreateDelegate(delegateType, target);
        LoggingHelper.LogInfo("Created dynamic method: {Method}", _method);
        return func;
    }

    /// <inheritdoc cref="DynamicMethod.CreateDelegate(Type, object?)"/>
    /// <typeparam name="TDelegate">The type of the delegate to create.</typeparam>
    public TDelegate CreateDelegate<TDelegate>(object? target = null) where TDelegate : Delegate
        => (TDelegate)CreateDelegate(typeof(TDelegate), target);

    /// <inheritdoc cref="DynamicMethod.ToString()"/>
    public override string ToString()
        => _method.ToString();

    /// <inheritdoc/>
    public void Dispose()
        => _scope.Dispose();
}
