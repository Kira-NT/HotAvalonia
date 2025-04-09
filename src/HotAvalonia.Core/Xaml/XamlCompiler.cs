using System.Reflection;
using System.Reflection.Emit;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using HotAvalonia.Helpers;
using HotAvalonia.Reflection;

namespace HotAvalonia.Xaml;

/// <summary>
/// Provides functionality to compile XAML documents.
/// </summary>
public static class XamlCompiler
{
    /// <summary>
    /// The prefix used for compiled XAML type names.
    /// </summary>
    private const string CompiledXamlTypeNamePrefix = "Builder_";

    /// <summary>
    /// The name of the method used to populate controls.
    /// </summary>
    private const string PopulateMethodName = "__AvaloniaXamlIlPopulate";

    /// <summary>
    /// The name of the method used to build new control instances.
    /// </summary>
    private const string BuildMethodName = "__AvaloniaXamlIlBuild";

    /// <summary>
    /// The assembly containing XamlX type definitions.
    /// </summary>
    //
    // Please **DO NOT** delete this field!
    // This is a small hack to force `DynamicSreAssembly` to generate its backing type.
    // Currently, there is an issue where it happens slightly too late on Mono,
    // causing the `IgnoresAccessChecksToAttribute("Avalonia.Markup.Xaml.Loader")` to be
    // ignored by the runtime (ironic), because something from that assembly has already
    // seeped into our dynamic one by that point.
    // Since we CANNOT create `DynamicXamlAssembly`, which is what we actually need, earlier,
    // we use this trick to initialize `DynamicSreAssembly` as soon as this class is accessed.
    private static readonly Assembly s_xamlLoaderAssembly = DynamicSreAssembly.Create(typeof(AvaloniaRuntimeXamlLoader).Assembly, null!).Assembly;

    /// <summary>
    /// The delegate used to compile XAML documents.
    /// </summary>
    private static readonly CompileXamlFunc s_compile = CreateXamlCompiler(s_xamlLoaderAssembly);

    /// <summary>
    /// Gets the dynamic assembly that houses compiled XAML types
    /// </summary>
    public static DynamicAssembly? DynamicXamlAssembly => field ??= GetDynamicXamlAssembly(s_xamlLoaderAssembly);

    /// <inheritdoc cref="Compile(string, Uri, Assembly?)"/>
    public static CompiledXamlDocument Compile(string xaml, string uri, Assembly? assembly = null)
        => Compile(xaml, new Uri(uri), assembly);

    /// <summary>
    /// Compiles the specified XAML content.
    /// </summary>
    /// <param name="xaml">The XAML content as a string.</param>
    /// <param name="uri">The URI associated with the provided XAML content.</param>
    /// <param name="assembly">An optional assembly context for the compilation.</param>
    /// <returns>A <see cref="CompiledXamlDocument"/> representing the compiled XAML.</returns>
    public static CompiledXamlDocument Compile(string xaml, Uri uri, Assembly? assembly = null)
    {
        _ = xaml ?? throw new ArgumentNullException(nameof(xaml));
        _ = uri ?? throw new ArgumentNullException(nameof(uri));

        XamlDocument document = new(uri, xaml);
        RuntimeXamlLoaderConfiguration config = CreateDefaultXamlLoaderConfig(document, assembly);
        return Compile(document, config);
    }

    /// <summary>
    /// Compiles the specified <see cref="XamlDocument"/> using the default configuration.
    /// </summary>
    /// <param name="document">The XAML document to compile.</param>
    /// <returns>A <see cref="CompiledXamlDocument"/> representing the compiled XAML.</returns>
    public static CompiledXamlDocument Compile(this XamlDocument document)
    {
        RuntimeXamlLoaderConfiguration config = CreateDefaultXamlLoaderConfig(document, assembly: null);
        return Compile(document, config);
    }

