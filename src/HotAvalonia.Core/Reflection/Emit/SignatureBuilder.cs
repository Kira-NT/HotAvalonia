using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

namespace HotAvalonia.Reflection.Emit;

/// <summary>
/// Provides methods for building signatures.
/// </summary>
internal sealed class SignatureBuilder
{
    /// <summary>
    /// The underlying <see cref="SignatureHelper"/> used to build the signature.
    /// </summary>
    private readonly SignatureHelper _signature;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignatureBuilder"/> class.
    /// </summary>
    /// <param name="signature">The <see cref="SignatureHelper"/> used to build the signature.</param>
    private SignatureBuilder(SignatureHelper signature)
    {
        _signature = signature;
    }

    /// <summary>
    /// Implicitly converts a <see cref="SignatureBuilder"/> to its corresponding <see cref="SignatureHelper"/>.
    /// </summary>
    /// <param name="signatureBuilder">The <see cref="SignatureBuilder"/> to convert.</param>
    /// <returns>A <see cref="SignatureHelper"/> representing the signature.</returns>
    [return: NotNullIfNotNull(nameof(signatureBuilder))]
    public static implicit operator SignatureHelper?(SignatureBuilder? signatureBuilder)
        => signatureBuilder?._signature;

#pragma warning disable RS0030 // Do not use banned APIs
    /// <summary>
    /// Returns a signature builder for a method given the method's calling convention and return type.
    /// </summary>
    /// <param name="callingConvention">The calling convention of the method.</param>
    /// <param name="returnType">The return type of the method.</param>
    /// <returns>The <see cref="SignatureBuilder"/> object for a method.</returns>
    public static SignatureBuilder CreateMethodSignature(CallingConventions callingConvention, Type? returnType)
    {
        using DynamicCodeScope scope = DynamicCodeScope.Create(nameof(CreateMethodSignature), nameof(SignatureBuilder));
        return new(SignatureHelper.GetMethodSigHelper(callingConvention, returnType));
    }

    /// <summary>
    /// Returns a signature builder for a method given the method's module, calling convention, and return type.
    /// </summary>
    /// <param name="module">The <see cref="ModuleBuilder"/> that contains the method for which the <see cref="SignatureBuilder"/> is requested.</param>
    /// <param name="callingConvention">The calling convention of the method.</param>
    /// <param name="returnType">The return type of the method.</param>
    /// <returns>The <see cref="SignatureBuilder"/> object for a method.</returns>
    public static SignatureBuilder CreateMethodSignature(Module module, CallingConventions callingConvention, Type? returnType)
    {
        using DynamicCodeScope scope = DynamicCodeScope.Create(nameof(CreateMethodSignature), nameof(SignatureBuilder));
        return new(SignatureHelper.GetMethodSigHelper(module, callingConvention, returnType));
    }

    /// <summary>
    /// Returns a signature builder for a method with a standard calling convention, given the method's module, return type, and argument types.
    /// </summary>
    /// <param name="module">The <see cref="ModuleBuilder"/> that contains the method for which the <see cref="SignatureBuilder"/> is requested.</param>
    /// <param name="returnType">The return type of the method.</param>
    /// <param name="parameterTypes">The types of the arguments of the method.</param>
    /// <returns>The <see cref="SignatureBuilder"/> object for a method.</returns>
    public static SignatureBuilder CreateMethodSignature(Module module, Type? returnType, Type[]? parameterTypes)
    {
        using DynamicCodeScope scope = DynamicCodeScope.Create(nameof(CreateMethodSignature), nameof(SignatureBuilder));
        return new(SignatureHelper.GetMethodSigHelper(module, returnType, parameterTypes));
    }

    /// <summary>
    /// Returns a signature builder for a local variable.
    /// </summary>
    /// <returns>The <see cref="SignatureBuilder"/> object for a local variable.</returns>
    public static SignatureBuilder CreateLocalSignature()
    {
        using DynamicCodeScope scope = DynamicCodeScope.Create(nameof(CreateLocalSignature), nameof(SignatureBuilder));
        return new(SignatureHelper.GetLocalVarSigHelper());
    }

    /// <summary>
    /// Returns a signature builder for a local variable.
    /// </summary>
    /// <param name="module">The <see cref="ModuleBuilder"/> that contains the local variable for which the <see cref="SignatureBuilder"/> is requested.</param>
    /// <returns>The <see cref="SignatureBuilder"/> object for a local variable.</returns>
    public static SignatureBuilder CreateLocalSignature(Module module)
    {
        using DynamicCodeScope scope = DynamicCodeScope.Create(nameof(CreateLocalSignature), nameof(SignatureBuilder));
        return new(SignatureHelper.GetLocalVarSigHelper(module));
    }

    /// <summary>
    /// Returns a signature builder for a field.
    /// </summary>
    /// <param name="module">The <see cref="ModuleBuilder"/> that contains the field for which the <see cref="SignatureBuilder"/> is requested.</param>
    /// <returns>The <see cref="SignatureBuilder"/> object for a field.</returns>
    public static SignatureBuilder CreateFieldSignature(Module module)
    {
        using DynamicCodeScope scope = DynamicCodeScope.Create(nameof(CreateFieldSignature), nameof(SignatureBuilder));
        return new(SignatureHelper.GetFieldSigHelper(module));
    }

