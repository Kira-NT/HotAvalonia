// <auto-generated>
//   This file has been automatically added to your project by the "HotAvalonia.Extensions" NuGet package
//   (https://nuget.org/packages/HotAvalonia.Extensions).
//
//   Please see https://github.com/Kir-Antipov/HotAvalonia for more information.
// </auto-generated>

//#region License
// MIT License
//
// Copyright (c) 2023-2024 Kir_Antipov
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//#endregion

#nowarn
#nowarn FS0044
#nowarn FS3261

namespace HotAvalonia

open System
open System.Diagnostics
open System.Diagnostics.CodeAnalysis
open System.IO
open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Avalonia

/// <summary>
/// Indicates that the decorated method should be called whenever the associated Avalonia control is hot reloaded.
/// </summary>
/// <remarks>
/// This attribute is intended to be applied to parameterless instance methods of Avalonia controls.
/// When the control is hot reloaded, the method marked with this attribute is executed.
/// This can be used to refresh or update the control's state in response to hot reload events.
///
/// <br/><br/>
///
/// The method must meet the following requirements:
/// <list type="bullet">
///   <item>It must be an instance method (i.e., not static).</item>
///   <item>It must not have any parameters.</item>
/// </list>
///
/// Example usage:
/// <code>
/// [<AvaloniaHotReload>]
/// let private initialize () =
///     // Code to initialize or refresh
///     // the control during hot reload.
///     ()
/// </code>
/// </remarks>
[<ExcludeFromCodeCoverage>]
[<Conditional("ENABLE_XAML_HOT_RELOAD")>]
[<AttributeUsage(AttributeTargets.Method)>]
type internal AvaloniaHotReloadAttribute() =
    inherit Attribute()

/// <summary>
/// Provides extension methods for enabling and disabling hot reload functionality for Avalonia applications.
/// </summary>
[<ExcludeFromCodeCoverage>]
[<Extension>]
type internal AvaloniaHotReloadExtensions =
#if ENABLE_XAML_HOT_RELOAD && !DISABLE_XAML_HOT_RELOAD
    /// <summary>
    /// Creates a factory method for generating an <see cref="IHotReloadContext"/>
    /// using the specified control type and its XAML file path.
    /// </summary>
    /// <param name="controlType">The control type.</param>
    /// <param name="controlFilePath">The file path to the associated XAML file.</param>
    /// <returns>A factory method for creating an <see cref="IHotReloadContext"/> instance.</returns>
    [<DebuggerStepThrough>]
    static member private CreateHotReloadContextFactory(controlType: Type, controlFilePath: string) = fun() ->
        let projectLocator = AvaloniaProjectLocator(AvaloniaHotReloadExtensions.GetFileSystem())
        if not (String.IsNullOrEmpty(controlFilePath) || projectLocator.FileSystem.FileExists(controlFilePath)) then
            raise (FileNotFoundException("The corresponding XAML file could not be found.", controlFilePath))

        if not (String.IsNullOrEmpty(controlFilePath)) then
            projectLocator.AddHint(controlType, controlFilePath)

        AvaloniaHotReloadExtensions.CreateHotReloadContext(projectLocator)

    /// <summary>
    /// Creates a factory method for generating an <see cref="IHotReloadContext"/>
    /// using a custom project path resolver.
    /// </summary>
    /// <param name="projectPathResolver">The callback function capable of resolving a project path for a given assembly.</param>
    /// <returns>A factory method for creating an <see cref="IHotReloadContext"/> instance.</returns>
    [<DebuggerStepThrough>]
    static member private CreateHotReloadContextFactory(projectPathResolver: Func<Assembly, string | null> | null) = fun() ->
        let projectLocator = AvaloniaProjectLocator(AvaloniaHotReloadExtensions.GetFileSystem())
        match projectPathResolver with
        | null -> ()
        | hint -> projectLocator.AddHint(hint)

        AvaloniaHotReloadExtensions.CreateHotReloadContext(projectLocator)

    /// <summary>
    /// Creates a hot reload context for the current environment.
    /// </summary>
    /// <param name="projectLocator">The project locator used to find source directories of assemblies.</param>
    /// <returns>A hot reload context for the current environment.</returns>
    [<DebuggerStepThrough>]
    static member private CreateHotReloadContext(projectLocator: AvaloniaProjectLocator) =
