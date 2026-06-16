# Host-compiled hot reload (iOS) — quick start

On iOS, XAML can't be compiled on-device (no runtime `Reflection.Emit`). HotAvalonia handles this by
**compiling each changed view on the host** into a `<view>.axaml.hotreload.dll` sidecar that the device
loads and applies. Two host-side processes make that work:

- **HARFS** (`HotAvalonia.Remote`) — serves your source files to the device.
- **Host compiler** (`HotAvalonia.HostCompiler`) — watches `.axaml` files and compiles each change.

Both ship in the `HotAvalonia` NuGet package's `tools/` folder. They are **dev-time host processes**, not
part of your build — start them once, then edit XAML freely (no rebuild).

## Prerequisites
- macOS + the .NET SDK (iOS apps build only on a Mac).
- The `HotAvalonia` package referenced in your iOS app + Avalonia views projects (`PrivateAssets="All"`).
- The app **built + deployed once** (so the compiler can auto-discover its reference closure), device on the
  same Wi-Fi/subnet (VPN off) for a real device.

## 90% case — one command
```bash
samples/host-compiled/hotreload.sh --app-project path/to/YourApp.iOS.csproj
```
Starts HARFS + the compiler (each only if not already running; Ctrl-C stops what it started). Then deploy a
Debug build, and edit any `.axaml`.

Useful flags:
- `--no-harfs` — you run your own HARFS server; just start the compiler.
- `--no-compiler` — start only HARFS.
- `--bind 127.0.0.1` — simulator (default `0.0.0.0` targets a real device over the LAN).
- `--views-dir <dir>`, `--port <n>`, `--secret <s>` — override the defaults.

No code is required in a stock Avalonia app — the weaver auto-enables hot reload, `UseHostCompiledXaml`
defaults on for iOS, and the views' source root is baked into the assembly (no `ProjectLocator.AddHint`).

## 10% case — roll your own
Everything is a plain CLI/API, so you can script your own loop:
```bash
# 1. Serve sources to the device
dotnet <pkg>/tools/HotAvalonia.Remote.dll --root <repo> --endpoint 0.0.0.0:9500 --secret text:<secret>
# 2. Compile each changed view (auto-discovers closure + Avalonia version from the app project)
dotnet <pkg>/tools/HotAvalonia.HostCompiler.dll watch <views-dir> --app-project <App.iOS.csproj>
#    (one-shot: `compile <view.axaml> --app-project <App.iOS.csproj>`; explicit inputs:
#     `--closure <dir> --avalonia-version <ver>`)
```
A custom app bootstrap (one that doesn't use a virtual `CustomizeAppBuilder`) enables hot reload itself:
```csharp
var fs = HotAvalonia.IO.FileSystem.Connect(macEndpoint, secret, fallbackFileSystem: HotAvalonia.IO.FileSystem.Empty);
var config = HotAvalonia.AvaloniaHotReloadConfig.Default with { FileSystem = fs };  // UseHostCompiledXaml defaults true on iOS
HotAvalonia.AvaloniaHotReloadContext.FromAppDomain(config).EnableHotReload();
```

## Notes
- The script reuses an already-running HARFS/compiler instead of starting a duplicate.
- Tool paths resolve from the newest cached `hotavalonia` package; override with `HARFS_DLL` /
  `HOSTCOMPILER_DLL` (e.g. to point at a local build output).
