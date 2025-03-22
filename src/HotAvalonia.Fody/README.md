> [!IMPORTANT]
>
> You probably don't want to install this package manually.
> Please, check out the main [`HotAvalonia`](https://nuget.org/packages/HotAvalonia) package, which will set everything up for you automatically.

# HotAvalonia.Fody

[![GitHub Build Status](https://img.shields.io/github/actions/workflow/status/Kira-NT/HotAvalonia/build.yml?logo=github)](https://github.com/Kira-NT/HotAvalonia/actions/workflows/build.yml)
[![Version](https://img.shields.io/github/v/release/Kira-NT/HotAvalonia?sort=date&label=version)](https://github.com/Kira-NT/HotAvalonia/releases/latest)
[![License](https://img.shields.io/github/license/Kira-NT/HotAvalonia?cacheSeconds=36000)](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/LICENSE.md)

`HotAvalonia.Fody` is a [Fody](https://github.com/Fody/Fody) weaver created to overcome certain limitations that would otherwise render hot reload infeasible on non-x64 devices, while also enhancing the overall experience for HotAvalonia users.

----

## Usage

Like any other Fody weaver, `HotAvalonia.Fody` can be configured either via `FodyWeavers.xml`, or directly in your project file using the `WeaverConfiguration` property, as can be seen below:

```xml
<PropertyGroup>
  <WeaverConfiguration>
    <Weavers>
      <HotAvalonia SolutionPath="$(SolutionPath)">
        <PopulateOverride Enable="true" />
        <UseHotReload Enable="true" GeneratePathResolver="true" />
        <FileSystemCredentials Enable="true" Address="127.0.0.1" Port="20158" Secret="TXkgU3VwZXIgU2VjcmV0IFZhbHVl" />
        <References Enable="true" Exclude="HotAvalonia.Core;Avalonia.Markup.Xaml.Loader" />
      </HotAvalonia>
    </Weavers>
  </WeaverConfiguration>
</PropertyGroup>
```

Here's a quick overview of the weaver-specific properties:

### HotAvalonia

The root configuration element represents the entry-point weaver that orchestrates and manages feature-specific weavers, while its child nodes represent the individual feature-specific weavers themselves.

| Name | Description | Default | Examples |
| :--- | :---------- | :------ | :------- |
| `SolutionPath` | Sets the path to a solution file *(`.sln`)* related to this project. | If not set, the weaver will attempt to search for the first `*.sln` file from the current project directory upwards. | `$(SolutionPath)` |

### PopulateOverride

This feature weaver is responsible for recompiling Avalonia resources *(such as styles and resource dictionaries)* to make them hot-reloadable even on non‑x64 devices, where injection‑based hot reload is unavailable.

| Name | Description | Default | Examples |
| :--- | :---------- | :------ | :------- |
| `Enable` | Sets a value indicating whether this feature weaver is enabled. | `false` | `true` <br> `false` |

Note that for this weaver to work properly, Fody needs to run **after** Avalonia has finished compiling the XAML files; otherwise, there will be nothing to recompile. This can be achieved by:

```xml
<FodyDependsOnTargets>$(FodyDependsOnTargets);CompileAvaloniaXaml</FodyDependsOnTargets>
```

### UseHotReload

This feature weaver is responsible for automatically invoking `HotAvalonia.AvaloniaHotReloadExtensions.UseHotReload(AppBuilder)` on the `AppBuilder` instance created during app initialization, thereby kicking HotAvalonia into action. Thanks to this, users do not need to modify their source code to enable hot reload for their application.

| Name | Description | Default | Examples |
| :--- | :---------- | :------ | :------- |
| `Enable` | Sets a value indicating whether this feature weaver is enabled. | `false` | `true` <br> `false` |
| `GeneratePathResolver` | Sets a value indicating whether this feature weaver should automatically generate `HotAvalonia.AvaloniaHotReloadExtensions.ResolveProjectPath(Assembly)` from the solution file if this method does not already exist. | `false` | `true` <br> `false` |

### FileSystemCredentials

This feature weaver is responsible for injecting remote file system credentials into the app, enabling it to connect to the appropriate host to retrieve all the information necessary for hot reload to function.

| Name | Description | Default | Examples |
| :--- | :---------- | :------ | :------- |
| `Enable` | Sets a value indicating whether this feature weaver is enabled. | `false` | `true` <br> `false` |
| `Address` | Provides the address of the remote file system to the app. | `127.0.0.1` | `192.168.0.2` |
| `Port` | Provides the port of the remote file system to the app. | `20158` | `8080` |
| `Secret` | Provides the secret required by the remote file system to the app. | N/A | `TXkgU3VwZXIgU2VjcmV0IFZhbHVl` |

### References

This feature weaver is responsible for removing specified assembly references from the main module definition of your project and cleaning up related copy-local paths.

| Name | Description | Default | Examples |
| :--- | :---------- | :------ | :------- |
| `Enable` | Sets a value indicating whether this feature weaver is enabled. | `false` | `true` <br> `false` |
| `Exclude` | Provides a semicolon-separated list of assembly names, all mentions of which should be erased from the target project. | N/A | `HotAvalonia.Core;Avalonia.Markup.Xaml.Loader` |

----

## License

Licensed under the terms of the [MIT License](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/LICENSE.md).
