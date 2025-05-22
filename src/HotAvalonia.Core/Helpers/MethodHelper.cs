using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using HotAvalonia.IO;

namespace HotAvalonia.Helpers;

/// <summary>
/// Provides utility methods for obtaining information about methods.
/// </summary>
internal static class MethodHelper
{
    /// <summary>
    /// Creates a delegate of type <typeparamref name="TDelegate"/>
    /// that represents the provided static or instance method.
    /// </summary>
    /// <typeparam name="TDelegate">The type of the delegate to create.</typeparam>
    /// <param name="method">The method the delegate is to represent.</param>
    /// <param name="target">
    /// The object to which the delegate is bound, or
    /// <c>null</c> to treat method as <c>static</c>.
    /// </param>
    /// <returns>A delegate of the specified type that represents the provided method.</returns>
    public static TDelegate CreateDelegate<TDelegate>(this MethodInfo method, object? target = null)
        where TDelegate : Delegate
        => (TDelegate)method.CreateDelegate(typeof(TDelegate), target);

    /// <summary>
    /// Creates a delegate of type <typeparamref name="TDelegate"/>
    /// that represents the provided static or instance method.
    /// </summary>
    /// <typeparam name="TDelegate">The type of the delegate to create.</typeparam>
    /// <inheritdoc cref="CreateUnsafeDelegate(MethodBase, Type, object?)"/>
    public static TDelegate CreateUnsafeDelegate<TDelegate>(this MethodBase method, object? target = null)
        where TDelegate : Delegate
        => (TDelegate)CreateUnsafeDelegate(method, typeof(TDelegate), target);

    /// <summary>
    /// Creates a delegate of the specified type that represents
    /// the provided static or instance method.
    /// </summary>
    /// <remarks>This method does not perform any safety checks.</remarks>
    /// <param name="method">The method the delegate is to represent.</param>
    /// <param name="delegateType">The <see cref="Type"/> of delegate to create.</param>
    /// <param name="target">
    /// The object to which the delegate is bound, or
    /// <c>null</c> to treat method as <c>static</c>.
    /// </param>
    /// <returns>A delegate of the specified type that represents the provided method.</returns>
    public static Delegate CreateUnsafeDelegate(this MethodBase method, Type delegateType, object? target = null)
    {
        RuntimeMethodHandle handle = method.MethodHandle;
        RuntimeHelpers.PrepareMethod(handle);
        nint ptr = handle.GetFunctionPointer();
        return (Delegate)Activator.CreateInstance(delegateType, target, ptr)!;
    }

    /// <summary>
    /// Gets the type of the instance for instance methods or <c>null</c> for static methods.
    /// </summary>
    /// <param name="method">The method for which to get the instance type.</param>
    /// <returns>
    /// The declaring type of the method if it's an instance method;
    /// otherwise, <c>null</c> for static methods.
    /// </returns>
    public static Type? GetThisType(this MethodBase method)
    {
        if (method.IsStatic)
            return null;

        Type declaringType = method.DeclaringType;
        return declaringType.IsValueType ? declaringType.MakeByRefType() : declaringType;
    }

    /// <summary>
    /// Gets the return type of the method.
    /// </summary>
    /// <param name="method">The method for which to get the return type.</param>
    /// <returns>The return type of the method.</returns>
    public static Type GetReturnType(this MethodBase method)
        => method is MethodInfo methodInfo ? methodInfo.ReturnType : typeof(void);

    /// <summary>
    /// Gets an array of the parameter types for the method.
    /// </summary>
    /// <param name="method">The method for which to get the parameter types.</param>
    /// <returns>An array of the parameter types of the method.</returns>
    public static Type[] GetParameterTypes(this MethodBase method)
        => Array.ConvertAll(method.GetParameters(), static x => x.ParameterType);

    /// <summary>
    /// Gets the delegate type that matches the method's signature.
    /// </summary>
    /// <param name="method">The method for which to get the delegate type.</param>
    /// <returns>A delegate type that matches the method's signature.</returns>
    public static Type GetDelegateType(this MethodBase method)
        => Expression.GetDelegateType([.. method.GetParameterTypes(), method.GetReturnType()]);

    /// <summary>
    /// Gets the delegate type that matches the method's signature,
    /// including the instance type for instance methods.
    /// </summary>
    /// <param name="method">The method for which to get the static delegate type.</param>
    /// <returns>
    /// A delegate type that matches the method's signature,
    /// including the instance type for instance methods.
    /// </returns>
    public static Type GetStaticDelegateType(this MethodBase method)
        => Expression.GetDelegateType([
            .. method.GetThisType() is Type thisType ? [thisType] : Type.EmptyTypes,
            .. method.GetParameterTypes(),
            method.GetReturnType()
        ]);

    /// <summary>
    /// Validates the compatibility of a method's parameter types with a given signature.
    /// </summary>
    /// <param name="signature">An array of types that define the expected method parameter types.</param>
    /// <param name="method">The method under examination.</param>
    /// <returns>A boolean value indicating whether the method's parameter types align with the provided signature.</returns>
    public static bool IsSignatureAssignableFrom(Type[] signature, MethodBase method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length != signature.Length)
            return false;

        for (int i = 0; i < signature.Length; ++i)
        {
            if (!signature[i].IsAssignableFrom(parameters[i].ParameterType))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Obtains the function pointer for a given method.
    /// </summary>
    /// <param name="method">The method for which to obtain the function pointer.</param>
    /// <returns>The function pointer of the method.</returns>
    public static nint GetFunctionPointer(this MethodBase method)
    {
        RuntimeHelpers.PrepareMethod(method.MethodHandle);

        return method.MethodHandle.GetFunctionPointer();
    }

    /// <summary>
    /// Gets the file path of the source code file where the specified method is defined.
    /// </summary>
    /// <param name="method">The method to get the file path for.</param>
    /// <param name="fileSystem">The file system where the method's assembly resides.</param>
    /// <returns>
    /// The file path of the source code file where the specified method is defined,
    /// or <c>null</c> if the path cannot be determined.
    /// </returns>
    public static unsafe string? GetFilePath(this MethodBase method, IFileSystem fileSystem)
    {
        if (method is not { DeclaringType: { FullName.Length: > 0, Assembly.IsDynamic: false } })
            return null;

        string? location = method.DeclaringType.Assembly.Location;
        string? pdbLocation = fileSystem.ChangeExtension(location, ".pdb");
        if (!fileSystem.FileExists(pdbLocation))
            return null;

        try
        {
            using Stream pdbStream = fileSystem.OpenRead(pdbLocation);
            using MetadataReaderProvider pdbReaderProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
            MetadataReader pdbReader = pdbReaderProvider.GetMetadataReader();

            int rowId = method.MetadataToken & 0x00FFFFFF;
            MethodDebugInformationHandle debugHandle = *(MethodDebugInformationHandle*)&rowId;
            MethodDebugInformation debugInfo = pdbReader.GetMethodDebugInformation(debugHandle);

            foreach (SequencePoint sequencePoint in debugInfo.GetSequencePoints())
            {
                if (sequencePoint.IsHidden || sequencePoint.Document.IsNil)
                    continue;

                Document document = pdbReader.GetDocument(sequencePoint.Document);
                return pdbReader.GetString(document.Name);
            }
        }
        catch (Exception e)
        {
            LoggingHelper.LogError("Failed to locate debug information for {Method}: {Exception}", method, e);
        }

        return null;
    }
}
