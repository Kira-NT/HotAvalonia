> [!IMPORTANT]
>
> You probably don't want to install this package manually.
> Please, check out the main [`HotAvalonia`](https://nuget.org/packages/HotAvalonia) package, which will set everything up for you automatically.

# HotAvalonia.Core

[![GitHub Build Status](https://img.shields.io/github/actions/workflow/status/Kira-NT/HotAvalonia/build.yml?logo=github)](https://github.com/Kira-NT/HotAvalonia/actions/workflows/build.yml)
[![Version](https://img.shields.io/github/v/release/Kira-NT/HotAvalonia?sort=date&label=version)](https://github.com/Kira-NT/HotAvalonia/releases/latest)
[![License](https://img.shields.io/github/license/Kira-NT/HotAvalonia?cacheSeconds=36000)](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/LICENSE.md)

`HotAvalonia.Core` is a library that provides the core components necessary to enable XAML hot reload for Avalonia apps.

----

## Installation

This library is a bit tricky to set up, because it has two shadow dependencies:

 - [`Avalonia.Markup.Xaml.Loader`](https://nuget.org/packages/Avalonia.Markup.Xaml.Loader) *(required)* - The official Avalonia package responsible for runtime XAML parsing. Note that its version must match the version of the core [`Avalonia`](https://nuget.org/packages/Avalonia) package used by your application for everything to work properly.
 - [`MonoMod.RuntimeDetour`](https://nuget.org/packages/MonoMod.RuntimeDetour) *(optional)* - If present, `HotAvalonia` uses its capabilities to perform injection-based hot reload.

To install `HotAvalonia.Core`, run the following commands:

```sh
dotnet add package HotAvalonia.Core
dotnet add package Avalonia.Markup.Xaml.Loader
# optional: dotnet add package MonoMod.RuntimeDetour
```

Alternatively, you can manually add these `PackageReference` entries to your project file:

```xml
<PackageReference Include="HotAvalonia.Core" Version="..." PrivateAssets="All" />
<PackageReference Include="Avalonia.Markup.Xaml.Loader" Version="..." PrivateAssets="All" />

<!-- This one is optional: -->
<!-- <PackageReference Include="MonoMod.RuntimeDetour" Version="*" PrivateAssets="All" /> -->
```

----

## Usage

### AvaloniaHotReloadContext

`AvaloniaHotReloadContext` provides a simple way to create a hot reload context based on your needs:

 - `.FromAssembly(...)` / `.FromControl(...)` - If you only need to hot reload precompiled XAML contained within a single assembly, use one of these methods.
 - `.FromAppDomain(...)` - If you need to enable XAML hot reload for all assemblies *(past, present, and future)* within a given `AppDomain`, consider this overload.
 - `.ForAssets(...)` - `HotAvalonia` also supports hot-reloading assets embedded in the application *(e.g., images, icons, etc.)*. However, the `IHotReloadContext` instance returned by this method requires injection-based hot reload to be enabled. Thus, `MonoMod.RuntimeDetour` must be available in your app's context, and the `HOTAVALONIA_DISABLE_INJECTIONS` environment variable should **not** be set to `1` or `true`.

So, to enable full hot reload for your app, you might do something like this:

```csharp
// Don't forget to store your hot reload context somewhere, so it doesn't get GCed out of existence!
_context = AvaloniaHotReloadContext.FromAppDomain().Combine(AvaloniaHotReloadContext.ForAssets());
_context.EnableHotReload();
```

### AvaloniaProjectLocator

For `HotAvalonia` to work, it must know where the source code for the project that produced a given control assembly is located. Usually, it can deduce this information from the corresponding `.pdb` file. However, if the `.pdb` is unavailable, the project is located on a remote device *(more on that later)*, or it's in a different location than indicated by the `.pdb`, you can tweak the project path resolution logic using this class and one of its `.AddHint(...)` overloads:

```csharp
AvaloniaProjectLocator locator = new AvaloniaProjectLocator();

// If you have a materialized `Assembly` object, prefer this overload over other ones:
locator.AddHint(typeof(MyControl).Assembly, "/home/user/projects/MyProject/src/MyProject");

// If you don't have an `Assembly` instance (for example, if it has not been loaded yet),
// but you do know its name (the one returned by `assembly.GetName().Name`), you can use
// it instead:
locator.AddHint("MyProject", "/home/user/projects/MyProject/src/MyProject");

// For full control over the project path resolution logic, use the overload that accepts a `Func<Assembly, string?>`.
// If you recognize the assembly passed to your callback, return the path to its source project location.
// Otherwise, return `null` and let HotAvalonia handle it.
locator.AddHint(assembly => assembly.GetName()?.Name == "MyProject" ? "/home/user/projects/MyProject/src/MyProject" : null);
```

Please note that `HotAvalonia` requires **a path to the root of the project** *(i.e., where your `.*proj` file is located)* that was compiled into the provided assembly, **not** the location of the assembly itself. So, it is not as simple as `assembly => assembly.Location`!

### FileSystem

You can supply `HotAvalonia` with a custom file system accessor in case you scenario requires one. Here are some common options:

 - `FileSystem.Empty` - Does exactly as advertised, i.e., absolutely nothing. Although you typically wouldn't use this one if you want a working hot reload, it works well as a fallback when your preferred `IFileSystem` instance cannot be created.
 - `FileSystem.Current` - This is the default file system accessor used by `HotAvalonia`, as the source files for your projects are usually located on the same machine where you debug your app *(at least when we talk about desktop development)*.
 - `FileSystem.Connect(...)` / `FileSystem.ConnectAsync(...)` - When developing for mobile platforms, your apps are typically run in an emulator or on a physical device connected to your PC *(unless you're a psychopath who develops Android apps directly on an Android device; if that's the case, please seek professional help)*. Even if `HotAvalonia` knows the paths of the source projects on your development machine, these paths are inaccessible from the device where the app is being executed. To solve this problem, you can start a simple read-only file system server on your development machine *(see [`HotAvalonia.Remote`](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/src/HotAvalonia.Remote) for more details)* and then connect to that server from within your app using one of the mentioned overloads.

Now it's time to bring everything we've learnt so far together - here's how you can provide a custom file system accessor to a hot reload context:

```csharp
// Connect to a file system server.
IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse("192.168.0.42"), 20158);
byte[] secret = Encoding.UTF8.GetBytes("My Super Secret Value");
IFileSystem fileSystem = FileSystem.Connect(endpoint, secret, fallbackFileSystem: FileSystem.Empty);

// Create a project locator using that file system accessor, and add path hints if needed.
AvaloniaProjectLocator locator = new AvaloniaProjectLocator(fileSystem);
locator.AddHint(assembly => assembly.GetName()?.Name == "MyProject" ? "/home/user/projects/MyProject/src/MyProject" : null);

// Provide your customized project locator to the hot reload context factories.
IHotReloadContext appDomainContext = AvaloniaHotReloadContext.FromAppDomain(AppDomain.Current, locator);
IHotReloadContext assetContext = AvaloniaHotReloadContext.ForAssets(AvaloniaServiceProvider.Current, locator);

// Finally, enable your combined hot reload context.
_context = appDomainContext.Combine(assetContext);
_context.EnableHotReload();
```

### HotAvalonia.Xaml

If you need or want to dig deeper, you might be interested in the `HotAvalonia.Xaml` namespace, which contains some of the most important primitives powering the underlying XAML hot reload mechanism. Here's a pretty basic overview of those:

 - `XamlDocument` - Serves as input for the `XamlCompiler` to produce a `CompiledXamlDocument`.
 - `CompiledXamlDocument` - Contains all the relevant information about a control compiled from a XAML document, including its URI and the type of its root element. It also provides methods to build a new control instance defined by the document from scratch, or to (re)populate an existing one.
 - `XamlCompiler` - Compiles the XAML documents you supply it with.
 - `XamlScanner` - Offers several utility methods to assist with processing XAML documents precompiled by Avalonia. For example, you can use this class to extract all Avalonia controls from a given assembly.
 - `XamlPatcher` - Some features of Avalonia *(e.g., `MergeResourceInclude`)* are designed to optimize your app's production build performance by inlining external documents into the current one. This allows you to enjoy the best of both worlds: code separation and decent performance, as you no longer need to instantiate 100 different resource dictionaries at runtime just to include them in a single control once. However, this approach makes hot reload somewhat tricky for components referenced in this way, as their source XAML is effectively inlined into another document. As a result, changes to them do not directly affect the app. To see the updates, you would need to hot reload the document into which they were inlined - an unintuitive *(and to be quite frankly honest with you, just annoying)* experience for end users. To address this, XAML patchers have been introduced into the codebase. These patchers replace inlinable declarations with their less efficient but semantically identical counterparts *(e.g., `MergeResourceInclude` is replaced with `ResourceInclude`)*. At the end of the day, this is a debug build we're talking about, so efficiency isn't a priority here, but a good development experience is.

### Environment Variables

Some features of this library can be toggled via environment variables:

| Name | Description | Default | Examples |
| :--- | :---------- | :------ | :------: |
| `HOTAVALONIA_DISABLE_INJECTIONS` | Forces `HotAvalonia` to ignore `MonoMod.RuntimeDetour`, if present, thereby disabling injection-based hot reload. | `false` | `true` <br> `false` <br> `1` <br> `0` |
| `HOTAVALONIA_LOG_LEVEL_OVERRIDE` | Overrides `HotAvalonia`'s base log level, promoting all logs to a higher severity. For example, setting it to `error` will make even debug logs appear as errors. | N/A | `information` <br> `warning` <br> `error` |
| `HOTAVALONIA_SKIP_INITIAL_PATCHING` | Hot reload contexts created by this library automatically patch and reload **all** controls that need patching upon being enabled. If this behavior causes issues for you, you can disable it using this variable. | `false` | `true` <br> `false` <br> `1` <br> `0` |
| `HOTAVALONIA_STATICRESOURCEPATCHER` | Enables the XAML patcher that replaces `'StaticResource'` declarations with `'DynamicResource'` ones. | `true` | `true` <br> `false` <br> `1` <br> `0` |
| `HOTAVALONIA_MERGERESOURCEINCLUDEPATCHER` | Enables the XAML patcher that replaces `MergeResourceInclude` declarations with `ResourceInclude` ones. | `true` | `true` <br> `false` <br> `1` <br> `0` |

----

## License

Licensed under the terms of the [MIT License](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/LICENSE.md).
