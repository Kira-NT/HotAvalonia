> [!IMPORTANT]
>
> You probably don't want to install this package manually.
> Please, check out the main [`HotAvalonia`](https://nuget.org/packages/HotAvalonia) package, which will set everything up for you automatically.

# HotAvalonia.Fody

[![GitHub Build Status](https://img.shields.io/github/actions/workflow/status/Kira-NT/HotAvalonia/build.yml?logo=github)](https://github.com/Kira-NT/HotAvalonia/actions/workflows/build.yml)
[![Version](https://img.shields.io/github/v/release/Kira-NT/HotAvalonia?sort=date&label=version)](https://github.com/Kira-NT/HotAvalonia/releases/latest)
[![License](https://img.shields.io/github/license/Kira-NT/HotAvalonia?cacheSeconds=36000)](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/LICENSE.md)

`HotAvalonia.Fody` is an optional [Fody](https://github.com/Fody/Fody) weaver designed to enhance the overall hot reload experience for HotAvalonia users.

----

## Usage

Like any other Fody weaver, `HotAvalonia.Fody` can be configured either via `FodyWeavers.xml`, or directly in your project file using the `WeaverConfiguration` property, as can be seen below:

```xml
<PropertyGroup>
  <WeaverConfiguration>
    <Weavers>
      <HotAvalonia>
        <Solution Path="$(SolutionPath)">
          <Project Path="/home/user/projects/MyApp/src/MyApp/MyApp.csproj" AssemblyName="MyApp" />
          <Project Path="/home/user/projects/MyApp/src/MyApp.Desktop/MyApp.Desktop.csproj" AssemblyName="MyApp.Desktop" />
        </Solution>
        <PopulateOverride Enable="true" />
        <UseHotReload Enable="true" GeneratePathResolver="true" />
        <References Enable="false" Exclude="HotAvalonia.Core;Avalonia.Markup.Xaml.Loader" />
      </HotAvalonia>
    </Weavers>
  </WeaverConfiguration>
</PropertyGroup>
```

Here's a quick overview of the weaver-specific properties:

### PopulateOverride

This feature weaver is responsible for injecting a small piece of logic into Avalonia resources *(such as styles and resource dictionaries)*, allowing their original, precompiled `Populate` method to be overridden at runtime even in environments where method injection is unavailable.

| Name | Description | Default | Examples |
| :--- | :---------- | :------ | :------- |
| `Enable` | Sets a value indicating whether this feature weaver is enabled. | `false` | `true` <br> `false` |

Note that for this weaver to work properly, Fody needs to run **after** Avalonia has finished compiling the XAML files; otherwise, there will be nothing to inject into. This can be achieved by:

```xml
<FodyDependsOnTargets>$(FodyDependsOnTargets);CompileAvaloniaXaml</FodyDependsOnTargets>
```

### UseHotReload

This feature weaver is responsible for automatically invoking `HotAvalonia.AvaloniaHotReloadExtensions.UseHotReload(AppBuilder)` on the `AppBuilder` instance created during app initialization, thereby kicking HotAvalonia into action. Thanks to this, users do not need to modify their source code to enable hot reload for their application.

| Name | Description | Default | Examples |
| :--- | :---------- | :------ | :------- |
| `Enable` | Sets a value indicating whether this feature weaver is enabled. | `false` | `true` <br> `false` |
| `GeneratePathResolver` | Sets a value indicating whether this feature weaver should automatically generate `HotAvalonia.AvaloniaHotReloadExtensions.ResolveProjectPath(Assembly)` based on the `<Solution>` element, if this method does not already exist. | `false` | `true` <br> `false` |

### References

This feature weaver is responsible for removing specified assembly references from the main module definition of your project and cleaning up related copy-local paths.

| Name | Description | Default | Examples |
| :--- | :---------- | :------ | :------- |
| `Enable` | Sets a value indicating whether this feature weaver is enabled. | `false` | `true` <br> `false` |
| `Exclude` | Provides a semicolon-separated list of assembly names, all mentions of which should be erased from the target project. | N/A | `HotAvalonia.Core;Avalonia.Markup.Xaml.Loader` |

----

## License

Licensed under the terms of the [MIT License](https://github.com/Kira-NT/HotAvalonia/blob/HEAD/LICENSE.md).