    /// <inheritdoc cref="CreatePropertySignature(Module, CallingConventions, Type, Type[], Type[], Type[], Type[][], Type[][])"/>
    public static SignatureBuilder CreatePropertySignature(Module module, Type returnType, Type[]? parameterTypes)
    {
        using DynamicCodeScope scope = DynamicCodeScope.Create(nameof(CreatePropertySignature), nameof(SignatureBuilder));
        return new(SignatureHelper.GetPropertySigHelper(module, returnType, parameterTypes));
    }

    /// <inheritdoc cref="CreatePropertySignature(Module, CallingConventions, Type, Type[], Type[], Type[], Type[][], Type[][])"/>
    public static SignatureBuilder CreatePropertySignature(Module module, Type returnType, Type[]? requiredReturnTypeCustomModifiers, Type[]? optionalReturnTypeCustomModifiers, Type[]? parameterTypes, Type[][]? requiredParameterTypeCustomModifiers, Type[][]? optionalParameterTypeCustomModifiers)
    {
        using DynamicCodeScope scope = DynamicCodeScope.Create(nameof(CreatePropertySignature), nameof(SignatureBuilder));
        return new(SignatureHelper.GetPropertySigHelper(module, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers));
    }

    /// <summary>
    /// Returns a signature builder for a property.
    /// </summary>
    /// <param name="module">The <see cref="ModuleBuilder"/> that contains the property for which the <see cref="SignatureBuilder"/> is requested.</param>
    /// <param name="callingConvention">The calling convention of the property accessors.</param>
    /// <param name="returnType">The property type.</param>
    /// <param name="requiredReturnTypeCustomModifiers">
    /// An array of types representing the required custom modifiers for the return type,
    /// such as <see cref="System.Runtime.CompilerServices.IsConst"/> or <see cref="System.Runtime.CompilerServices.IsBoxed"/>.
    /// </param>
    /// <param name="optionalReturnTypeCustomModifiers">
    /// An array of types representing the optional custom modifiers for the return type,
    /// such as <see cref="System.Runtime.CompilerServices.IsConst"/> or <see cref="System.Runtime.CompilerServices.IsBoxed"/>.
    /// </param>
    /// <param name="parameterTypes">The types of the property's arguments.</param>
    /// <param name="requiredParameterTypeCustomModifiers">
    /// An array of arrays of types.
    /// Each array of types represents the required custom modifiers for the corresponding argument of the property.
    /// </param>
    /// <param name="optionalParameterTypeCustomModifiers">
    /// An array of arrays of types.
    /// Each array of types represents the optional custom modifiers for the corresponding argument of the property.
    /// </param>
    /// <returns>The <see cref="SignatureBuilder"/> object for a property.</returns>
    public static SignatureBuilder CreatePropertySignature(Module module, CallingConventions callingConvention, Type returnType, Type[]? requiredReturnTypeCustomModifiers, Type[]? optionalReturnTypeCustomModifiers, Type[]? parameterTypes, Type[][]? requiredParameterTypeCustomModifiers, Type[][]? optionalParameterTypeCustomModifiers)
    {
        using DynamicCodeScope scope = DynamicCodeScope.Create(nameof(CreatePropertySignature), nameof(SignatureBuilder));
        return new(SignatureHelper.GetPropertySigHelper(module, callingConvention, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers));
    }
#pragma warning restore RS0030 // Do not use banned APIs

    /// <inheritdoc cref="SignatureHelper.AddArgument(Type)"/>
    /// <returns>This instance.</returns>
    public SignatureBuilder AddArgument(Type clsArgument)
    {
        _signature.AddArgument(clsArgument);
        return this;
    }

    /// <inheritdoc cref="SignatureHelper.AddArgument(Type, bool)"/>
    /// <returns>This instance.</returns>
    public SignatureBuilder AddArgument(Type argument, bool pinned)
    {
        _signature.AddArgument(argument, pinned);
        return this;
    }

    /// <inheritdoc cref="SignatureHelper.AddArgument(Type, Type[], Type[])"/>
    /// <returns>This instance.</returns>
    public SignatureBuilder AddArgument(Type argument, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers)
    {
        _signature.AddArgument(argument, requiredCustomModifiers, optionalCustomModifiers);
        return this;
    }

    /// <inheritdoc cref="SignatureHelper.AddArguments(Type[], Type[][], Type[][])"/>
    /// <returns>This instance.</returns>
    public SignatureBuilder AddArguments(Type[] arguments, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers)
    {
        _signature.AddArguments(arguments, requiredCustomModifiers, optionalCustomModifiers);
        return this;
    }

    /// <summary>
    /// Gets the binary serialized representation of this instance.
    /// </summary>
    public byte[] Signature
        => _signature.GetSignature();

    /// <summary>
    /// Returns the underlying <see cref="SignatureHelper"/> used to build the signature.
    /// </summary>
    /// <returns>The <see cref="SignatureHelper"/> used to build the signature.</returns>
    public SignatureHelper AsSignatureHelper()
        => _signature;

    /// <inheritdoc/>
    public override int GetHashCode()
        => _signature.GetHashCode();

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is SignatureBuilder other && _signature.Equals(other._signature);

    /// <summary>
    /// Returns a string representing the signature arguments.
    /// </summary>
    /// <returns>A string representing the arguments of this signature.</returns>
    public override string ToString()
        => _signature.ToString();
}
