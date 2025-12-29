using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Avalonia.Markup.Xaml;
using HotAvalonia.Collections;
using HotAvalonia.Helpers;

namespace HotAvalonia.Xaml;

/// <summary>
/// Provides a (somewhat) thread-safe implementation of <c>IXamlTypeSystem</c> that
/// has access to all types provided by the specified application domain at all times.
/// </summary>
internal static class DynamicSreTypeSystem
{
    /// <summary>
    /// Represents the <c>XamlX.IL.DynamicSreTypeSystem</c> type.
    /// </summary>
    private static readonly Type s_type = CreateDynamicSreTypeSystemType();

    /// <summary>
    /// Creates a new instance of the <see cref="DynamicSreTypeSystem"/> class.
    /// </summary>
    /// <param name="appDomain">The <see cref="AppDomain"/> that contains the types accessible by the type system.</param>
    /// <param name="host">The <see cref="AssemblyBuilder"/> used for runtime-compiled XAML.</param>
    /// <returns>A new instance of the <see cref="DynamicSreTypeSystem"/> class.</returns>
    public static object Create(AppDomain appDomain, AssemblyBuilder host)
        => Activator.CreateInstance(s_type, appDomain, host)!;

    /// <summary>
    /// Generates the <c>XamlX.IL.DynamicSreTypeSystem</c> type.
    /// </summary>
    /// <returns>The generated <c>XamlX.IL.DynamicSreTypeSystem</c> type.</returns>
    [MethodImpl(MethodImplOptions.NoOptimization)]
    private static Type CreateDynamicSreTypeSystemType()
    {
        using IDisposable context = AssemblyHelper.GetDynamicAssembly(out AssemblyBuilder assembly, out ModuleBuilder module);
        string fullName = "XamlX.IL.DynamicSreTypeSystem";
        Type? existingType = assembly.GetType(fullName, throwOnError: false);
        if (existingType is not null)
            return existingType;

        Assembly XamlX = typeof(AvaloniaRuntimeXamlLoader).Assembly;
        assembly.ForceAllowAccessTo(XamlX);
        foreach (AssemblyName referencedAssemblyName in XamlX.GetReferencedAssemblies())
            assembly.ForceAllowAccessTo(referencedAssemblyName);

        Type IXamlType = XamlX.GetType("XamlX.TypeSystem.IXamlType", throwOnError: true)!;
        Type IXamlAssembly = XamlX.GetType("XamlX.TypeSystem.IXamlAssembly", throwOnError: true)!;
        Type IXamlTypeSystem = XamlX.GetType("XamlX.TypeSystem.IXamlTypeSystem", throwOnError: true)!;
        Type IXamlCustomAttribute = XamlX.GetType("XamlX.TypeSystem.IXamlCustomAttribute", throwOnError: true)!;
        Type SreTypeSystem = XamlX.GetType("XamlX.IL.SreTypeSystem", throwOnError: true)!;
        Type SreType = XamlX.GetType("XamlX.IL.SreTypeSystem+SreType", throwOnError: true)!;
        Type SreAssembly = XamlX.GetType("XamlX.IL.SreTypeSystem+SreAssembly", throwOnError: true)!;
        FieldInfo SreAssembly_typeDic = SreAssembly.GetField("_typeDic", (BindingFlags)(-1))!;
        Type XamlAssemblyName = CreateXamlAssemblyNameType(module, IXamlAssembly, IXamlType, IXamlCustomAttribute);

        // public sealed class DynamicSreTypeSystem : SreTypeSystem, IXamlTypeSystem, IDisposable
        // {
        ILGenerator il = null!;
        TypeBuilder DynamicSreTypeSystem = module.DefineType(fullName, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class);
        DynamicSreTypeSystem.SetParent(SreTypeSystem);
        DynamicSreTypeSystem.AddInterfaceImplementation(IXamlTypeSystem);
        DynamicSreTypeSystem.AddInterfaceImplementation(typeof(IDisposable));

        //     private static readonly Comparer<IXamlAssembly> s_xamlAssemblyNameComparer;
        FieldBuilder s_xamlAssemblyNameComparer = DynamicSreTypeSystem.DefineField(
            nameof(s_xamlAssemblyNameComparer),
            typeof(Comparer<>).MakeGenericType(IXamlAssembly),
            FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly
        );

        //     private readonly AppDomain _appDomain;
        FieldBuilder _appDomain = DynamicSreTypeSystem.DefineField(
            nameof(_appDomain),
            typeof(AppDomain),
            FieldAttributes.Private | FieldAttributes.InitOnly
        );

        //     private readonly AssemblyBuilder _host;
        FieldBuilder _host = DynamicSreTypeSystem.DefineField(
            nameof(_host),
            typeof(AssemblyBuilder),
            FieldAttributes.Private | FieldAttributes.InitOnly
        );

        //     private readonly SreAssembly _internalAssembly;
        FieldBuilder _internalAssembly = DynamicSreTypeSystem.DefineField(
            nameof(_internalAssembly),
            SreAssembly,
            FieldAttributes.Private | FieldAttributes.InitOnly
        );

        //     private readonly object _lock;
        FieldBuilder _lock = DynamicSreTypeSystem.DefineField(
            nameof(_lock),
            typeof(object),
            FieldAttributes.Private | FieldAttributes.InitOnly
        );

        //     private List<IXamlAssembly> _assemblies; // Inherited from SreTypeSystem
        FieldInfo _assemblies = SreTypeSystem.GetField(nameof(_assemblies), (BindingFlags)(-1))!;

        //     private Dictionary<Type, SreType> _typeDic; // Inherited from SreTypeSystem
        FieldInfo _typeDic = SreTypeSystem.GetField(nameof(_typeDic), (BindingFlags)(-1))!;

        //     private static int CompareAssemblyNames(Assembly x, Assembly y)
        //         => string.Compare(x.GetName().Name, y.GetName().Name, StringComparison.InvariantCultureIgnoreCase);
        MethodBuilder CompareAssemblyNames = DynamicSreTypeSystem.DefineMethod(
            nameof(CompareAssemblyNames),
            MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig,
            typeof(int), [typeof(Assembly), typeof(Assembly)]
        );
        il = CompareAssemblyNames.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Callvirt, typeof(Assembly).GetMethod(nameof(Assembly.GetName), [])!);
        il.Emit(OpCodes.Callvirt, typeof(AssemblyName).GetProperty(nameof(AssemblyName.Name))!.GetMethod!);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Callvirt, typeof(Assembly).GetMethod(nameof(Assembly.GetName), [])!);
        il.Emit(OpCodes.Callvirt, typeof(AssemblyName).GetProperty(nameof(AssemblyName.Name))!.GetMethod!);
        il.Emit(OpCodes.Ldc_I4_3);
        il.Emit(OpCodes.Call, new Func<string, string, StringComparison, int>(string.Compare).Method);
        il.Emit(OpCodes.Ret);

        //     private static int CompareXamlAssemblyNames(IXamlAssembly x, IXamlAssembly y)
        //         => string.Compare(x.Name, y.Name, StringComparison.InvariantCultureIgnoreCase);
        MethodBuilder CompareXamlAssemblyNames = DynamicSreTypeSystem.DefineMethod(
            nameof(CompareXamlAssemblyNames),
            MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig,
            typeof(int), [IXamlAssembly, IXamlAssembly]
        );
        il = CompareXamlAssemblyNames.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Callvirt, IXamlAssembly.GetProperty(nameof(AssemblyName.Name))!.GetMethod!);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Callvirt, IXamlAssembly.GetProperty(nameof(AssemblyName.Name))!.GetMethod!);
        il.Emit(OpCodes.Ldc_I4_3);
        il.Emit(OpCodes.Call, new Func<string, string, StringComparison, int>(string.Compare).Method);
        il.Emit(OpCodes.Ret);

        //     private static SreAssembly CreateSreAssembly(Assembly assembly, SreTypeSystem sreTypeSystem)
        //     {
        //         SreAssembly sreAssembly = new(sreTypeSystem, assembly);
        //         sreAssembly.Types = new ListAdapter<SreType>(sreAssembly._typeDic.Values);
        //         return sreAssembly;
        //     }
        Type sreTypeListAdapter = typeof(ListAdapter<>).MakeGenericType(SreType);
        MethodBuilder CreateSreAssembly = DynamicSreTypeSystem.DefineMethod(
            nameof(CreateSreAssembly),
            MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig,
            SreAssembly, [typeof(Assembly), SreTypeSystem]
        );
        il = CreateSreAssembly.GetILGenerator();
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Newobj, SreAssembly.GetConstructor((BindingFlags)(-1), null, [SreTypeSystem, typeof(Assembly)], null)!);
        il.Emit(OpCodes.Dup);
        il.Emit(OpCodes.Dup);
        il.Emit(OpCodes.Ldfld, SreAssembly_typeDic);
        il.Emit(OpCodes.Call, SreAssembly_typeDic.FieldType.GetProperty(nameof(Dictionary<,>.Values))!.GetMethod);
        il.Emit(OpCodes.Newobj, sreTypeListAdapter.GetConstructors()[0]);
        il.Emit(OpCodes.Stfld, SreAssembly.GetFields((BindingFlags)(-1)).Single(x => x.FieldType.IsAssignableFrom(sreTypeListAdapter)));
        il.Emit(OpCodes.Ret);

        //     private static void LoadSreTypes(Type[] types, SreAssembly publicAssembly, SreAssembly internalAssembly)
        //     {
        //         SreTypeSystem sreTypeSystem = publicAssembly._system;
        //         Dictionary<Type, SreType> globalTypes = sreTypeSystem._typeDic;
        //         Dictionary<string, SreType> publicTypes = publicAssembly._typeDic;
        //         Dictionary<string, SreType> internalTypes = internalAssembly._typeDic;
        //         for (int i = 0; i < types.Length; i++)
        //         {
        //             Type type = types[i];
        //             SreType sreType = new(sreTypeSystem, publicAssembly, type);
        //             globalTypes[type] = sreType;
        //
        //             // Unlike Avalonia, we do not consider internal types "public" (duh!), so they
        //             // don't have the potential to overshadow public types from other assemblies.
        //             bool isPublic = type.IsPublic | (type.IsNestedPublic && publicTypes.ContainsKey(type.DeclaringType.FullName));
        //             if (isPublic)
        //             {
        //                 publicTypes[type.FullName] = sreType;
        //             }
        //             else if (!internalTypes.TryGetValue(type.FullName, out SreType? oldType) || CompareSreAssemblyNames(oldType.Assembly!, sreType.Assembly!) > 0)
        //             {
        //                 internalTypes[type.FullName] = sreType;
        //             }
        //         }
        //     }
        MethodBuilder LoadSreTypes = DynamicSreTypeSystem.DefineMethod(
            nameof(LoadSreTypes),
            MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig,
            typeof(void), [typeof(Type[]), SreAssembly, SreAssembly]
        );
        il = LoadSreTypes.GetILGenerator();
        Label loadSreTypesLoopHead = il.DefineLabel();
        Label loadSreTypesLoopAction = il.DefineLabel();
        Label loadSreTypesLoopCheck = il.DefineLabel();
        Label loadSreTypesIsNotNestedPublic = il.DefineLabel();
        Label loadSreTypesCheckIsPublic = il.DefineLabel();
        Label loadSreTypesIsNotPublic = il.DefineLabel();
        Label loadSreTypesAddInternalType = il.DefineLabel();
        il.DeclareLocal(SreTypeSystem);
        il.DeclareLocal(_typeDic.FieldType);
        il.DeclareLocal(SreAssembly_typeDic.FieldType);
        il.DeclareLocal(SreAssembly_typeDic.FieldType);
        il.DeclareLocal(typeof(int));
        il.DeclareLocal(typeof(Type));
        il.DeclareLocal(SreType);
        il.DeclareLocal(SreType);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldfld, SreAssembly.GetField("_system", (BindingFlags)(-1))!);
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldfld, _typeDic);
        il.Emit(OpCodes.Stloc_1);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldfld, SreAssembly_typeDic);
        il.Emit(OpCodes.Stloc_2);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Ldfld, SreAssembly_typeDic);
        il.Emit(OpCodes.Stloc_3);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stloc_S, (byte)4);
        il.Emit(OpCodes.Br, loadSreTypesLoopCheck);
        il.MarkLabel(loadSreTypesLoopHead);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldloc_S, (byte)4);
        il.Emit(OpCodes.Ldelem_Ref);
        il.Emit(OpCodes.Stloc_S, (byte)5);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldloc_S, (byte)5);
        il.Emit(OpCodes.Newobj, SreType.GetConstructor((BindingFlags)(-1), null, [SreTypeSystem, SreAssembly, typeof(Type)], null)!);
        il.Emit(OpCodes.Stloc_S, (byte)6);
        il.Emit(OpCodes.Ldloc_1);
        il.Emit(OpCodes.Ldloc_S, (byte)5);
        il.Emit(OpCodes.Ldloc_S, (byte)6);
        il.Emit(OpCodes.Call, _typeDic.FieldType.GetProperty("Item")!.SetMethod!);
        il.Emit(OpCodes.Ldloc_S, (byte)5);
        il.Emit(OpCodes.Callvirt, typeof(Type).GetProperty(nameof(Type.IsPublic))!.GetMethod!);
        il.Emit(OpCodes.Ldloc_S, (byte)5);
        il.Emit(OpCodes.Callvirt, typeof(Type).GetProperty(nameof(Type.IsNestedPublic))!.GetMethod!);
        il.Emit(OpCodes.Brfalse_S, loadSreTypesIsNotNestedPublic);
        il.Emit(OpCodes.Ldloc_2);
        il.Emit(OpCodes.Ldloc_S, (byte)5);
        il.Emit(OpCodes.Callvirt, typeof(MemberInfo).GetProperty(nameof(MemberInfo.DeclaringType))!.GetMethod!);
        il.Emit(OpCodes.Callvirt, typeof(Type).GetProperty(nameof(Type.FullName))!.GetMethod!);
        il.Emit(OpCodes.Call, SreAssembly_typeDic.FieldType.GetMethod(nameof(Dictionary<,>.ContainsKey))!);
        il.Emit(OpCodes.Br_S, loadSreTypesCheckIsPublic);
        il.MarkLabel(loadSreTypesIsNotNestedPublic);
        il.Emit(OpCodes.Ldc_I4_0);
        il.MarkLabel(loadSreTypesCheckIsPublic);
        il.Emit(OpCodes.Or);
        il.Emit(OpCodes.Brfalse_S, loadSreTypesIsNotPublic);
        il.Emit(OpCodes.Ldloc_2);
        il.Emit(OpCodes.Ldloc_S, (byte)5);
        il.Emit(OpCodes.Callvirt, typeof(Type).GetProperty(nameof(Type.FullName))!.GetMethod!);
        il.Emit(OpCodes.Ldloc_S, (byte)6);
        il.Emit(OpCodes.Call, SreAssembly_typeDic.FieldType.GetProperty("Item")!.SetMethod!);
        il.Emit(OpCodes.Br_S, loadSreTypesLoopAction);
        il.MarkLabel(loadSreTypesIsNotPublic);
        il.Emit(OpCodes.Ldloc_3);
        il.Emit(OpCodes.Ldloc_S, (byte)5);
        il.Emit(OpCodes.Callvirt, typeof(Type).GetProperty(nameof(Type.FullName))!.GetMethod!);
        il.Emit(OpCodes.Ldloca_S, (byte)7);
        il.Emit(OpCodes.Call, SreAssembly_typeDic.FieldType.GetMethod(nameof(Dictionary<,>.TryGetValue)));
        il.Emit(OpCodes.Brfalse_S, loadSreTypesAddInternalType);
        il.Emit(OpCodes.Ldloc_S, (byte)7);
        il.Emit(OpCodes.Callvirt, SreType.GetProperty(nameof(Type.Assembly))!.GetMethod!);
        il.Emit(OpCodes.Ldloc_S, (byte)6);
        il.Emit(OpCodes.Callvirt, SreType.GetProperty(nameof(Type.Assembly))!.GetMethod!);
        il.Emit(OpCodes.Call, CompareXamlAssemblyNames);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ble_S, loadSreTypesLoopAction);
        il.MarkLabel(loadSreTypesAddInternalType);
        il.Emit(OpCodes.Ldloc_3);
        il.Emit(OpCodes.Ldloc_S, (byte)5);
        il.Emit(OpCodes.Callvirt, typeof(Type).GetProperty(nameof(Type.FullName))!.GetMethod!);
        il.Emit(OpCodes.Ldloc_S, (byte)6);
        il.Emit(OpCodes.Call, SreAssembly_typeDic.FieldType.GetProperty("Item")!.SetMethod!);
        il.MarkLabel(loadSreTypesLoopAction);
        il.Emit(OpCodes.Ldloc_S, (byte)4);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Stloc_S, (byte)4);
        il.MarkLabel(loadSreTypesLoopCheck);
        il.Emit(OpCodes.Ldloc_S, (byte)4);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldlen);
        il.Emit(OpCodes.Conv_I4);
        il.Emit(OpCodes.Blt, loadSreTypesLoopHead);
        il.Emit(OpCodes.Ret);

        //     private void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs eventArgs)
        //     {
        //         Assembly? assembly = eventArgs?.LoadedAssembly;
        //         if (assembly is not { IsDynamic: false })
        //             return;
        //
        //         using (IDisposable context = AssemblyHelper.ForceAllowDynamicCode())
        //             _host.AllowAccessTo(assembly);
        //
        //         List<IXamlAssembly> assemblies = _assemblies;
        //         SreAssembly publicAssembly = CreateSreAssembly(assembly, this);
        //         SreAssembly internalAssembly = _internalAssembly;
        //         IEnumerable<Type> loadedTypes = assembly.GetLoadedTypes();
        //         Type[] types = loadedTypes as Type[] ?? loadedTypes.ToArray();
        //         lock (_lock)
        //         {
        //             LoadSreTypes(types, publicAssembly, internalAssembly);
        //             if (publicAssembly._typeDic.Count == 0)
        //                 return;
        //
        //             int i = assemblies.BinarySearch(0, assemblies.Count - 1, publicAssembly, s_xamlAssemblyNameComparer);
        //             assemblies.Insert(i ^ (i >> 31), publicAssembly);
        //         }
        //     }
        MethodBuilder OnAssemblyLoad = DynamicSreTypeSystem.DefineMethod(
            nameof(OnAssemblyLoad),
            MethodAttributes.Private | MethodAttributes.HideBySig,
            typeof(void), [typeof(object), typeof(AssemblyLoadEventArgs)]
        );
        il = OnAssemblyLoad.GetILGenerator();
        Label onAssemblyLoadEventArgsNotNull = il.DefineLabel();
        Label onAssemblyLoadCheckLoadedAssembly = il.DefineLabel();
        Label onAssemblyLoadLoadedAssemblyIsNullOrDynamic = il.DefineLabel();
        Label onAssemblyLoadLoadedAssemblyIsNotDynamic = il.DefineLabel();
        Label onAssemblyLoadEndDynamicScopeFinally = il.DefineLabel();
        Label onAssemblyLoadLoadedTypesIsArray = il.DefineLabel();
        Label onAssemblyLoadLoadedAssemblyContainsPublicTypes = il.DefineLabel();
        Label onAssemblyLoadEndFinally = il.DefineLabel();
        Label onAssemblyLoadRet = il.DefineLabel();
        il.DeclareLocal(typeof(Assembly));
        il.DeclareLocal(_assemblies.FieldType);
        il.DeclareLocal(SreAssembly);
        il.DeclareLocal(SreAssembly);
        il.DeclareLocal(typeof(IEnumerable<Type>));
        il.DeclareLocal(typeof(Type[]));
        il.DeclareLocal(typeof(IDisposable));
        il.DeclareLocal(typeof(object));
        il.DeclareLocal(typeof(bool));
        il.DeclareLocal(typeof(int));
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Brtrue_S, onAssemblyLoadEventArgsNotNull);
        il.Emit(OpCodes.Ldnull);
        il.Emit(OpCodes.Br_S, onAssemblyLoadCheckLoadedAssembly);
        il.MarkLabel(onAssemblyLoadEventArgsNotNull);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Call, typeof(AssemblyLoadEventArgs).GetProperty(nameof(AssemblyLoadEventArgs.LoadedAssembly))!.GetMethod!);
        il.MarkLabel(onAssemblyLoadCheckLoadedAssembly);
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Brfalse_S, onAssemblyLoadLoadedAssemblyIsNullOrDynamic);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Callvirt, typeof(Assembly).GetProperty(nameof(Assembly.IsDynamic))!.GetMethod!);
        il.Emit(OpCodes.Brfalse_S, onAssemblyLoadLoadedAssemblyIsNotDynamic);
        il.MarkLabel(onAssemblyLoadLoadedAssemblyIsNullOrDynamic);
        il.Emit(OpCodes.Ret);
        il.MarkLabel(onAssemblyLoadLoadedAssemblyIsNotDynamic);
        il.Emit(OpCodes.Call, new Func<IDisposable>(AssemblyHelper.ForceAllowDynamicCode).Method);
        il.Emit(OpCodes.Stloc_S, (byte)6);
        il.BeginExceptionBlock();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, _host);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Call, new Action<AssemblyBuilder, Assembly>(AssemblyHelper.ForceAllowAccessTo).Method);
        il.BeginFinallyBlock();
        il.Emit(OpCodes.Ldloc_S, (byte)6);
        il.Emit(OpCodes.Brfalse_S, onAssemblyLoadEndDynamicScopeFinally);
        il.Emit(OpCodes.Ldloc_S, (byte)6);
        il.Emit(OpCodes.Callvirt, typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose))!);
        il.MarkLabel(onAssemblyLoadEndDynamicScopeFinally);
        il.EndExceptionBlock();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, _assemblies);
        il.Emit(OpCodes.Stloc_1);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, CreateSreAssembly);
        il.Emit(OpCodes.Stloc_2);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, _internalAssembly);
        il.Emit(OpCodes.Stloc_3);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Call, new Func<Assembly, IEnumerable<Type>>(AssemblyHelper.GetLoadedTypes).Method);
        il.Emit(OpCodes.Stloc_S, (byte)4);
        il.Emit(OpCodes.Ldloc_S, (byte)4);
        il.Emit(OpCodes.Isinst, typeof(Type[]));
        il.Emit(OpCodes.Dup);
        il.Emit(OpCodes.Brtrue_S, onAssemblyLoadLoadedTypesIsArray);
        il.Emit(OpCodes.Pop);
        il.Emit(OpCodes.Ldloc_S, (byte)4);
        il.Emit(OpCodes.Call, new Func<IEnumerable<Type>, Type[]>(Enumerable.ToArray).Method);
        il.MarkLabel(onAssemblyLoadLoadedTypesIsArray);
        il.Emit(OpCodes.Stloc_S, (byte)5);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, _lock);
        il.Emit(OpCodes.Stloc_S, (byte)7);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stloc_S, (byte)8);
        il.BeginExceptionBlock();
        il.Emit(OpCodes.Ldloc_S, (byte)7);
        il.Emit(OpCodes.Ldloca_S, (byte)8);
        il.Emit(OpCodes.Call, typeof(Monitor).GetMethod(nameof(Monitor.Enter), [typeof(object), typeof(bool).MakeByRefType()])!);
        il.Emit(OpCodes.Ldloc_S, (byte)5);
        il.Emit(OpCodes.Ldloc_2);
        il.Emit(OpCodes.Ldloc_3);
        il.Emit(OpCodes.Call, LoadSreTypes);
        il.Emit(OpCodes.Ldloc_2);
        il.Emit(OpCodes.Ldfld, SreAssembly_typeDic);
        il.Emit(OpCodes.Call, SreAssembly_typeDic.FieldType.GetProperty(nameof(Dictionary<,>.Count))!.GetMethod);
        il.Emit(OpCodes.Brtrue_S, onAssemblyLoadLoadedAssemblyContainsPublicTypes);
        il.Emit(OpCodes.Leave_S, onAssemblyLoadRet);
        il.MarkLabel(onAssemblyLoadLoadedAssemblyContainsPublicTypes);
        il.Emit(OpCodes.Ldloc_1);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ldloc_1);
        il.Emit(OpCodes.Call, _assemblies.FieldType.GetProperty(nameof(List<>.Count))!.GetMethod!);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Sub);
        il.Emit(OpCodes.Ldloc_2);
        il.Emit(OpCodes.Ldsfld, s_xamlAssemblyNameComparer);
        il.Emit(OpCodes.Call, _assemblies.FieldType.GetMethod(nameof(List<>.BinarySearch), [typeof(int), typeof(int), IXamlAssembly, typeof(IComparer<>).MakeGenericType(IXamlAssembly)])!);
        il.Emit(OpCodes.Stloc_S, (byte)9);
        il.Emit(OpCodes.Ldloc_1);
        il.Emit(OpCodes.Ldloc_S, (byte)9);
        il.Emit(OpCodes.Ldloc_S, (byte)9);
        il.Emit(OpCodes.Ldc_I4_S, (sbyte)31);
        il.Emit(OpCodes.Shr);
        il.Emit(OpCodes.Xor);
        il.Emit(OpCodes.Ldloc_2);
        il.Emit(OpCodes.Call, _assemblies.FieldType.GetMethod(nameof(List<>.Insert), [typeof(int), IXamlAssembly])!);
        il.BeginFinallyBlock();
        il.Emit(OpCodes.Ldloc_S, (byte)8);
        il.Emit(OpCodes.Brfalse_S, onAssemblyLoadEndFinally);
        il.Emit(OpCodes.Ldloc_S, (byte)7);
        il.Emit(OpCodes.Call, new Action<object>(Monitor.Exit).Method);
        il.MarkLabel(onAssemblyLoadEndFinally);
        il.EndExceptionBlock();
        il.MarkLabel(onAssemblyLoadRet);
        il.Emit(OpCodes.Ret);

        //     public void Dispose()
        //         => _appDomain.AssemblyLoad -= OnAssemblyLoad;
        MethodBuilder Dispose = DynamicSreTypeSystem.DefineMethod(
            nameof(Dispose),
            MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
            typeof(void), []
        );
        il = Dispose.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, _appDomain);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldftn, OnAssemblyLoad);
        il.Emit(OpCodes.Newobj, typeof(AssemblyLoadEventHandler).GetConstructor([typeof(object), typeof(nint)])!);
        il.Emit(OpCodes.Callvirt, typeof(AppDomain).GetEvent(nameof(AppDomain.AssemblyLoad))!.RemoveMethod!);
        il.Emit(OpCodes.Ret);

        //     IEnumerable<IXamlAssembly> IXamlTypeSystem.Assemblies
        //         => new SynchronizedEnumerable<IXamlAssembly>(_assemblies, _lock);
        MethodBuilder get_Assemblies = DynamicSreTypeSystem.DefineMethod(
            $"{nameof(IXamlTypeSystem)}.{nameof(get_Assemblies)}",
            MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Virtual,
            typeof(IEnumerable<>).MakeGenericType(IXamlAssembly), []
        );
        il = get_Assemblies.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, _assemblies);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, _lock);
        il.Emit(OpCodes.Newobj, typeof(SynchronizedEnumerable<>).MakeGenericType(IXamlAssembly).GetConstructor([typeof(IReadOnlyList<>).MakeGenericType(IXamlAssembly), typeof(object)]));
        il.Emit(OpCodes.Ret);
        PropertyBuilder Assemblies = DynamicSreTypeSystem.DefineProperty($"{nameof(IXamlTypeSystem)}.{nameof(Assemblies)}", PropertyAttributes.None, get_Assemblies.ReturnType, []);
        Assemblies.SetGetMethod(get_Assemblies);
        DynamicSreTypeSystem.DefineMethodOverride(get_Assemblies, IXamlTypeSystem.GetProperty(nameof(Assemblies))!.GetMethod!);

        //     private IXamlAssembly? FindAssemblyUnsafe(string name)
        //     {
        //         List<IXamlAssembly> assemblies = _assemblies;
        //         int i = assemblies.BinarySearch(0, assemblies.Count - 1, new XamlAssemblyName(name), s_sreAssemblyNameComparer);
        //         return i >= 0 ? assemblies[i] : null;
        //     }
        MethodBuilder FindAssemblyUnsafe = DynamicSreTypeSystem.DefineMethod(
            nameof(FindAssemblyUnsafe),
            MethodAttributes.Private | MethodAttributes.HideBySig,
            IXamlAssembly, [typeof(string)]
        );
        il = FindAssemblyUnsafe.GetILGenerator();
        Label findAssemblyUnsafeFound = il.DefineLabel();
        il.DeclareLocal(_assemblies.FieldType);
        il.DeclareLocal(typeof(int));
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, _assemblies);
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Call, _assemblies.FieldType.GetProperty(nameof(List<>.Count))!.GetMethod!);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Sub);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Newobj, XamlAssemblyName.GetConstructor([typeof(string)])!);
        il.Emit(OpCodes.Ldsfld, s_xamlAssemblyNameComparer);
        il.Emit(OpCodes.Call, _assemblies.FieldType.GetMethod(nameof(List<>.BinarySearch), [typeof(int), typeof(int), IXamlAssembly, typeof(IComparer<>).MakeGenericType(IXamlAssembly)]));
        il.Emit(OpCodes.Stloc_1);
        il.Emit(OpCodes.Ldloc_1);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Bge_S, findAssemblyUnsafeFound);
        il.Emit(OpCodes.Ldnull);
        il.Emit(OpCodes.Ret);
        il.MarkLabel(findAssemblyUnsafeFound);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldloc_1);
        il.Emit(OpCodes.Call, _assemblies.FieldType.GetProperty("Item", IXamlAssembly, [typeof(int)])!.GetMethod!);
        il.Emit(OpCodes.Ret);

        //     IXamlAssembly? IXamlTypeSystem.FindAssembly(string name)
        //     {
        //         lock (_lock)
        //             return FindAssemblyUnsafe(name);
        //     }
        MethodBuilder FindAssembly = DynamicSreTypeSystem.DefineMethod(
            $"{nameof(IXamlTypeSystem)}.{nameof(FindAssembly)}",
            MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
            IXamlAssembly, [typeof(string)]
        );
        il = FindAssembly.GetILGenerator();
        Label findAssemblyEndFinally = il.DefineLabel();
        il.DeclareLocal(typeof(object));
        il.DeclareLocal(typeof(bool));
        il.DeclareLocal(IXamlAssembly);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, _lock);
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stloc_1);
        il.BeginExceptionBlock();
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldloca_S, (byte)1);
        il.Emit(OpCodes.Call, typeof(Monitor).GetMethod(nameof(Monitor.Enter), [typeof(object), typeof(bool).MakeByRefType()])!);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Call, FindAssemblyUnsafe);
        il.Emit(OpCodes.Stloc_2);
        il.BeginFinallyBlock();
        il.Emit(OpCodes.Ldloc_1);
        il.Emit(OpCodes.Brfalse_S, findAssemblyEndFinally);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Call, new Action<object>(Monitor.Exit).Method);
        il.MarkLabel(findAssemblyEndFinally);
        il.EndExceptionBlock();
        il.Emit(OpCodes.Ldloc_2);
        il.Emit(OpCodes.Ret);
        DynamicSreTypeSystem.DefineMethodOverride(FindAssembly, IXamlTypeSystem.GetMethod(nameof(FindAssembly), [typeof(string)])!);

        //     IXamlType? IXamlTypeSystem.FindType(string name, string assembly)
        //     {
        //         lock (_lock)
        //         {
        //             IXamlType? type = FindAssemblyUnsafe(assembly)?.FindType(name);
        //             if (type is not null)
        //                 return type;
        //
        //             type = _internalAssembly.FindType(name);
        //             if (type is not null && string.Equals(type.Assembly!.Name, assembly, StringComparison.InvariantCultureIgnoreCase))
        //                 return type;
        //
        //             return FindType(name, assembly);
        //         }
        //     }
        MethodBuilder FindType = DynamicSreTypeSystem.DefineMethod(
            $"{nameof(IXamlTypeSystem)}.{nameof(FindType)}",
            MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
            IXamlType, [typeof(string), typeof(string)]
        );
        il = FindType.GetILGenerator();
        Label findTypeEndFinally = il.DefineLabel();
        Label findTypeInternalFallback = il.DefineLabel();
        Label findTypeFallback = il.DefineLabel();
        Label findTypeLeave = il.DefineLabel();
        il.DeclareLocal(typeof(object));
        il.DeclareLocal(typeof(bool));
        il.DeclareLocal(IXamlType);
        il.DeclareLocal(IXamlAssembly);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, _lock);
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stloc_1);
        il.BeginExceptionBlock();
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldloca_S, (byte)1);
        il.Emit(OpCodes.Call, typeof(Monitor).GetMethod(nameof(Monitor.Enter), [typeof(object), typeof(bool).MakeByRefType()])!);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Call, FindAssemblyUnsafe);
        il.Emit(OpCodes.Stloc_3);
        il.Emit(OpCodes.Ldloc_3);
        il.Emit(OpCodes.Brfalse_S, findTypeInternalFallback);
        il.Emit(OpCodes.Ldloc_3);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Callvirt, IXamlAssembly.GetMethod(nameof(FindType), [typeof(string)])!);
        il.Emit(OpCodes.Stloc_2);
        il.Emit(OpCodes.Ldloc_2);
        il.Emit(OpCodes.Brtrue_S, findTypeLeave);
        il.MarkLabel(findTypeInternalFallback);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, _internalAssembly);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Callvirt, IXamlAssembly.GetMethod(nameof(FindType), [typeof(string)])!);
        il.Emit(OpCodes.Stloc_2);
        il.Emit(OpCodes.Ldloc_2);
        il.Emit(OpCodes.Brfalse_S, findTypeFallback);
        il.Emit(OpCodes.Ldloc_2);
        il.Emit(OpCodes.Callvirt, IXamlType.GetProperty(nameof(Assembly))!.GetMethod!);
        il.Emit(OpCodes.Callvirt, IXamlAssembly.GetProperty(nameof(AssemblyName.Name))!.GetMethod!);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Ldc_I4_3);
        il.Emit(OpCodes.Call, new Func<string, string, StringComparison, bool>(string.Equals).Method);
        il.Emit(OpCodes.Brtrue_S, findTypeLeave);
        il.MarkLabel(findTypeFallback);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Call, SreTypeSystem.GetMethod(nameof(FindType), [typeof(string), typeof(string)])!);
        il.Emit(OpCodes.Stloc_2);
        il.MarkLabel(findTypeLeave);
        il.BeginFinallyBlock();
        il.Emit(OpCodes.Ldloc_1);
        il.Emit(OpCodes.Brfalse_S, findTypeEndFinally);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Call, new Action<object>(Monitor.Exit).Method);
        il.MarkLabel(findTypeEndFinally);
        il.EndExceptionBlock();
        il.Emit(OpCodes.Ldloc_2);
        il.Emit(OpCodes.Ret);
        DynamicSreTypeSystem.DefineMethodOverride(FindType, IXamlTypeSystem.GetMethod(nameof(FindType), [typeof(string), typeof(string)])!);

        //     IXamlType? IXamlTypeSystem.FindType(string name)
        //     {
        //         // Types usually reside in namespaces whose names are derived from the assemblies in which they are defined.
        //         // With that in mind, we can optimize type lookup by starting the search not with the first assembly in our
        //         // list, but with the one whose name is most similar to the first part of the type's namespace.
        //         //
        //         // For example, given "Newtonsoft.Json.JsonConverter", we would first look for an assembly named "Newtonsoft".
        //         // Since it doesn't exist, a binary search would land us on "Newtonsoft.Json" instead, which would be
        //         // an immediate match.
        //         // In cases where a type's namespace does not share any similarities with its host assembly,
        //         // we would simply scan all assemblies in a slightly awkward order:
        //         // (X..N, 0..X) instead of the naive (0..N) - but that's about it.
        //         List<IXamlAssembly> assemblies = _assemblies;
        //         XamlAssemblyName assemblyName = new(name.Substring(0, (int)Math.Min((uint)name.IndexOf('.'), (uint)name.Length)));
        //         lock (_lock)
        //         {
        //             int end = assemblies.Count - 1;
        //             int start = assemblies.BinarySearch(0, end, assemblyName, s_sreAssemblyNameComparer);
        //             start ^= start >> 31;
        //
        //             // This loop needs to be unrolled.
        //             // It's written this way because it will be easier for me to implement in ILAsm.
        //             for (int loopIndex = 1; loopIndex >= 0; loopIndex--)
        //             {
        //                 for (int i = start; i < end; i++)
        //                 {
        //                     IXamlType? type = assemblies[i].FindType(name);
        //                     if (type is not null)
        //                         return type;
        //                 }
        //                 (start, end) = (0, start);
        //             }
        //
        //             return _internalAssembly.FindType(name);
        //         }
        //     }
        FindType = DynamicSreTypeSystem.DefineMethod(
            $"{nameof(IXamlTypeSystem)}.{nameof(FindType)}",
            MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
            IXamlType, [typeof(string)]
        );
        il = FindType.GetILGenerator();
        Label findType2OuterLoopHead = il.DefineLabel();
        Label findType2InnerLoopHead = il.DefineLabel();
        Label findType2InnerLoopCheck = il.DefineLabel();
        Label findType2Leave = il.DefineLabel();
        Label findType2EndFinally = il.DefineLabel();
        il.DeclareLocal(_assemblies.FieldType);
        il.DeclareLocal(XamlAssemblyName);
        il.DeclareLocal(typeof(object));
        il.DeclareLocal(typeof(bool));
        il.DeclareLocal(typeof(int));
        il.DeclareLocal(typeof(int));
        il.DeclareLocal(typeof(int));
        il.DeclareLocal(typeof(int));
        il.DeclareLocal(IXamlType);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, _assemblies);
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldc_I4_S, (sbyte)'.');
        il.Emit(OpCodes.Callvirt, typeof(string).GetMethod(nameof(string.IndexOf), [typeof(char)])!);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Callvirt, typeof(string).GetProperty(nameof(string.Length))!.GetMethod!);
        il.Emit(OpCodes.Call, new Func<uint, uint, uint>(Math.Min).Method);
        il.Emit(OpCodes.Callvirt, typeof(string).GetMethod(nameof(string.Substring), [typeof(int), typeof(int)])!);
        il.Emit(OpCodes.Newobj, XamlAssemblyName.GetConstructor([typeof(string)])!);
        il.Emit(OpCodes.Stloc_1);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, _lock);
        il.Emit(OpCodes.Stloc_2);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stloc_3);
        il.BeginExceptionBlock();
        il.Emit(OpCodes.Ldloc_2);
        il.Emit(OpCodes.Ldloca_S, (byte)3);
        il.Emit(OpCodes.Call, typeof(Monitor).GetMethod(nameof(Monitor.Enter), [typeof(object), typeof(bool).MakeByRefType()])!);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Call, _assemblies.FieldType.GetProperty(nameof(List<>.Count))!.GetMethod!);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Sub);
        il.Emit(OpCodes.Stloc_S, (byte)4);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ldloc_S, (byte)4);
        il.Emit(OpCodes.Ldloc_1);
        il.Emit(OpCodes.Ldsfld, s_xamlAssemblyNameComparer);
        il.Emit(OpCodes.Call, _assemblies.FieldType.GetMethod(nameof(List<>.BinarySearch), [typeof(int), typeof(int), IXamlAssembly, typeof(IComparer<>).MakeGenericType(IXamlAssembly)]));
        il.Emit(OpCodes.Dup);
        il.Emit(OpCodes.Ldc_I4_S, (sbyte)31);
        il.Emit(OpCodes.Shr);
        il.Emit(OpCodes.Xor);
        il.Emit(OpCodes.Stloc_S, (byte)5);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Stloc_S, (byte)6);
        il.MarkLabel(findType2OuterLoopHead);
        il.Emit(OpCodes.Ldloc_S, (byte)5);
        il.Emit(OpCodes.Stloc_S, (byte)7);
        il.Emit(OpCodes.Br_S, findType2InnerLoopCheck);
        il.MarkLabel(findType2InnerLoopHead);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldloc_S, (byte)7);
        il.Emit(OpCodes.Call, _assemblies.FieldType.GetProperty("Item", IXamlAssembly, [typeof(int)])!.GetMethod!);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Callvirt, IXamlAssembly.GetMethod(nameof(FindType), [typeof(string)])!);
        il.Emit(OpCodes.Stloc_S, (byte)8);
        il.Emit(OpCodes.Ldloc_S, (byte)8);
        il.Emit(OpCodes.Brtrue_S, findType2Leave);
        il.Emit(OpCodes.Ldloc_S, (byte)7);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Stloc_S, (byte)7);
        il.MarkLabel(findType2InnerLoopCheck);
        il.Emit(OpCodes.Ldloc_S, (byte)7);
        il.Emit(OpCodes.Ldloc_S, (byte)4);
        il.Emit(OpCodes.Blt_S, findType2InnerLoopHead);
        il.Emit(OpCodes.Ldloc_S, (byte)5);
        il.Emit(OpCodes.Stloc_S, (byte)4);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stloc_S, (byte)5);
        il.Emit(OpCodes.Ldloc_S, (byte)6);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Sub);
        il.Emit(OpCodes.Stloc_S, (byte)6);
        il.Emit(OpCodes.Ldloc_S, (byte)6);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Bge_S, findType2OuterLoopHead);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, _internalAssembly);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Callvirt, IXamlAssembly.GetMethod(nameof(FindType), [typeof(string)])!);
        il.Emit(OpCodes.Stloc_S, (byte)8);
        il.MarkLabel(findType2Leave);
        il.BeginFinallyBlock();
        il.Emit(OpCodes.Ldloc_3);
        il.Emit(OpCodes.Brfalse_S, findType2EndFinally);
        il.Emit(OpCodes.Ldloc_2);
        il.Emit(OpCodes.Call, new Action<object>(Monitor.Exit).Method);
        il.MarkLabel(findType2EndFinally);
        il.EndExceptionBlock();
        il.Emit(OpCodes.Ldloc_S, (byte)8);
        il.Emit(OpCodes.Ret);
        DynamicSreTypeSystem.DefineMethodOverride(FindType, IXamlTypeSystem.GetMethod(nameof(FindType), [typeof(string)])!);

        //     public DynamicSreTypeSystem(AppDomain appDomain, AssemblyBuilder host)
        //         // Note that we do NOT call the base constructor on this type,
        //         // because SreTypeSystem performs its own costly initialization,
        //         // which is completely useless for us.
        //         //
        //         // And yeah, this semantic is inaccessible from C#.
        //         // Good thing we're not implementing this in C# then, right?
        //         : object()
        //     {
        //         // Pre-sort the assemblies to (hopefully) minimize the number of times
        //         // `_assemblies` needs to insert a new instance into itself somehwere
        //         // in the middle, shifting all existing elements.
        //         Assembly[] loadedAssemblies = appDomain.GetAssemblies();
        //         Array.Sort(loadedAssemblies, new Comparison<Assembly>(CompareAssemblyNames));
        //
        //         _appDomain = appDomain;
        //         _host = host;
        //         _assemblies = new(loadedAssemblies.Length * 2);
        //         _typeDic = new(loadedAssemblies.Length * 10);
        //         _lock = new();
        //         _internalAssembly = CreateSreAssembly(host, this);
        //         _assemblies.Add(_internalAssembly);
        //
        //         appDomain.AssemblyLoad += OnAssemblyLoad;
        //         for (int i = 0; i < loadedAssemblies.Length; i++)
        //             OnAssemblyLoad(appDomain, new(loadedAssemblies[i]));
        //     }
        ConstructorBuilder ctor = DynamicSreTypeSystem.DefineConstructor(
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            CallingConventions.HasThis,
            [typeof(AppDomain), typeof(AssemblyBuilder)]
        );
        il = ctor.GetILGenerator();
        Label ctorLoopHead = il.DefineLabel();
        Label ctorLoopCheck = il.DefineLabel();
        il.DeclareLocal(typeof(Assembly[]));
        il.DeclareLocal(typeof(int));
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(object).GetConstructor([])!);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Callvirt, typeof(AppDomain).GetMethod(nameof(AppDomain.GetAssemblies))!);
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldnull);
        il.Emit(OpCodes.Ldftn, CompareAssemblyNames);
        il.Emit(OpCodes.Newobj, typeof(Comparison<Assembly>).GetConstructor([typeof(object), typeof(nint)]));
        il.Emit(OpCodes.Call, new Action<Assembly[], Comparison<Assembly>>(Array.Sort).Method);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stfld, _appDomain);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Stfld, _host);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldlen);
        il.Emit(OpCodes.Conv_I4);
        il.Emit(OpCodes.Ldc_I4_2);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Newobj, _assemblies.FieldType.GetConstructor([typeof(int)])!);
        il.Emit(OpCodes.Stfld, _assemblies);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldlen);
        il.Emit(OpCodes.Conv_I4);
        il.Emit(OpCodes.Ldc_I4_S, (sbyte)10);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Newobj, _typeDic.FieldType.GetConstructor([typeof(int)])!);
        il.Emit(OpCodes.Stfld, _typeDic);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Newobj, typeof(object).GetConstructor([])!);
        il.Emit(OpCodes.Stfld, _lock);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, CreateSreAssembly);
        il.Emit(OpCodes.Stfld, _internalAssembly);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, _assemblies);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, _internalAssembly);
        il.Emit(OpCodes.Call, _assemblies.FieldType.GetMethod(nameof(List<>.Add), [IXamlAssembly])!);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldftn, OnAssemblyLoad);
        il.Emit(OpCodes.Newobj, typeof(AssemblyLoadEventHandler).GetConstructor([typeof(object), typeof(nint)])!);
        il.Emit(OpCodes.Callvirt, typeof(AppDomain).GetEvent(nameof(AppDomain.AssemblyLoad))!.AddMethod!);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stloc_1);
        il.Emit(OpCodes.Br_S, ctorLoopCheck);
        il.MarkLabel(ctorLoopHead);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldloc_1);
        il.Emit(OpCodes.Ldelem_Ref);
        il.Emit(OpCodes.Newobj, typeof(AssemblyLoadEventArgs).GetConstructor([typeof(Assembly)])!);
        il.Emit(OpCodes.Call, OnAssemblyLoad);
        il.Emit(OpCodes.Ldloc_1);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Stloc_1);
        il.MarkLabel(ctorLoopCheck);
        il.Emit(OpCodes.Ldloc_1);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldlen);
        il.Emit(OpCodes.Conv_I4);
        il.Emit(OpCodes.Blt_S, ctorLoopHead);
        il.Emit(OpCodes.Ret);

        //     static DynamicSreTypeSystem()
        //     {
        //         s_xamlAssemblyNameComparer = Comparer<IXamlAssembly>.Create(new Comparison<IXamlAssembly>(CompareXamlAssemblyNames));
        //     }
        Type xamlAssemblyComparison = typeof(Comparison<>).MakeGenericType(IXamlAssembly);
        ConstructorBuilder cctor = DynamicSreTypeSystem.DefineConstructor(
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Static,
            CallingConventions.Standard,
            []
        );
        il = cctor.GetILGenerator();
        il.Emit(OpCodes.Ldnull);
        il.Emit(OpCodes.Ldftn, CompareXamlAssemblyNames);
        il.Emit(OpCodes.Newobj, xamlAssemblyComparison.GetConstructor([typeof(object), typeof(nint)])!);
        il.Emit(OpCodes.Call, s_xamlAssemblyNameComparer.FieldType.GetMethod(nameof(Comparer<>.Create), [xamlAssemblyComparison])!);
        il.Emit(OpCodes.Stsfld, s_xamlAssemblyNameComparer);
        il.Emit(OpCodes.Ret);

        // }
        return DynamicSreTypeSystem.CreateTypeInfo();
    }

    /// <summary>
    /// Generates the <c>XamlX.TypeSystem.XamlAssemblyName</c> type.
    /// </summary>
    /// <param name="module">The module where the type should be defined.</param>
    /// <param name="IXamlAssembly">The <c>XamlX.TypeSystem.IXamlAssembly</c> type.</param>
    /// <param name="IXamlType">The <c>XamlX.TypeSystem.IXamlType</c> type.</param>
    /// <param name="IXamlCustomAttribute">The <c>XamlX.TypeSystem.IXamlCustomAttribute</c> type.</param>
    /// <returns>The generated <c>XamlX.TypeSystem.XamlAssemblyName</c> type.</returns>
    [MethodImpl(MethodImplOptions.NoOptimization)]
    private static Type CreateXamlAssemblyNameType(ModuleBuilder module, Type IXamlAssembly, Type IXamlType, Type IXamlCustomAttribute)
    {
        string fullName = "XamlX.TypeSystem.XamlAssemblyName";
        Type? existingType = module.Assembly.GetType(fullName, throwOnError: false);
        if (existingType is not null)
            return existingType;

        // public sealed class XamlAssemblyName : IXamlAssembly
        // {
        ILGenerator il = null!;
        TypeBuilder XamlAssemblyName = module.DefineType(fullName, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class);
        XamlAssemblyName.AddInterfaceImplementation(IXamlAssembly);
        XamlAssemblyName.AddInterfaceImplementation(typeof(IEquatable<>).MakeGenericType(IXamlAssembly));

        //     private readonly string _name;
        FieldBuilder _name = XamlAssemblyName.DefineField(nameof(_name), typeof(string), FieldAttributes.Private | FieldAttributes.InitOnly);

        //     public XamlAssemblyName(string name)
        //     {
        //         _name = name;
        //     }
        ConstructorBuilder ctor = XamlAssemblyName.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.HasThis, [typeof(string)]);
        il = ctor.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(object).GetConstructor([])!);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stfld, _name);
        il.Emit(OpCodes.Ret);

        //     public string Name => _name;
        MethodBuilder get_Name = XamlAssemblyName.DefineMethod(nameof(get_Name), MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Virtual, typeof(string), []);
        il = get_Name.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, _name);
        il.Emit(OpCodes.Ret);
        PropertyBuilder Name = XamlAssemblyName.DefineProperty(nameof(Name), PropertyAttributes.None, get_Name.ReturnType, []);
        Name.SetGetMethod(get_Name);

        //     IReadOnlyList<IXamlCustomAttribute> CustomAttributes => [];
        MethodBuilder get_CustomAttributes = XamlAssemblyName.DefineMethod(nameof(get_CustomAttributes), MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Virtual, typeof(IReadOnlyList<>).MakeGenericType(IXamlCustomAttribute), []);
        il = get_CustomAttributes.GetILGenerator();
        il.Emit(OpCodes.Call, typeof(Array).GetMethod(nameof(Array.Empty), []).MakeGenericMethod(IXamlCustomAttribute));
        il.Emit(OpCodes.Ret);
        PropertyBuilder CustomAttributes = XamlAssemblyName.DefineProperty(nameof(CustomAttributes), PropertyAttributes.None, get_CustomAttributes.ReturnType, []);
        CustomAttributes.SetGetMethod(get_CustomAttributes);

        //     public IXamlType? FindType(string fullName) => null;
        MethodBuilder FindType = XamlAssemblyName.DefineMethod(nameof(FindType), MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, IXamlType, [typeof(string)]);
        il = FindType.GetILGenerator();
        il.Emit(OpCodes.Ldnull);
        il.Emit(OpCodes.Ret);

        //     public bool Equals(IXamlAssembly? other) => string.Equals(_name, other?.Name);
        MethodBuilder Equals = XamlAssemblyName.DefineMethod(nameof(Equals), MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, typeof(bool), [IXamlAssembly]);
        il = Equals.GetILGenerator();
        Label equalsRet = il.DefineLabel();
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Brfalse_S, equalsRet);
        il.Emit(OpCodes.Pop);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, _name);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Callvirt, IXamlAssembly.GetProperty(nameof(AssemblyName.Name))!.GetMethod!);
        il.Emit(OpCodes.Call, new Func<string, string, bool>(string.Equals).Method);
        il.MarkLabel(equalsRet);
        il.Emit(OpCodes.Ret);

        // }
        return XamlAssemblyName.CreateTypeInfo();
    }
}
