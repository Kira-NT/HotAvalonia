# HotAvalonia.HostCompiler

[![GitHub Build Status](https://img.shields.io/github/actions/workflow/status/Kira-NT/HotAvalonia/build.yml?logo=github)](https://github.com/Kira-NT/HotAvalonia/actions/workflows/build.yml)
[![Version](https://img.shields.io/github/v/release/Kira-NT/HotAvalonia?sort=date&label=version)](https://github.com/Kira-NT/HotAvalonia/releases/latest)
[![License](https://img.shields.io/github/license/Kira-NT/HotAvalonia?cacheSeconds=36000)](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/LICENSE.md)

`HotAvalonia.HostCompiler` compiles changed Avalonia XAML views on the **host** into standalone "populate" assemblies that a device loads and applies. It enables hot reload on targets where XAML can't be recompiled at runtime (no `Reflection.Emit`) — most notably **iOS**.

It pairs with HotAvalonia's `UseHostCompiledXaml` mode: the host watches your `.axaml` files, compiles each change into a `<view>.axaml.hotreload.dll` sidecar next to the source, and HotAvalonia serves it to the device, which loads it and drives the view's `!XamlIlPopulateOverride` — no runtime codegen on the device.

----

## Usage

```
Usage:
  HotAvalonia.HostCompiler compile <view.axaml> (--app-project <csproj> | --closure <dir> --avalonia-version <ver>) [options]
  HotAvalonia.HostCompiler watch <views-dir>    (--app-project <csproj> | --closure <dir> --avalonia-version <ver>) [options]

Inputs (auto-discovered from --app-project, or given explicitly):
  --app-project <csproj>  App project (or its directory); infers --closure, --avalonia-version and --tfm.
  --closure <dir>         The app's pre-link build-output directory (the device's reference closure).
  --avalonia-version <v>  The exact Avalonia package version the app uses.

Options:
  --tfm <tfm>             Target framework of the compile project (default: net10.0).
  --output <path>         Output DLL path for 'compile' (default: <view>.axaml.hotreload.dll).
  --exclude <glob>        File-name glob excluded from the reference closure (repeatable; overrides defaults).
```

In the common case, `--app-project` is all you need: the tool reads the app's restore/build output (`obj/project.assets.json` + `bin`) to discover the reference closure, the exact Avalonia version, and the target framework. The app must be built once first so that output exists.

----

## How it works

For a changed view, the tool:

1. strips `x:Class` and qualifies each `clr-namespace` with its defining assembly (resolved from the closure via `System.Reflection.Metadata`), so types bind the device's exact identities;
2. compiles the view with Avalonia's exact build-time XAML compiler, referencing the app's **pre-link** reference closure;
3. emits a `<view>.axaml.hotreload.dll` sidecar (written atomically).

The device side (`HotAvalonia.Core`, with `UseHostCompiledXaml`) loads that assembly, reflects out its `Build:`/`Populate:` methods, and applies them through Avalonia's `!XamlIlPopulateOverride` hook — the same emit-free path used for compiled-XAML reload.

It's a dev-time host helper; nothing ships in Release builds.

----

## License

Licensed under the terms of the [MIT License](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/LICENSE.md).
