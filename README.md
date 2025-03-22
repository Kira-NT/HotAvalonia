# HotAvalonia

[![GitHub Build Status](https://img.shields.io/github/actions/workflow/status/Kira-NT/HotAvalonia/build.yml?logo=github)](https://github.com/Kira-NT/HotAvalonia/actions/workflows/build.yml)
[![Version](https://img.shields.io/github/v/release/Kira-NT/HotAvalonia?sort=date&label=version)](https://github.com/Kira-NT/HotAvalonia/releases/latest)
[![License](https://img.shields.io/github/license/Kira-NT/HotAvalonia?cacheSeconds=36000)](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/LICENSE.md)

<img alt="HotAvalonia Icon" src="https://raw.githubusercontent.com/Kira-NT/HotAvalonia/HEAD/media/icon.png" width="128">

`HotAvalonia` is a hot reload plugin for Avalonia that enables you to see UI changes in real time as you edit XAML files, drastically accelerating your design and development workflow.

----

## NuGet Packages

| **Package** | **Latest Version** |
|:------------|:-------------------|
| HotAvalonia | [![NuGet](https://img.shields.io/nuget/v/HotAvalonia?logo=nuget&label=nuget)](https://nuget.org/packages/HotAvalonia/ "Download HotAvalonia from NuGet.org") |
| HotAvalonia.Core | [![NuGet](https://img.shields.io/nuget/v/HotAvalonia.Core?logo=nuget&label=nuget)](https://nuget.org/packages/HotAvalonia.Core/ "Download HotAvalonia.Core from NuGet.org") |
| HotAvalonia.Extensions | [![NuGet](https://img.shields.io/nuget/v/HotAvalonia.Extensions?logo=nuget&label=nuget)](https://nuget.org/packages/HotAvalonia.Extensions/ "Download HotAvalonia.Extensions from NuGet.org") |
| HotAvalonia.Fody | [![NuGet](https://img.shields.io/nuget/v/HotAvalonia.Fody?logo=nuget&label=nuget)](https://nuget.org/packages/HotAvalonia.Fody/ "Download HotAvalonia.Fody from NuGet.org") |

----

## Installation

To instantly enable hot reload for Debug builds of your application, simply add the [`HotAvalonia`](https://nuget.org/packages/HotAvalonia) package to your startup project *(i.e., the project that produces the executable)* by inserting the following snippet into your project file *(e.g., `.csproj`, `.fsproj`, `.vbproj`, etc.)*:

```xml
<PackageReference Include="HotAvalonia" Version="..." PrivateAssets="All" Publish="True" />
```

If you're developing a mobile app, please double-check that `Publish="True"` is set. Otherwise, Debug builds of your app will immediately crash on startup due to an old .NET SDK bug (dotnet/sdk#47332).

If you have a multi-project setup, it is **highly recommended** to add `HotAvalonia` to **every** project that contains Avalonia controls. You can either do this manually for each project, or include a `PackageReference` to `HotAvalonia` in your [`Directory.Build.targets`](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory).

`HotAvalonia` is a development-only dependency, meaning it doesn't affect your Release builds in any way, shape, or form, and its binaries are **not** shipped with your application.

----

## Examples

Here are some examples demonstrating `HotAvalonia` in action:

| ![Hot Reload: App](https://raw.githubusercontent.com/Kira-NT/HotAvalonia/HEAD/media/examples/hot_reload_app.gif) | ![Hot Reload: User Control](https://raw.githubusercontent.com/Kira-NT/HotAvalonia/HEAD/media/examples/hot_reload_user_control.gif) |
| :---: | :-----: |
| ![Hot Reload: View](https://raw.githubusercontent.com/Kira-NT/HotAvalonia/HEAD/media/examples/hot_reload_view.gif) | ![Hot Reload: Styles](https://raw.githubusercontent.com/Kira-NT/HotAvalonia/HEAD/media/examples/hot_reload_styles.gif) |
| ![Hot Reload: Resources](https://raw.githubusercontent.com/Kira-NT/HotAvalonia/HEAD/media/examples/hot_reload_resources.gif) | ![Hot Reload: Window](https://raw.githubusercontent.com/Kira-NT/HotAvalonia/HEAD/media/examples/hot_reload_window.gif) |

To try it out yourself, you can run the [`samples/HotReloadDemo.Desktop`](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/samples/HotReloadDemo.Desktop) and/or [`samples/HotReloadDemo.Android`](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/samples/HotReloadDemo.Android) applications included in the repository.

----

## Usage

`HotAvalonia` is designed in such a way that you usually don't need to do anything else after installation - it just works™. However, here are some tips and tricks for more advanced users who want to tweak their hot reload experience:

### [AvaloniaHotReload]

If you want to refresh a control's state during a hot reload, you can apply the `[AvaloniaHotReload]` attribute to one or more parameterless instance methods of the said control. For example:

```diff
  using Avalonia.Controls;
+ using HotAvalonia;

  public partial class FooControl : UserControl
  {
      public FooControl()
      {
          InitializeComponent();
          Initialize();
      }

+     [AvaloniaHotReload]
      private void Initialize()
      {
          // Code to initialize or refresh
          // the control during hot reload.
      }
  }
```

### 'StaticResource'

When you reference a static resource, Avalonia effectively inlines its value, turning it into a constant. As a result, hot reloading a resource dictionary, for example, from which the value originally came **will not** affect any `StaticResource` definitions - you would need to hot reload the document containing them for those constant values to be recomputed.

However, if you're actively working on something referenced this way, having to hot reload two documents at a time just to see the changes can be pretty annoying. To make this a bit more convenient, you can use a special `'StaticResource'` syntax like this:

```xml
<TextBox Watermark="{'StaticResource' Watermark}" />
```

This syntax is valid because Avalonia's parser allows quoting parts of your queries. However, since no sane person would ever quote `StaticResource` under normal circumstances, `HotAvalonia` treats it as a virtue signal to dynamically replace it with a `DynamicResource` at runtime.

With this trick, your production builds will retain the intended semantics of optimized and inlined static resources, while your debug builds empowered by `HotAvalonia` will convert those resources into dynamic ones, making hot reloading them much more convenient.

### MSBuild Properties

`HotAvalonia` is highly configurable, and lots of its options can be adjusted via MSBuild properties directly in your project file *(e.g., `.csproj`, `.fsproj`, `.vbproj`)*, like so:

```xml
<PropertyGroup>
  <HotAvaloniaLite>enable</HotAvaloniaLite>
</PropertyGroup>
```

Below is a non-exhaustive list of the most common and useful properties supported by `HotAvalonia`:

| Name | Description | Default | Examples |
| :--- | :---------- | :------ | :------: |
| `HotAvalonia` | Specifies whether `HotAvalonia` is enabled in the current environment. <br><br> Please, do **not** enable `HotAvalonia` unconditionally. | `true` if current configuration is `Debug`; otherwise, `false`. | `enable` <br> `disable` <br> `true` <br> `false` |
| `HotAvaloniaRemote` | Specifies whether the app will be executed on a remote device *(e.g., when running the app in an emulator)*. | `true` for Android, iOS, and browser builds; otherwise, `false`. | `enable` <br> `disable` <br> `true` <br> `false` |
| `HotAvaloniaLite` | Specifies whether `HotAvalonia` should disable some of its more resource-intensive features, such as automatic hot reload of images and icons. | The same value as `HotAvaloniaRemote`. | `enable` <br> `disable` <br> `true` <br> `false` |
| `HotAvaloniaIncludeExtensions` | Specifies whether the `AvaloniaHotReloadExtensions` class should be included in the current project. <br><br> This class provides extension methods like `.UseHotReload()` necessary to enable hot reload for your application. | `true` if the current project is a startup project; otherwise, `false`. | `true` <br> `false` |
| `HotAvaloniaAutoEnable` | Specifies whether hot reload should be enabled automatically or if the user should manually call `.UseHotReload()` on their `AppBuilder` instance. | `true` if the current project is a startup project; otherwise, `false`. | `true` <br> `false` |
| `HotAvaloniaRecompileResources` | Specifies whether `HotAvalonia` should recompile resources like styles and resource dictionaries to make them hot-reloadable even on non-x64 devices. | `true` | `true` <br> `false` |
| `HotAvaloniaGeneratePathResolver` | Specifies whether `HotAvalonia` should generate a project path resolver during compile-time based on your solution file *(i.e., `.sln`)*, or if it should search for source project locations during runtime. <br><br> Resolving project paths at runtime may be more reliable in some situations, but it's also a tiny bit more time-demanding solution. | `true` | `true` <br> `false` |

Also, if you are using hot reload on a remote device *(for example, if you are developing a mobile app)*, there are some additional options to configure [`HotAvalonia.Remote`](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/src/HotAvalonia.Remote) *(also known as `HotAvalonia Remote File System`, or just `HARFS` for short)*:

| Name | Description | Default | Examples |
| :--- | :---------- | :------ | :------- |
| `HarfsAddress` | Specifies the address of the machine that hosts the source code of the app *(i.e., the machine on which you built the application)*. | The IPv4 address of your machine within the local network. | `192.168.0.42` |
| `HarfsFallbackAddress` | Specifies a fallback address of the machine hosting the source code, in case `HotAvalonia` cannot resolve your computer's local network address. | `10.0.2.2` if the target device is an Android emulator; otherwise, `127.0.0.1`. | `127.0.0.1` |
| `HarfsLocalAddress` | Specifies the address for HARFS to listen on for new client connections. | `0.0.0.0` <br> *(all available network interfaces)* | `192.168.0.42` |
| `HarfsPort` | Specifies the port for HARFS to listen on for new client connections. | `0` <br> *(any currently available TCP port)* | `20158` |
| `HarfsSecret` | Specifies the secret used by HARFS to authenticate new clients. | - | `My Super Secret Value` |
| `HarfsSecretBase64` | Specifies the secret used by HARFS to authenticate new clients in the form of a Base64 string. | A new secret is generated each time you run your app. | `TXkgU3VwZXIgU2VjcmV0IFZhbHVl` |
| `HarfsCertificateFile` | Specifies a path to the X.509 certificate file for securing connections with new clients. | A new self-signed certificate is generated each time you run your app. | `/etc/ssl/certs/harfs.pfx` |
| `HarfsMaxSearchDepth` | Specifies how many files HARFS can return in a recursive directory search. | `256` | `-1` <br> `10` <br> `2147483647` |
| `HarfsTimeout` | Specifies a timeout (in milliseconds) for HARFS to shut down if no clients connect within the specified time window. | `300000` <br> *(5 minutes)* | `-1` <br> `10000` <br> `2147483647` |
| `HarfsExitOnDisconnect` | Specifies whether HARFS should shut down as soon as its primary client *(that being your app)* disconnects. | `true` | `true` <br> `false` |

As mentioned earlier, this list is far from exhaustive. For a complete overview of available options, please check out the [`HotAvalonia.targets`](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/src/HotAvalonia/HotAvalonia.targets) file. However, note that any properties not listed here *(especially those prefixed with `_`)* are considered internal and may be renamed, replaced, or completely removed without notice. So, feel free to experiment with those, but do not rely on them for long-term compatibility.

### Optional Dependencies

To enhance your experience with `HotAvalonia`, you may want to install the following optional dependencies:

| Name | Description | Snippet |
| :--- | :---------- | :------ |
| `Avalonia.Markup.Xaml.Loader` | HotAvalonia relies on this official Avalonia package to compile XAML during runtime.<br><br>To accommodate users of all Avalonia releases starting from v11.0.0, HotAvalonia includes a pretty old version of `Avalonia.Markup.Xaml.Loader`. So, you might want to bump it explicitly in your project file. | `<PackageReference Include="Avalonia.Markup.Xaml.Loader" Version="*" PrivateAssets="All" />` |
| `MonoMod.RuntimeDetour` | If this package is installed, HotAvalonia will automatically switch to injection-based hot reload, enabling some of its more advanced features, such as asset hot reloading, which are otherwise disabled. | `<PackageReference Include="MonoMod.RuntimeDetour" Version="*" PrivateAssets="All" />` |

### Miscellaneous

[`HotAvalonia.Core`](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/src/HotAvalonia.Core) and [`HotAvalonia.Extensions`](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/src/HotAvalonia.Extensions), provided by the main [`HotAvalonia`](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/src/HotAvalonia) package, can be fine-tuned even further. For more details about their internals, please refer to their respective `README`s.

----

## License

Licensed under the terms of the [MIT License](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/LICENSE.md).