    /// <summary>
    /// Compiles the specified <see cref="XamlDocument"/> using the specified configuration.
    /// </summary>
    /// <param name="document">The XAML document to compile.</param>
    /// <param name="config">The configuration settings for the XAML compiler.</param>
    /// <returns>A <see cref="CompiledXamlDocument"/> representing the compiled XAML.</returns>
    public static CompiledXamlDocument Compile(this XamlDocument document, RuntimeXamlLoaderConfiguration config)
    {
        _ = config ?? throw new ArgumentNullException(nameof(config));

        using IDisposable context = AssemblyHelper.ForceAllowDynamicCode();
        if (config.LocalAssembly is not null)
            DynamicXamlAssembly?.AllowAccessTo(config.LocalAssembly);

        Type compiledXamlType = s_compile([new(document.Uri, document.Stream)], config).First();
        return CreateCompiledXamlDocument(document.Uri, compiledXamlType);
    }

    /// <summary>
    /// Compiles a collection of <see cref="XamlDocument"/> using the default configuration.
    /// </summary>
    /// <param name="documents">The collection of XAML documents to compile.</param>
    /// <returns>An enumerable collection of compiled XAML documents.</returns>
    public static IEnumerable<CompiledXamlDocument> Compile(this IEnumerable<XamlDocument> documents)
    {
        _ = documents ?? throw new ArgumentNullException(nameof(documents));

        IReadOnlyCollection<XamlDocument> documentCollection = (documents as IReadOnlyCollection<XamlDocument>) ?? documents.ToArray();
        XamlDocument firstDocument = documentCollection.FirstOrDefault();
        RuntimeXamlLoaderConfiguration config = firstDocument.Uri is null ? new() : CreateDefaultXamlLoaderConfig(firstDocument, assembly: null);
        return Compile(documents, config);
    }

    /// <summary>
    /// Compiles a collection of <see cref="XamlDocument"/> using the specified configuration.
    /// </summary>
    /// <param name="documents">The collection of XAML documents to compile.</param>
    /// <param name="config">The configuration settings for the XAML compiler.</param>
    /// <returns>An enumerable collection of compiled XAML documents.</returns>
    public static IEnumerable<CompiledXamlDocument> Compile(this IEnumerable<XamlDocument> documents, RuntimeXamlLoaderConfiguration config)
    {
        _ = documents ?? throw new ArgumentNullException(nameof(documents));
        _ = config ?? throw new ArgumentNullException(nameof(config));

        using IDisposable context = AssemblyHelper.ForceAllowDynamicCode();
        if (config.LocalAssembly is not null)
            DynamicXamlAssembly?.AllowAccessTo(config.LocalAssembly);

        RuntimeXamlLoaderDocument[] xamlLoaderDocuments = documents.Select(static x => new RuntimeXamlLoaderDocument(x.Uri, x.Stream)).ToArray();
        return s_compile(xamlLoaderDocuments, config).Zip(xamlLoaderDocuments, static (type, doc) => CreateCompiledXamlDocument(doc.BaseUri!, type));
    }

    /// <summary>
    /// Creates the default configuration for the XAML compiler based on the provided document and assembly.
    /// </summary>
    /// <param name="document">The XAML document for which to create the configuration.</param>
    /// <param name="assembly">
    /// An optional assembly context; if not provided, it will be inferred from the document's URI.
    /// </param>
    /// <returns>A <see cref="RuntimeXamlLoaderConfiguration"/> initialized with default settings.</returns>
    private static RuntimeXamlLoaderConfiguration CreateDefaultXamlLoaderConfig(XamlDocument document, Assembly? assembly)
    {
        Assembly? localAssembly = assembly ?? AssetLoader.GetAssembly(document.Uri);
        bool useCompiledBindings = XamlScanner.UsesCompiledBindingsByDefault(assembly);
        return new() { LocalAssembly = localAssembly, UseCompiledBindingsByDefault = useCompiledBindings };
    }

