# HotAvalonia

[![GitHub Build Status](https://img.shields.io/github/actions/workflow/status/Kira-NT/HotAvalonia/build.yml?logo=github)](https://github.com/Kira-NT/HotAvalonia/actions/workflows/build.yml)
[![Version](https://img.shields.io/github/v/release/Kira-NT/HotAvalonia?sort=date&label=version)](https://github.com/Kira-NT/HotAvalonia/releases/latest)
[![License](https://img.shields.io/github/license/Kira-NT/HotAvalonia?cacheSeconds=36000)](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/LICENSE.md)

<img alt="HotAvalonia Icon" src="https://raw.githubusercontent.com/Kira-NT/HotAvalonia/HEAD/media/icon.png" width="128">

`HotAvalonia` is an IDE-agnostic hot reload plugin for [Avalonia](https://github.com/AvaloniaUI/Avalonia) that lets you see UI changes in real time as you edit XAML files, drastically accelerating your design and development workflow. Supports Linux, macOS, Windows, and Android.

----

## Installation

Add the following package reference to your project file *(e.g., `.csproj`, `.fsproj`, `.vbproj`)*:

```xml
<PackageReference Include="HotAvalonia" Version="3.*" PrivateAssets="All" Publish="True" />
```

That's it. Simply start debugging your app as you normally would, make changes to some XAML files, and see them being applied in real time. Enjoy!

If you have a multi-project setup, `HotAvalonia` must be installed into your startup project *(the one that produces the final executable)*. It is also highly recommended to install it in every project that contains Avalonia controls to ensure the most stable hot reload experience possible.

`HotAvalonia` is a development-only dependency, meaning it doesn't affect your Release builds in any way, shape, or form, and its binaries are not shipped with your application.

----

## Optional Dependencies

`HotAvalonia` has two optional dependencies:

 - [`Avalonia.Markup.Xaml.Loader`](https://nuget.org/packages/Avalonia.Markup.Xaml.Loader) - The official Avalonia package responsible for runtime XAML parsing. To accommodate users of all Avalonia releases starting from v11.0.0, HotAvalonia ships with a pretty old version of this library. This can cause issues if you are experimenting with nightly builds of the next major Avalonia release, in which case you may need to manually bump the dependency:
    ```xml
    <PackageReference Include="Avalonia.Markup.Xaml.Loader" Version="$(AvaloniaVersion)" PrivateAssets="All" />
    ```
 - [`MonoMod.RuntimeDetour`](https://nuget.org/packages/MonoMod.RuntimeDetour) - If you want to be able to hot reload embedded assets such as icons and images, HotAvalonia needs a way to perform method injections into optimized code, and this is what this library is for. Without it, hot reload support is limited to XAML files only.
    ```xml
    <PackageReference Include="MonoMod.RuntimeDetour" Version="*" PrivateAssets="All" />
    ```

If you have a multi-project setup, these packages only need to be referenced from your startup project.

----

## MSBuild Properties

`HotAvalonia` is highly configurable, and many of its features can be adjusted via MSBuild properties directly in your project file.

Below is a non-exhaustive list of the most common and useful properties:

```xml
<PropertyGroup>
  <!-- Specifies whether HotAvalonia is enabled.                                          -->
  <!-- Please, do not enable it unconditionally.                                          -->
  <!--                                                                                    -->
  <!-- Default: true if the current configuration is Debug; otherwise, false.             -->
  <!-- Options: true, false, enable, disable                                              -->
  <HotAvalonia>$(IsDebug)</HotAvalonia>

  <!-- Specifies how aggressively HotAvalonia reloads your app when it detects an update. -->
  <!--                                                                                    -->
  <!-- - Minimal:    Only the affected control is reloaded.                               -->
  <!--                                                                                    -->
  <!-- - Balanced:   HotAvalonia attempts to identify controls that depend on the updated -->
  <!--               instance and reloads them as well.                                   -->
  <!--                                                                                    -->
  <!-- - Aggressive: The entire app is reloaded on every change.                          -->
  <!--                                                                                    -->
  <!-- Default: Balanced                                                                  -->
  <!-- Options: Minimal, Balanced, Aggressive                                             -->
  <HotAvaloniaMode>Balanced</HotAvaloniaMode>

  <!-- Defines a hotkey used to trigger an app-wide hot reload event.                     -->
  <!--                                                                                    -->
  <!-- Default: Alt+F5                                                                    -->
  <!-- Options: F11, Ctrl+R, Ctrl+Shift+H, disable, ...                                   -->
  <HotAvaloniaHotkey>Alt+F5</HotAvaloniaHotkey>

  <!-- Specifies the default timeout, in milliseconds, for hot reload-related operations. -->
  <!--                                                                                    -->
  <!-- Default: 10000                                                                     -->
  <!-- Options: 30000, 5000, 1000, 0, disable, ...                                        -->
  <HotAvaloniaTimeout>10000</HotAvaloniaTimeout>

  <!-- Specifies whether hot reload should be enabled automatically or if the user should -->
  <!-- manually call .UseHotReload() on their AppBuilder instance.                        -->
  <!--                                                                                    -->
  <!-- Default: true if the current project is a startup project; otherwise, false.       -->
  <!-- Options: true, false, enable, disable                                              -->
  <HotAvaloniaAutoEnable>$(IsExe)</HotAvaloniaAutoEnable>

  <!-- Specifies whether HotAvalonia should connect to a remote file system instead of    -->
  <!-- querying the local one.                                                            -->
  <!--                                                                                    -->
  <!-- Default: true for Android, iOS, and browser builds; otherwise, false.              -->
  <!-- Options: true, false, enable, disable                                              -->
  <HotAvaloniaRemote>$(IsNotDesktop)</HotAvaloniaRemote>

  <!-- Specifies whether method injections are allowed in the current environment.        -->
  <!--                                                                                    -->
  <!-- Default: true                                                                      -->
  <!-- Options: true, false, enable, disable                                              -->
  <HotAvaloniaInjections>enable</HotAvaloniaInjections>

  <!-- Specifies whether known XAML patches should be applied to all controls on load.    -->
  <!-- May negatively affect cold-start times.                                            -->
  <!--                                                                                    -->
  <!-- Default: true                                                                      -->
  <!-- Options: true, false, enable, disable                                              -->
  <HotAvaloniaInitialPatching>enable</HotAvaloniaInitialPatching>
</PropertyGroup>
```

Additionally, when `<HotAvaloniaRemote>` is `true` *(which is usually the case if you are developing for a non-desktop platform such as Android, iOS, or the browser)*, you may want to configure the remote file system component known as [HARFS](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/src/HotAvalonia.Remote) - for example, if you want to run your own server instead of relying on `HotAvalonia` to start one automatically.

```xml
<PropertyGroup>
  <!-- Specifies the address of the machine hosting the app's source code.                -->
  <!--                                                                                    -->
  <!-- Default: The IPv4 address of your machine within the local network.                -->
  <!-- Options: 192.168.0.42, 127.0.0.1, 10.0.2.2, ...                                    -->
  <HarfsAddress>$(CurrentLocalIpAddress)</HarfsAddress>

  <!-- Specifies the port for HARFS to listen on for new client connections.              -->
  <!--                                                                                    -->
  <!-- Default: 0 (any currently available TCP port)                                      -->
  <!-- Options: 0, 20158, ...                                                             -->
  <HarfsPort>0</HarfsPort>

  <!-- Specifies the secret used by HARFS to authenticate new clients.                    -->
  <!--                                                                                    -->
  <!-- Note:    The secret does not need to be a valid UTF-8 string.                      -->
  <!--          For an arbitrary sequence of bytes, you can use <HarfsSecretBase64>.      -->
  <!-- Default: A new secret is generated each time you rebuild the app.                  -->
  <!-- Options: password, qwerty, 12345, ...                                              -->
  <HarfsSecret>$(RandomlyGeneratedSecret)</HarfsSecret>

  <!-- Specifies the path to the X.509 certificate file used to secure connections with   -->
  <!-- new clients.                                                                       -->
  <!--                                                                                    -->
  <!-- Default: A new self-signed certificate is generated each time you rebuild the app. -->
  <!-- Options: /etc/ssl/certs/harfs.pfx, ...                                             -->
  <HarfsCertificateFile>$(RandomlyGeneratedSelfSignedCertificate)</HarfsCertificateFile>

  <!-- Specifies the maximum allowed period of inactivity before HARFS shuts down.        -->
  <!--                                                                                    -->
  <!-- Default: 300000 (5 minutes)                                                        -->
  <!-- Options: -1, 10000, 2147483647, ...                                                -->
  <HarfsTimeout>300000</HarfsTimeout>

  <!-- Specifies whether HARFS should shut down as soon as its first client disconnects.  -->
  <!--                                                                                    -->
  <!-- Default: true                                                                      -->
  <!-- Options: true, false                                                               -->
  <HarfsExitOnDisconnect>true</HarfsExitOnDisconnect>
</PropertyGroup>
```

As mentioned earlier, this list is far from exhaustive. For a complete overview of all available options, please refer to the [`HotAvalonia.targets`](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/src/HotAvalonia/HotAvalonia.targets) file.

---

## Advanced Features

[`HotAvalonia.Core`](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/src/HotAvalonia.Core) and [`HotAvalonia.Extensions`](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/src/HotAvalonia.Extensions), shipped with the main [`HotAvalonia`](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/src/HotAvalonia) package, provide quite a few niche features of their own that can be fine-tuned to your liking. So, if you are interested in learning more about their internals, please refer to their respective READMEs. There are, however, some features I would like to highlight here as well.

### InitializeComponentState & [AvaloniaHotReload]

Hot reloading a control with complex initialization logic may leave it in an awkward state because its constructor is not actually being re-run. As a result, if you capture layout-dependent state in fields, for example, those fields may contain stale values after a hot reload event.

To address this, you can move layout-sensitive initialization logic into a parameterless instance method named `InitializeComponentState`, which HotAvalonia will automatically re-run on a hot reload event.

If you prefer a different method name or want to re-run more than one method, you can apply the special `[AvaloniaHotReload]` attribute to any number of parameterless instance methods on your control, and all such methods will be invoked when a hot reload event occurs.

```diff
  using Avalonia.Controls;
+ using HotAvalonia;

  public partial class FooControl : UserControl
  {
      public FooControl()
      {
          InitializeComponent();
          InitializeComponentState();
      }

      // If a method is named "InitializeComponentState", you do not
      // strictly need to apply an attribute to it, as it will be
      // picked up by HotAvalonia automatically.
+     [AvaloniaHotReload]
      private void InitializeComponentState()
      {
          // Code to initialize or refresh
          // the control during hot reload.
      }
  }
```

----

## FAQ

> Is this an official Avalonia project?

No, this project is not affiliated with Avalonia nor endorsed by it.

This is a one-girl-army effort to improve the framework's DX by providing the community with a free and open-source hot reload solution. The Avalonia team has repeatedly stated that they are not currently interested in spending their limited resources on implementing something this complex, and even if they ever do come around to it, it is planned to be a closed-source, paid feature for participants in the [Accelerate](https://avaloniaui.net/accelerate) program.

<br/>

> Which platforms does HotAvalonia support?

As stated at the top of the README, HotAvalonia officially supports Linux, macOS, Windows, and Android. That means if you have any problems on any of those platforms, feel free to open an issue!

Hot reload may also work on other platforms, such as FreeBSD and iOS; however, I haven't had a chance to test them, and I won't spend much time fixing issues on those platforms if they do arise.

<br/>

> What about browser support?

HotAvalonia doesn't support hot reload in the browser *yet*.

<br/>

> Does it support Visual Studio / Visual Studio Code / Rider / Vim / NeoVim / Sublime / Notepad / Yet another "revolutionary" fork of VS Code with "AI" slop built into it / Whatever else?

Yes.

<br/>

> It doesn't seem to work with Visual Studio Code Dev Containers on Windows.

As discussed in [#44](https://github.com/Kira-NT/HotAvalonia/issues/44), when you mount parts of the Windows filesystem into a Linux Docker container, the original filesystem events are not translated into `inotify` events. As a result, HotAvalonia cannot detect that a change has occurred.

To mitigate this problem, you need to move your project into the WSL2 filesystem, which is capable of raising filesystem events.

<br/>

> Do you accept donations?

Oh, thank you for asking! :3

Unfortunately, no, I don't have any way to accept donations at the moment, but that might change in the future.

----

## Examples

| ![Hot Reload: App](https://raw.githubusercontent.com/Kira-NT/HotAvalonia/HEAD/media/examples/hot_reload_app.gif) | ![Hot Reload: User Control](https://raw.githubusercontent.com/Kira-NT/HotAvalonia/HEAD/media/examples/hot_reload_user_control.gif) |
| :---: | :-----: |
| ![Hot Reload: View](https://raw.githubusercontent.com/Kira-NT/HotAvalonia/HEAD/media/examples/hot_reload_view.gif) | ![Hot Reload: Styles](https://raw.githubusercontent.com/Kira-NT/HotAvalonia/HEAD/media/examples/hot_reload_styles.gif) |
| ![Hot Reload: Resources](https://raw.githubusercontent.com/Kira-NT/HotAvalonia/HEAD/media/examples/hot_reload_resources.gif) | ![Hot Reload: Window](https://raw.githubusercontent.com/Kira-NT/HotAvalonia/HEAD/media/examples/hot_reload_window.gif) |

----

## License

Licensed under the terms of the [MIT License](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/LICENSE.md).
