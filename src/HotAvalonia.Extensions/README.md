> [!IMPORTANT]
>
> You probably don't want to install this package manually.
> Please, check out the main [`HotAvalonia`](https://nuget.org/packages/HotAvalonia) package, which will set everything up for you automatically.

# HotAvalonia.Extensions

[![GitHub Build Status](https://img.shields.io/github/actions/workflow/status/Kira-NT/HotAvalonia/build.yml?logo=github)](https://github.com/Kira-NT/HotAvalonia/actions/workflows/build.yml)
[![Version](https://img.shields.io/github/v/release/Kira-NT/HotAvalonia?sort=date&label=version)](https://github.com/Kira-NT/HotAvalonia/releases/latest)
[![License](https://img.shields.io/github/license/Kira-NT/HotAvalonia?cacheSeconds=36000)](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/LICENSE.md)

`HotAvalonia.Extensions` is a companion library for [HotAvalonia](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/src/HotAvalonia) that provides extension methods for `Avalonia.AppBuilder` and `Avalonia.Application`, designed to make it easy to enable or disable hot reload for your application.

----

## Usage

This library is distributed in the form of source code, intended to be included in the compilation context of consuming projects. While this approach makes it quite a bit less portable, since the same logic must be re-implemented again and again across different languages *(e.g., C#, F#, VB)*, it is also the best possible solution for our goals: it allows for a dynamic, easily extensible codebase that consumers can use and modify as needed, with or without referencing [`HotAvalonia.Core`](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/src/HotAvalonia.Core), depending on their context.

### AvaloniaHotReloadExtensions

This class provides extension methods to enable or disable hot reload for an Avalonia application.

The preferred way to enable `HotAvalonia` is by calling the `.UseHotReload()` extension method on your `AppBuilder` instance. For example:

```diff
  using Avalonia;
  using Avalonia.ReactiveUI;
+ using HotAvalonia;

  static class Program
  {
      [STAThread]
      public static void Main(string[] args) => BuildAvaloniaApp()
          .StartWithClassicDesktopLifetime(args);

      public static AppBuilder BuildAvaloniaApp()
          => AppBuilder.Configure<App>()
+             .UseHotReload()
              .UsePlatformDetect()
              .WithInterFont()
              .LogToTrace()
              .UseReactiveUI();
  }
```

However, for legacy reasons, there is also `.EnableHotReload()`, which can be called on an `Application` instance. This method is **discouraged**, because it shifts the responsibility for enabling hot reload from the startup project to a control library that defines the application class *(in case you have a multi-project solution)*, and thus is prone to user errors. That said, here's how it was supposed to be used before `.UseHotReload()` made its way into the library:

```diff
  using Avalonia;
  using Avalonia.Controls.ApplicationLifetimes;
  using Avalonia.Markup.Xaml;
+ using HotAvalonia;

  public partial class App : Application
  {
      public override void Initialize()
      {
+         this.EnableHotReload(); // Ensure this line **precedes** `AvaloniaXamlLoader.Load(this);`
          AvaloniaXamlLoader.Load(this);
      }

      public override void OnFrameworkInitializationCompleted()
      {
          // ...
      }
  }
```

The behavior of this class is heavily influenced by compiler constants, which you can define via the `DefineConstants` property in your project file. The available options are:

| Name | Description |
| :--- | :---------- |
| `HOTAVALONIA_EXCLUDE_EXTENSIONS` | If defined, the `AvaloniaHotReloadExtensions` class is **not** emitted at all. <br><br> This is useful if you only need `AvaloniaHotReloadAttribute` in your current project. |
| `HOTAVALONIA_ENABLE` | If defined, `AvaloniaHotReloadExtensions` assumes the current project references [`HotAvalonia.Core`](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/src/HotAvalonia.Core), thereby transforming `.UseHotReload()`, `.EnableHotReload()`, and `.DisableHotReload()` from no-op stubs into methods that actually do what they advertise. <br><br> This mechanism allows consuming projects to reference [`HotAvalonia.Core`](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/src/HotAvalonia.Core) conditionally, such as only for Debug builds, making it possible to completely strip out hot reload functionality from the Release builds. |
| `HOTAVALONIA_DISABLE` | Overrides `HOTAVALONIA_ENABLE`, effectively un-defining it. |
| `HOTAVALONIA_ENABLE_LITE` | If defined, the hot reload context is created using `AvaloniaHotReloadContext.CreateLite()` instead of `AvaloniaHotReloadContext.Create()`. |
| `HOTAVALONIA_USE_REMOTE_FILE_SYSTEM` | If defined, HotAvalonia uses a file system accessor created via `FileSystem.Connect()` instead of defaulting to `FileSystem.Current`. |
| `HOTAVALONIA_USE_CUSTOM_FILE_SYSTEM` | If defined, you must provide a custom implementation of `AvaloniaHotReloadExtensions.GetFileSystem()`. |

The class definition of this type is marked as `partial`, allowing you to extend it with your own methods if needed. For example:

```csharp
namespace HotAvalonia;

partial class AvaloniaHotReloadExtensions
{
    // If `HOTAVALONIA_USE_CUSTOM_FILE_SYSTEM` is defined,
    // you can supply HotAvalonia with a custom file system accessor.
    internal static IFileSystem GetFileSystem() => new MyCustomFileSystem();

    // `HotAvalonia.Fody` looks for this method to pass it
    // to the `.UseHotReload()` call emitted by the auto-enable feature.
    //
    // You don't really need to implement it. This is just a way for
    // you to easily override HotAvalonia's project path resolution
    // logic, which is useful in case your setup is so esoteric that
    // it struggles to locate your application's sources automatically.
    //
    // The returned path should point to the **root of the project** that
    // produced the provided assembly, and **not** the assembly location itself.
    internal static string? ResolveProjectPath(Assembly assembly) => assembly.GetName()?.Name switch
    {
        "MyProject" => "/home/user/projects/MyProject/src/MyProject",
        "MyProject.Desktop" => "/home/user/projects/MyProject/src/MyProject.Desktop",
        _ => null
    };
}
```

### AvaloniaHotReloadAttribute

The `AvaloniaHotReloadAttribute` can be used to mark parameterless instance methods of controls, enabling them to act as hot reload callbacks. For example:

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

The attribute's class definition is marked as `partial`, allowing you to modify and extend it if needed:

```csharp
namespace HotAvalonia;

partial class AvaloniaHotReloadAttribute
{
    public string Reason { get; }

    // Force consumers to provide a reason why the method
    // should be treated as a hot reload callback.
    public AvaloniaHotReloadAttribute(string reason) => Reason = reason;
}
```

----

## License

Licensed under the terms of the [MIT License](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/LICENSE.md).