    /// <summary>
    /// Creates a compiled XAML document from the specified URI and compiled type.
    /// </summary>
    /// <param name="uri">The URI associated with the XAML content.</param>
    /// <param name="compiledXamlType">The type representing the compiled XAML.</param>
    /// <returns>A <see cref="CompiledXamlDocument"/> representing the compiled XAML.</returns>
    private static CompiledXamlDocument CreateCompiledXamlDocument(Uri uri, Type compiledXamlType)
    {
        MethodInfo? buildMethod = compiledXamlType.GetStaticMethod(BuildMethodName);
        MethodInfo? populateMethod = compiledXamlType.GetStaticMethod(PopulateMethodName);
        if (buildMethod is null || populateMethod is null)
            throw new ArgumentException(null, nameof(compiledXamlType));

        return new(uri, buildMethod, populateMethod);
    }

    /// <summary>
    /// Retrieves the dynamic assembly that houses compiled XAML types.
    /// </summary>
    /// <param name="xamlLoaderAssembly">The assembly containing the XAML loader.</param>
    /// <returns>A <see cref="DynamicAssembly"/> instance if found; otherwise, <c>null</c>.</returns>
    private static DynamicAssembly? GetDynamicXamlAssembly(Assembly xamlLoaderAssembly)
    {
        // Avalonia creates a dynamic assembly during AvaloniaXamlIlRuntimeCompiler's initialization.
        using IDisposable context = AssemblyHelper.ForceAllowDynamicCode();

        Type xamlAssembly = xamlLoaderAssembly.GetType("XamlX.TypeSystem.IXamlAssembly") ?? typeof(object);
        Type? xamlIlRuntimeCompiler = xamlLoaderAssembly.GetType("Avalonia.Markup.Xaml.XamlIl.AvaloniaXamlIlRuntimeCompiler");

        MethodInfo? initializeSre = xamlIlRuntimeCompiler?.GetStaticMethod("InitializeSre", Type.EmptyTypes);
        initializeSre?.Invoke(null, null);

        object? sreAsm = xamlIlRuntimeCompiler?.GetStaticField("_sreAsm")?.GetValue(null);
        object? sreTypeSystem = xamlIlRuntimeCompiler?.GetStaticField("_sreTypeSystem")?.GetValue(null);
        if (sreAsm is not Assembly asm || sreTypeSystem is null)
            return null;

        DynamicAssembly dynamicAssembly = DynamicSreAssembly.Create(asm, sreTypeSystem);
        object? sreTypeSystemAssemblies = sreTypeSystem.GetType().GetInstanceField("_assemblies")?.GetValue(sreTypeSystem);
        MethodInfo? addAssembly = sreTypeSystemAssemblies?.GetType().GetMethod(nameof(List<string>.Add), [xamlAssembly]);
        if (xamlAssembly.IsAssignableFrom(dynamicAssembly.GetType()))
            addAssembly?.Invoke(sreTypeSystemAssemblies, [dynamicAssembly]);

        return dynamicAssembly;
    }

    /// <summary>
    /// Creates a XAML compiler delegate.
    /// </summary>
    /// <param name="xamlLoaderAssembly">The assembly containing the XAML loader.</param>
    /// <returns>A <see cref="CompileXamlFunc"/> delegate used to compile XAML documents.</returns>
    private static CompileXamlFunc CreateXamlCompiler(Assembly xamlLoaderAssembly)
    {
        try
        {
            Type? originalCompiler = xamlLoaderAssembly.GetType("Avalonia.Markup.Xaml.XamlIl.AvaloniaXamlIlRuntimeCompiler");
            MethodInfo? loadGroupSreCore = originalCompiler?.GetStaticMethod("LoadGroupSreCore", [typeof(IReadOnlyCollection<RuntimeXamlLoaderDocument>), typeof(RuntimeXamlLoaderConfiguration)]);
            return RecompileXamlCompiler(loadGroupSreCore);
        }
        catch (Exception e)
        {
            LoggingHelper.LogError("Failed to recompile XamlIl compiler: {Exception}", e);
        }

        // Welp, at least we have a fallback.
        // Somehow, we managed to live off it until v3, so it's not *that* bad.
        // However, I would definitely prefer a much more predictable solution
        // with no side effects, which we aim to achieve by recompiling the compiler itself.
        return Load;
    }

