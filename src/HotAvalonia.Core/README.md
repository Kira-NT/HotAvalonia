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

This library has two shadow dependencies:

 - [`Avalonia.Markup.Xaml.Loader`](https://nuget.org/packages/Avalonia.Markup.Xaml.Loader) *(required)* - The official Avalonia package responsible for runtime XAML parsing. Note that its version should match the version of the core [`Avalonia`](https://nuget.org/packages/Avalonia) package used by your application for everything to work correctly.
 - [`MonoMod.RuntimeDetour`](https://nuget.org/packages/MonoMod.RuntimeDetour) *(optional)* - If present, `HotAvalonia.Core` uses its capabilities to perform injection-based hot reload, required for it to be able to hot reload embedded assets such as icons and images.

So, to install `HotAvalonia.Core`, you need to add something like this to your project file:

```xml
<PackageReference Include="HotAvalonia.Core" Version="..." PrivateAssets="All" />
<PackageReference Include="Avalonia.Markup.Xaml.Loader" Version="..." PrivateAssets="All" />
<!-- <PackageReference Include="MonoMod.RuntimeDetour" Version="*" PrivateAssets="All" /> -->
```

----

## Usage

### AvaloniaHotReloadContext

`AvaloniaHotReloadContext` provides a simple way to create a hot reload context tailored to your needs:

 - `.FromAssembly(...)` - Use this method when you only need to hot reload precompiled XAML contained within a single assembly.
 - `.FromAppDomain(...)` - Use this overload to enable XAML hot reload for all assemblies within an `AppDomain`.
 - `.ForAssets(...)` - `HotAvalonia.Core` also supports hot-reloading assets embedded in the application *(e.g., images, icons, etc.)*. However, the `IHotReloadContext` instance returned by this method requires injection-based hot reload to be enabled. Thus, `MonoMod.RuntimeDetour` must be available in your app's context.

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
 - `FileSystem.Connect(...)` - When developing for mobile platforms, your apps are typically run in an emulator or on a physical device connected to your PC. Even if `HotAvalonia` knows the paths of the source projects on your development machine, these paths are inaccessible from the device where the app is being executed. To solve this problem, you can start a simple read-only file system server on your development machine *(see [`HotAvalonia.Remote`](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/src/HotAvalonia.Remote) for more details)* and then connect to that server from within your app using one of the mentioned overloads.

Now it's time to bring everything we've learnt so far together - here's how you can provide a custom file system accessor to a hot reload context:

```csharp
// Connect to a file system server.
IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse("192.168.0.42"), 20158);
byte[] secret = Encoding.UTF8.GetBytes("My Super Secret Value");
IFileSystem fileSystem = FileSystem.Connect(endpoint, secret, fallbackFileSystem: FileSystem.Empty);

// Create a hot reload config.
AvaloniaHotReloadConfig config = AvaloniaHotReloadConfig.Default with { FileSystem = fileSystem };

// Add path hints if needed.
config.ProjectLocator.AddHint(assembly => assembly.GetName().Name == "MyProject" ? "/home/user/projects/MyProject/src/MyProject" : null);

// Provide your customized config to the hot reload context factories.
IHotReloadContext appDomainContext = AvaloniaHotReloadContext.FromAppDomain(config);
IHotReloadContext assetContext = AvaloniaHotReloadContext.ForAssets(config);

// Finally, enable your combined hot reload context.
_context = appDomainContext.Combine(assetContext);
_context.EnableHotReload();
```

### HotAvalonia.Xaml

If you need or want to dig deeper, you might be interested in the `HotAvalonia.Xaml` namespace, which contains some of the most important primitives powering the underlying XAML hot reload mechanism. Here's a pretty basic overview of those:

 - `XamlDocument` - Serves as an input for the `XamlCompiler` to produce a `CompiledXamlDocument`.
 - `CompiledXamlDocument` - Contains all the relevant information about a control compiled from a XAML document, including its URI and the type of its root element. It also provides methods to build a new control instance defined by the document from scratch, or to (re)populate an existing one.
 - `XamlCompiler` - Compiles the XAML documents you supply it with.
 - `XamlScanner` - Offers several utility methods to assist with processing XAML documents precompiled by Avalonia. For example, you can use this class to extract all Avalonia controls from a given assembly.
 - `XamlPatcher` - Some features of Avalonia *(e.g., `MergeResourceInclude`)* are designed to optimize your app's production build performance by inlining external documents into the current one. This allows you to enjoy the best of both worlds: code separation and decent performance, as you no longer need to instantiate 100 different resource dictionaries at runtime just to include them in a single control once. However, this approach makes hot reload somewhat tricky for components referenced in this way, as their source XAML is effectively inlined into another document. As a result, changes to them do not directly affect the app. To see the updates, you would need to hot reload the document into which they were inlined - an unintuitive *(and to be quite frankly honest with you, just annoying)* experience for end users. To address this, XAML patchers have been introduced into the codebase. These patchers replace inlinable declarations with their less efficient but semantically identical counterparts *(e.g., `MergeResourceInclude` is replaced with `ResourceInclude`)*. At the end of the day, this is a debug build we're talking about, so efficiency isn't a priority here, but a good development experience is.

### Feature Flags

Some features of this library can be toggled via runtime options and environment variables *(the latter taking precedence over the former)*. Note that higher-level libraries such as [`HotAvalonia`](https://nuget.org/packages/HotAvalonia) provide their own user-friendly wrappers for these features in the form of MSBuild properties, so you will likely want to use those instead of manually setting runtime configuration options.

This section primarily serves to document the raw behavior of `HotAvalonia.Core`, both for those who may want to build an alternative front end for it and to highlight that you can use environment variables to quickly experiment with the outlined features without recompiling your app or messing with its `*.runtimeconfig.json`.

Below is the exhaustive list of available runtime options and their default values:

```xml
<ItemGroup>
    <!-- @env:  HOTAVALONIA_INJECTION_TYPE                                                      -->
    <!-- flags: None, MonoMod, CodeCave, PointerSwap                                            -->
    <!--========================================================================================-->
    <!-- Specifies the set of allowed injection techniques.                                     -->
    <!--                                                                                        -->
    <!-- You can use any of the following flags individually or in conjunction with each other. -->
    <!-- If none of the selected injection techniques are supported by the current environment, -->
    <!-- injections will be automatically disabled.                                             -->
    <!--                                                                                        -->
    <!-- - MonoMod:     The only injection technique that allows hot reloading embedded assets. -->
    <!--                Requires MonoMod.RuntimeDetour to be installed.                         -->
    <!--                Supports Linux, macOS, and Windows on x86 and x86-64.                   -->
    <!--                                                                                        -->
    <!-- - CodeCave:    An architecture-dependent injection technique.                          -->
    <!--                Supports all modern desktop runtimes on x86, x86-64, and arm64.         -->
    <!--                                                                                        -->
    <!-- - PointerSwap: A somewhat buggy but fairly fast injection technique.                   -->
    <!--                Supports all desktop runtimes prior to .NET 9.                          -->
    <!--                                                                                        -->
    <!-- - None:        Signifies that injections are disabled.                                 -->
    <RuntimeHostConfigurationOption Include="HotAvalonia.InjectionType" Value="MonoMod, CodeCave, PointerSwap" />

    <!-- @env:   HOTAVALONIA_SKIP_INITIAL_PATCHING                                              -->
    <!-- values: true, false, 1, 0                                                              -->
    <!--========================================================================================-->
    <!-- By default, whenever a hot reload context is initialized, it automatically applies all -->
    <!-- active XAML patchers to all controls it manages.                                       -->
    <!-- While this improves the hot reload experience and its reliability, it also incurs      -->
    <!-- a noticeable cost to the app's cold-start time.                                        -->
    <!-- In scenarios where you need to frequently restart the application during testing,      -->
    <!-- you may want to disable this costly operation. Hence this switch. -->
    <RuntimeHostConfigurationOption Include="HotAvalonia.SkipInitialPatching" Value="false" />

    <!-- @env:   HOTAVALONIA_MIN_LOG_LEVEL                                                      -->
    <!-- values: debug, information, warning, error                                             -->
    <!--========================================================================================-->
    <!-- Overrides HotAvalonia's base log level, promoting all its logs to a higher severity.   -->
    <!-- For example, setting it to "error" will cause even debug logs to appear as errors.     -->
    <!-- This is useful for debugging HotAvalonia itself without lowering the overall log       -->
    <!-- threshold for the entire app.                                                          -->
    <RuntimeHostConfigurationOption Include="HotAvalonia.MinLogLevel" Value="debug" />

    <!-- @env:   HOTAVALONIA_TIMEOUT                                                            -->
    <!-- values: 0, 5000, 10000, ...                                                            -->
    <!--========================================================================================-->
    <!-- Specifies the default timeout, in milliseconds, for hot reload-related operations.     -->
    <!-- Non-positive values (<=0) are treated as an infinite timeout.                          -->
    <RuntimeHostConfigurationOption Include="HotAvalonia.Timeout" Value="10000" />

    <!-- @env:   HOTAVALONIA_MODE                                                               -->
    <!-- values: minimal, balanced, aggressive                                                  -->
    <!--========================================================================================-->
    <!-- Specifies how aggressively HotAvalonia reloads your app when it detects an update.     -->
    <!--                                                                                        -->
    <!-- - Minimal:    Only the affected control is reloaded. While this may seem like the most -->
    <!--               logical approach, it does not always work well. For example, reloading   -->
    <!--               a stylesheet instance will not update controls that reference its values -->
    <!--               as static resources.                                                     -->
    <!--                                                                                        -->
    <!-- - Balanced:   HotAvalonia attempts to identify controls that depend on the updated     -->
    <!--               instance and reloads them as well.                                       -->
    <!--                                                                                        -->
    <!-- - Aggressive: The entire app is reloaded on every change. This mode is the most        -->
    <!--               reliable in terms of results, but also the least responsive due to       -->
    <!--               the amount of work involved.                                             -->
    <RuntimeHostConfigurationOption Include="HotAvalonia.Mode" Value="balanced" />

    <!-- @env:   HOTAVALONIA_HOTKEY                                                             -->
    <!-- values: F11, Ctrl+R, Ctrl+Shift+H, 0, false, ...                                       -->
    <!--========================================================================================-->
    <!-- Defines a hotkey used to trigger an app-wide hot reload event.                         -->
    <!-- The literal values "0" and "false" can be used to disable this feature.                -->
    <RuntimeHostConfigurationOption Include="HotAvalonia.Hotkey" Value="Alt+F5" />

    <!-- @env:   HOTAVALONIA_REMOTE_FILE_SYSTEM_ADDRESS                                         -->
    <!-- values: 192.168.0.42:20158, [::0]:12345, ...                                           -->
    <!--========================================================================================-->
    <!-- Specifies the address of the remote file system that HotAvalonia needs to connect to.  -->
    <RuntimeHostConfigurationOption Include="HotAvalonia.RemoteFileSystemAddress" Value="" />

    <!-- @env:   HOTAVALONIA_REMOTE_FILE_SYSTEM_SECRET                                          -->
    <!-- values: SGVsbG8sIHdvcmxkIQ==, , ...                                                    -->
    <!--========================================================================================-->
    <!-- Specifies the secret key required to access the remote file system,                    -->
    <!-- provided as a Base64-encoded string.                                                   -->
    <RuntimeHostConfigurationOption Include="HotAvalonia.RemoteFileSystemSecret" Value="" />

    <!-- @env:   HOTAVALONIA_STATIC_RESOURCE_PATCHER                                            -->
    <!-- values: true, false, 1, 0                                                              -->
    <!--========================================================================================-->
    <!-- Enables or disables a XAML patcher that replaces all instances of "'StaticResource'"   -->
    <!-- in your XAML with "'DynamicResource'" (including the single quotes). This is useful in -->
    <!-- conjunction with the Minimal hot reload mode, where you may want to automatically      -->
    <!-- convert some static resources to dynamic ones during a debugging session, while        -->
    <!-- preserving the StaticResource semantics for the final release build.                   -->
    <RuntimeHostConfigurationOption Include="HotAvalonia.StaticResourcePatcher" Value="true" />

    <!-- @env:   HOTAVALONIA_MERGE_RESOURCE_INCLUDE_PATCHER                                     -->
    <!-- values: true, false, 1, 0                                                              -->
    <!--========================================================================================-->
    <!-- Enables or disables a XAML patcher that replaces all instances of                      -->
    <!-- the <MergeResourceInclude> element in your XAML with its <ResourceInclude> counterpart.-->
    <!-- Without it, it would be impossible to hot reload controls that contain this element.   -->
    <RuntimeHostConfigurationOption Include="HotAvalonia.MergeResourceIncludePatcher" Value="true" />
</ItemGroup>
```

----

## License

Licensed under the terms of the [MIT License](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/LICENSE.md).