#if ENABLE_LITE_XAML_HOT_RELOAD
        AvaloniaHotReloadContext.CreateLite(projectLocator)
#else
        AvaloniaHotReloadContext.Create(projectLocator)
#endif

    /// <summary>
    /// Gets the current file system instance.
    /// </summary>
    /// <returns>The current file system instance.</returns>
    [<DebuggerStepThrough>]
    static member private GetFileSystem() =
#if ENABLE_REMOTE_XAML_HOT_RELOAD
        HotAvalonia.IO.FileSystem.Connect(HotAvalonia.IO.FileSystem.Empty)
#else
        HotAvalonia.IO.FileSystem.Current
#endif
#endif

    /// <summary>
    /// Enables hot reload functionality for the specified <see cref="AppBuilder"/> instance.
    /// </summary>
    /// <param name="builder">The app builder instance.</param>
    /// <returns>The app builder instance.</returns>
    [<DebuggerStepThrough>]
    [<Extension>]
    static member UseHotReload(builder: AppBuilder) =
#if ENABLE_XAML_HOT_RELOAD && !DISABLE_XAML_HOT_RELOAD
        AvaloniaHotReload.Enable(builder, AvaloniaHotReloadExtensions.CreateHotReloadContextFactory(null))
#endif
        builder

    /// <summary>
    /// Enables hot reload functionality for the specified <see cref="AppBuilder"/> instance.
    /// </summary>
    /// <param name="builder">The app builder instance.</param>
    /// <param name="projectPathResolver">The callback function capable of resolving a project path for a given assembly.</param>
    /// <returns>The app builder instance.</returns>
    [<DebuggerStepThrough>]
    [<Extension>]
    static member UseHotReload(builder: AppBuilder, projectPathResolver: Func<Assembly, string | null>) =
#if ENABLE_XAML_HOT_RELOAD && !DISABLE_XAML_HOT_RELOAD
        AvaloniaHotReload.Enable(builder, AvaloniaHotReloadExtensions.CreateHotReloadContextFactory(projectPathResolver))
#endif
        builder

    /// <summary>
    /// Enables hot reload functionality for the given Avalonia application.
    /// </summary>
    /// <param name="app">The Avalonia application instance for which hot reload should be enabled.</param>
    /// <param name="appFilePath">The file path of the application's main source file. Optional if the method called within the file of interest.</param>
    [<Conditional("ENABLE_XAML_HOT_RELOAD")>]
    [<DebuggerStepThrough>]
    [<Extension>]
    static member EnableHotReload(app: Application, [<CallerFilePath; Optional; DefaultParameterValue("")>] appFilePath: string) =
#if ENABLE_XAML_HOT_RELOAD && !DISABLE_XAML_HOT_RELOAD
        AvaloniaHotReload.Enable(app, AvaloniaHotReloadExtensions.CreateHotReloadContextFactory(app.GetType(), appFilePath))
#else
        ()
#endif

    /// <summary>
    /// Enables hot reload functionality for the given Avalonia application.
    /// </summary>
    /// <param name="app">The Avalonia application instance for which hot reload should be enabled.</param>
    /// <param name="projectPathResolver">The callback function capable of resolving a project path for a given assembly.</param>
    [<Conditional("ENABLE_XAML_HOT_RELOAD")>]
    [<DebuggerStepThrough>]
    [<Extension>]
    static member EnableHotReload(app: Application, projectPathResolver: Func<Assembly, string | null>) =
#if ENABLE_XAML_HOT_RELOAD && !DISABLE_XAML_HOT_RELOAD
        AvaloniaHotReload.Enable(app, AvaloniaHotReloadExtensions.CreateHotReloadContextFactory(projectPathResolver))
#else
        ()
#endif

    /// <summary>
    /// Disables hot reload functionality for the given Avalonia application.
    /// </summary>
    /// <param name="app">The Avalonia application instance for which hot reload should be disabled.</param>
    [<Conditional("ENABLE_XAML_HOT_RELOAD")>]
    [<DebuggerStepThrough>]
    [<Extension>]
    static member DisableHotReload(app: Application) =
#if ENABLE_XAML_HOT_RELOAD && !DISABLE_XAML_HOT_RELOAD
        AvaloniaHotReload.Disable(app)
#else
        ()
#endif