    /// <summary>
    /// Loads compiled types from XAML documents using the default XAML loader.
    /// </summary>
    /// <param name="documents">A read-only collection of runtime XAML loader documents.</param>
    /// <param name="config">The configuration settings for the XAML loader.</param>
    /// <returns>An enumerable collection of compiled types.</returns>
    private static IEnumerable<Type> Load(IReadOnlyCollection<RuntimeXamlLoaderDocument> documents, RuntimeXamlLoaderConfiguration config)
    {
        HashSet<Type>[] oldCompiledTypes = documents.Select(static x => new HashSet<Type>(FindCompiledXamlTypes(x.BaseUri!.ToString()))).ToArray();
        _ = AvaloniaRuntimeXamlLoader.LoadGroup(documents, config);
        return documents.Select((x, i) => FindCompiledXamlTypes(x.BaseUri!.ToString()).FirstOrDefault(y => !oldCompiledTypes[i].Contains(y)));
    }

    /// <summary>
    /// Finds compiled XAML types within the dynamic assembly that match the specified URI.
    /// </summary>
    /// <param name="uri">The URI used to identify the compiled XAML types.</param>
    /// <returns>An enumerable collection of types whose names match the specified URI.</returns>
    private static IEnumerable<Type> FindCompiledXamlTypes(string uri)
    {
        Assembly? dynamicXamlAssembly = DynamicXamlAssembly?.Assembly;
        if (dynamicXamlAssembly is null)
            yield break;

        string safeUri = UriHelper.GetSafeUriIdentifier(uri);
        foreach (Type type in dynamicXamlAssembly.GetLoadedTypes())
        {
            if (!type.Name.StartsWith(CompiledXamlTypeNamePrefix, StringComparison.Ordinal))
                continue;

            if (!type.Name.EndsWith(safeUri, StringComparison.Ordinal))
                continue;

            MethodInfo? populateMethod = type.GetStaticMethod(PopulateMethodName);
            if (XamlScanner.IsPopulateMethod(populateMethod))
                yield return type;
        }
    }

