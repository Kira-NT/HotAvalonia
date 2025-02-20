using System.Net;
using HotAvalonia.Fody.Cecil;
using HotAvalonia.Fody.Helpers;
using HotAvalonia.Fody.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HotAvalonia.Fody;

/// <summary>
/// Weaves remote file system credentials into the hot reload mechanism.
/// </summary>
internal sealed class FileSystemCredentialsWeaver : FeatureWeaver
{
    /// <summary>
    /// The constructor reference for <see cref="IPEndPoint(IPAddress, int)"/>.
    /// </summary>
    private readonly CecilMethod _ipEndpointCtor;

    /// <summary>
    /// The method reference for <see cref="IPAddress.Parse(string)"/>.
    /// </summary>
    private readonly CecilMethod _ipAddressParse;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemCredentialsWeaver"/> class.
    /// </summary>
    /// <param name="root">The root module weaver.</param>
    public FileSystemCredentialsWeaver(ModuleWeaver root) : base(root)
    {
        _ipEndpointCtor = root.GetType(typeof(IPEndPoint)).GetMethod(x => x.GetConstructor([typeof(IPAddress), typeof(int)]));
        _ipAddressParse = root.GetType(typeof(IPAddress)).GetMethod(x => x.GetMethod(nameof(IPAddress.Parse), [typeof(string)]));
    }

    /// <summary>
    /// Gets the remote file system address specified in the weaver configuration.
    /// </summary>
    private string Address => this[nameof(Address)];

    /// <summary>
    /// Gets the remote file system port number specified in the weaver configuration.
    /// </summary>
    private int Port => this[nameof(Port), 0];

    /// <summary>
    /// Gets the secret used for remote file system credentials, encoded in Base64.
    /// </summary>
    private string Secret => this[nameof(Secret)];

    /// <summary>
    /// Gets the endpoint used for remote file system communication.
    /// </summary>
    private IPEndPoint Endpoint
    {
        get
        {
            const int DefaultRemoteFileSystemPort = 20158;

            string? address = Address;
            if (!IPAddress.TryParse(address, out IPAddress? ip))
                ip = IPAddress.Loopback;

            int port = Port;
            if (port <= 0)
                port = DefaultRemoteFileSystemPort;

            return new(ip, port);
        }
    }

    /// <inheritdoc/>
    public override IEnumerable<string> GetAssembliesForScanning() =>
    [
        "System.Net.Primitives",
    ];

    /// <inheritdoc/>
    public override void Execute()
    {
        const string GetFileSystemMethodName = "GetFileSystem";

        TypeDefinition? hotReloadExtensions = ModuleDefinition.GetType(UnreferencedTypes.HotAvalonia_AvaloniaHotReloadExtensions);
        MethodDefinition? getFileSystem = hotReloadExtensions?.GetMethod(GetFileSystemMethodName, BindingFlag.AnyStatic, []);
        if (hotReloadExtensions is null || getFileSystem is null)
        {
            WriteError($"'{UnreferencedTypes.HotAvalonia_AvaloniaHotReloadExtensions}.{GetFileSystemMethodName}()' not found in '{AssemblyFilePath}'.");
            return;
        }

        if (!StringHelper.TryGetBase64Bytes(Secret, out byte[]? secretBytes))
        {
            WriteError("Invalid secret format. The secret must be a valid Base64 string.");
            return;
        }

        if (!getFileSystem.TryGetLastCallInstruction(UnreferencedTypes.HotAvalonia_IO_IFileSystem, out Instruction? call))
        {
            WriteError($"Unable to patch '{getFileSystem}'.");
            return;
        }

        if (!InjectCredentials(getFileSystem, call, Endpoint, secretBytes))
        {
            WriteError($"Unable to find matching method for '{call.Operand}'.");
            return;
        }
    }

    /// <summary>
    /// Injects the remote file system credentials into the specified method at the given injection point.
    /// </summary>
    /// <param name="target">The method definition in which to inject the credentials.</param>
    /// <param name="injectionPoint">The instruction at which to perform the injection.</param>
    /// <param name="endpoint">The endpoint containing the IP address and port for remote file system communication.</param>
    /// <param name="secret">The secret bytes to use as credentials.</param>
    /// <returns><c>true</c> if the injection was successful; otherwise, <c>false</c>.</returns>
    private bool InjectCredentials(MethodDefinition target, Instruction injectionPoint, IPEndPoint endpoint, byte[] secret)
    {
        if (injectionPoint.Operand is not MethodReference factory)
            return false;

        TypeName[] patchedFactoryParameters = factory.Parameters.Select(x => (TypeName)x.ParameterType).Concat([typeof(IPEndPoint), typeof(byte[])]).ToArray();
        TypeDefinition declaringType = factory.DeclaringType.Resolve();
        MethodDefinition? patchedFactory = declaringType.GetMethod(factory.Name, patchedFactoryParameters, factory.ReturnType);
        if (patchedFactory is null)
            return false;

        injectionPoint.Operand = target.Module.ImportReference(patchedFactory);
        ILProcessor il = target.Body.GetILProcessor();

        il.InsertBefore(injectionPoint, il.Create(OpCodes.Ldstr, endpoint.Address.ToString()));
        il.InsertBefore(injectionPoint, il.Create(OpCodes.Call, _ipAddressParse));
        il.InsertBefore(injectionPoint, il.Create(OpCodes.Ldc_I4, endpoint.Port));
        il.InsertBefore(injectionPoint, il.Create(OpCodes.Newobj, _ipEndpointCtor));

        il.InsertBefore(injectionPoint, il.Create(OpCodes.Ldc_I4, secret.Length));
        il.InsertBefore(injectionPoint, il.Create(OpCodes.Newarr, target.Module.TypeSystem.Byte));
        for (int i = 0; i < secret.Length; i++)
        {
            il.InsertBefore(injectionPoint, il.Create(OpCodes.Dup));
            il.InsertBefore(injectionPoint, il.Create(OpCodes.Ldc_I4, i));
            il.InsertBefore(injectionPoint, il.Create(OpCodes.Ldc_I4, (int)secret[i]));
            il.InsertBefore(injectionPoint, il.Create(OpCodes.Stelem_I1));
        }

        return true;
    }
}