    /// <summary>
    /// Recompiles the XAML compiler so that it returns the generated types directly,
    /// without attempting to unnecessarily instantiate them when we don't need it.
    /// </summary>
    /// <param name="compile">The original compile method to recompile.</param>
    /// <returns>A <see cref="CompileXamlFunc"/> delegate that can be used to compile XAML documents.</returns>
    private static CompileXamlFunc RecompileXamlCompiler(MethodInfo? compile)
    {
        if (compile?.GetMethodBody() is not MethodBody body || body.GetILAsByteArray() is not byte[] bodyIl)
            throw new ArgumentException("Provided method does not have a body.", nameof(compile));

        // `ILGenerator` is atrociously designed. Somebody, in their infinite wisdom, decided that
        // making a class responsible for emitting opcodes user-hecking-friendly was a good idea.
        // As a consequence of this decision, we now have `BeginExceptionBlock`, `EndExceptionBlock`,
        // etc., which automatically emit opcodes that you don't want. Since `ILGenerator` doesn't
        // support removing already emitted instructions, this problem becomes quite complicated to
        // solve. The best approach here would be to dig into the internals of `ILGenerator` and use
        // its private mechanism for defining protected regions. However, CoreCLR and Mono handle this
        // **very** differently, and I don't really want to deal with that right now. Maybe I'll make
        // a separate library for it later.
        //
        // For now, though, we can take the simplest (and stupidest) approach - just ignore exception
        // blocks. `LoadGroupSreCore` only uses a few of them, and that's only because of `try-finally`
        // blocks automatically generated by `using` directives for disposable types.
        // The second `IDisposable` in question is literally an array enumerator (i.e., its `Dispose()`
        // is a no-op), while the first one will be disposed properly no matter what.
        //
        // To "skip" the exception blocks, we need to replace `leave`, `leave.s`, and `endfinally` opcodes
        // with `nop`s. This preserves the natural flow of the function because `leave` instructions literally
        // point to the instruction **right after** their respective `finally` blocks, which we still need
        // to execute. However, this approach is pretty fragile. Thus, if we detect that the method has changed
        // and gained/lost an exception block or two, just abort and fall back to using the original compiler as-is.
        if (body.ExceptionHandlingClauses.Any(x => x.Flags != ExceptionHandlingClauseOptions.Finally))
            throw new NotSupportedException("Provided method contains unsupported exception block(s).");

        Module module = compile.Module;
        string name = $"<Recompiled{Guid.NewGuid():N}>{compile.Name}";
        Type returnType = typeof(IEnumerable<Type>);
        Type[] parameterTypes = compile.GetParameterTypes();
        using IDisposable ctx = MethodHelper.DefineDynamicMethod(name, returnType, parameterTypes, out DynamicMethod recompiled);

        ILGenerator il = recompiled.GetILGenerator();
        il.DeclareLocals(body.LocalVariables);

        MethodBodyReader reader = new(bodyIl);
        while (reader.Next())
        {
            switch (reader.OpCode.OperandType)
            {
                case OperandType.InlineI8:
                    il.Emit(reader.OpCode, reader.GetInt64());
                    break;

                case OperandType.InlineField:
                    il.Emit(reader.OpCode, reader.ResolveField(module));
                    break;

                case OperandType.InlineNone when reader.OpCode == OpCodes.Endfinally:
                    il.Emit(OpCodes.Nop);
                    break;

                case OperandType.InlineNone:
                    il.Emit(reader.OpCode);
                    break;

                case OperandType.ShortInlineR:
                    il.Emit(reader.OpCode, reader.GetSingle());
                    break;

                case OperandType.InlineR:
                    il.Emit(reader.OpCode, reader.GetDouble());
                    break;

                case OperandType.InlineString:
                    il.Emit(reader.OpCode, reader.ResolveString(module));
                    break;

                case OperandType.InlineType:
                    il.Emit(reader.OpCode, reader.ResolveType(module));
                    break;

                case OperandType.InlineVar:
                    il.Emit(reader.OpCode, reader.GetInt16());
                    break;

                case OperandType.InlineBrTarget when reader.OpCode == OpCodes.Leave:
                    il.Emit(OpCodes.Nop);
                    il.Emit(OpCodes.Nop);
                    il.Emit(OpCodes.Nop);
                    il.Emit(OpCodes.Nop);
                    il.Emit(OpCodes.Nop);
                    break;

                case OperandType.InlineBrTarget:
                case OperandType.InlineI:
                    il.Emit(reader.OpCode, reader.GetInt32());
                    break;

                case OperandType.ShortInlineBrTarget when reader.OpCode == OpCodes.Leave_S:
                    il.Emit(OpCodes.Nop);
                    il.Emit(OpCodes.Nop);
                    break;

                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    il.Emit(reader.OpCode, reader.GetByte());
                    break;

                case OperandType.InlineMethod when reader.ResolveMethod(module) is ConstructorInfo ctor:
                    il.Emit(reader.OpCode, ctor);
                    break;

                case OperandType.InlineMethod when reader.ResolveMethod(module) is MethodInfo method:
                    if (method is not { DeclaringType.Name: nameof(Enumerable), Name: nameof(Enumerable.Zip) })
                    {
                        il.Emit(reader.OpCode, method);
                        break;
                    }

                    // `LoadGroupSreCore` uses `types.Zip(documents, (x, y) => (x, y)).Select(...).ToArray()`
                    // to instantiate generated types. So, if we want to get those as-is, this is the best
                    // place to intercept the method's flow. When execution reaches the `.Zip(...)` call,
                    // there are three objects on the stack: a collection of generated types, the documents
                    // they were created from, and a no-op selector. If we simply pop the last two, we get
                    // exactly what we need. Slap a `ret` after that, and we're done.
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Ret);
                    return recompiled.CreateDelegate<CompileXamlFunc>();

                default:
                    throw new NotSupportedException($"Provided method contains unsupported instruction: {reader.OpCode}");
            }
        }

        // We should have never reached this point.
        throw new NotSupportedException("Provided method contains unsupported exit point.");
    }
}
