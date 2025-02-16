// <auto-generated>
//   This file has been automatically added to your project by the "HotAvalonia.Extensions" NuGet package
//   (https://nuget.org/packages/HotAvalonia.Extensions).
//
//   Please see https://github.com/Kir-Antipov/HotAvalonia for more information.
// </auto-generated>

#region License
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
#endregion

#pragma warning disable
#nullable enable

namespace HotAvalonia
{
    using global::System;
    using global::System.Diagnostics;
    using global::System.Diagnostics.CodeAnalysis;
    using global::System.IO;
    using global::System.Reflection;
    using global::System.Runtime.CompilerServices;
    using global::Avalonia;

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
    /// [AvaloniaHotReload]
    /// private void Initialize()
    /// {
    ///     // Code to initialize or refresh
    ///     // the control during hot reload.
    /// }
    /// </code>
    /// </remarks>
    [ExcludeFromCodeCoverage]
    [Conditional("HOTAVALONIA_ENABLE")]
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class AvaloniaHotReloadAttribute : Attribute
    {
    }

    /// <summary>
    /// Provides extension methods for enabling and disabling hot reload functionality for Avalonia applications.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal static class AvaloniaHotReloadExtensions
    {
#if HOTAVALONIA_ENABLE && !HOTAVALONIA_DISABLE
        /// <summary>
        /// Creates a factory method for generating an <see cref="IHotReloadContext"/>
        /// using the specified control type and its XAML file path.
        /// </summary>
        /// <param name="controlType">The control type.</param>
        /// <param name="controlFilePath">The file path to the associated XAML file.</param>
        /// <returns>A factory method for creating an <see cref="IHotReloadContext"/> instance.</returns>
        [DebuggerStepThrough]
        private static Func<IHotReloadContext> CreateHotReloadContextFactory(Type controlType, string? controlFilePath)
        {
            return new Func<IHotReloadContext>(() =>
            {
                AvaloniaProjectLocator projectLocator = new AvaloniaProjectLocator(GetFileSystem());
                if (!string.IsNullOrEmpty(controlFilePath) && !projectLocator.FileSystem.FileExists(controlFilePath))
                    throw new FileNotFoundException("The corresponding XAML file could not be found.", controlFilePath);

                if (!string.IsNullOrEmpty(controlFilePath))
                    projectLocator.AddHint(controlType, controlFilePath);

                return CreateHotReloadContext(projectLocator);
            });
        }

        /// <summary>
        /// Creates a factory method for generating an <see cref="IHotReloadContext"/>
        /// using a custom project path resolver.
        /// </summary>
        /// <param name="projectPathResolver">The callback function capable of resolving a project path for a given assembly.</param>
        /// <returns>A factory method for creating an <see cref="IHotReloadContext"/> instance.</returns>
        [DebuggerStepThrough]
        private static Func<IHotReloadContext> CreateHotReloadContextFactory(Func<Assembly, string?>? projectPathResolver)
        {
            return new Func<IHotReloadContext>(() =>
            {
                AvaloniaProjectLocator projectLocator = new AvaloniaProjectLocator(GetFileSystem());
                if ((object?)projectPathResolver != null)
                    projectLocator.AddHint(projectPathResolver);

                return CreateHotReloadContext(projectLocator);
            });
        }

        /// <summary>
        /// Creates a hot reload context for the current environment.
        /// </summary>
        /// <param name="projectLocator">The project locator used to find source directories of assemblies.</param>
        /// <returns>A hot reload context for the current environment.</returns>
        [DebuggerStepThrough]
        private static IHotReloadContext CreateHotReloadContext(AvaloniaProjectLocator projectLocator)
        {
#if HOTAVALONIA_ENABLE_LITE
            return AvaloniaHotReloadContext.CreateLite(projectLocator);
#else
            return AvaloniaHotReloadContext.Create(projectLocator);
#endif
        }

        /// <summary>
        /// Gets the current file system instance.
        /// </summary>
        /// <returns>The current file system instance.</returns>
        [DebuggerStepThrough]
        private static global::HotAvalonia.IO.IFileSystem GetFileSystem()
        {
#if HOTAVALONIA_USE_REMOTE_FILE_SYSTEM
            return global::HotAvalonia.IO.FileSystem.Connect(global::HotAvalonia.IO.FileSystem.Empty);
#else
            return global::HotAvalonia.IO.FileSystem.Current;
#endif
        }
#endif

        /// <summary>
        /// Enables hot reload functionality for the specified <see cref="AppBuilder"/> instance.
        /// </summary>
        /// <param name="builder">The app builder instance.</param>
        /// <returns>The app builder instance.</returns>
        [DebuggerStepThrough]
        public static AppBuilder UseHotReload(this AppBuilder builder)
        {
#if HOTAVALONIA_ENABLE && !HOTAVALONIA_DISABLE
            AvaloniaHotReload.Enable(builder, CreateHotReloadContextFactory(null));
#endif
            return builder;
        }

        /// <summary>
        /// Enables hot reload functionality for the specified <see cref="AppBuilder"/> instance.
        /// </summary>
        /// <param name="builder">The app builder instance.</param>
        /// <param name="projectPathResolver">The callback function capable of resolving a project path for a given assembly.</param>
        /// <returns>The app builder instance.</returns>
        [DebuggerStepThrough]
        public static AppBuilder UseHotReload(this AppBuilder builder, Func<Assembly, string?> projectPathResolver)
        {
#if HOTAVALONIA_ENABLE && !HOTAVALONIA_DISABLE
            AvaloniaHotReload.Enable(builder, CreateHotReloadContextFactory(projectPathResolver));
#endif
            return builder;
        }

        /// <summary>
        /// Enables hot reload functionality for the given Avalonia application.
        /// </summary>
        /// <param name="app">The Avalonia application instance for which hot reload should be enabled.</param>
        /// <param name="appFilePath">The file path of the application's main source file. Optional if the method called within the file of interest.</param>
        [Conditional("HOTAVALONIA_ENABLE")]
        [DebuggerStepThrough]
        public static void EnableHotReload(this Application app, [CallerFilePath] string? appFilePath = null)
        {
#if HOTAVALONIA_ENABLE && !HOTAVALONIA_DISABLE
            AvaloniaHotReload.Enable(app, CreateHotReloadContextFactory(app?.GetType(), appFilePath));
#endif
        }

        /// <summary>
        /// Enables hot reload functionality for the given Avalonia application.
        /// </summary>
        /// <param name="app">The Avalonia application instance for which hot reload should be enabled.</param>
        /// <param name="projectPathResolver">The callback function capable of resolving a project path for a given assembly.</param>
        [Conditional("HOTAVALONIA_ENABLE")]
        [DebuggerStepThrough]
        public static void EnableHotReload(this Application app, Func<Assembly, string?> projectPathResolver)
        {
#if HOTAVALONIA_ENABLE && !HOTAVALONIA_DISABLE
            AvaloniaHotReload.Enable(app, CreateHotReloadContextFactory(projectPathResolver));
#endif
        }

        /// <summary>
        /// Disables hot reload functionality for the given Avalonia application.
        /// </summary>
        /// <param name="app">The Avalonia application instance for which hot reload should be disabled.</param>
        [Conditional("HOTAVALONIA_ENABLE")]
        [DebuggerStepThrough]
        public static void DisableHotReload(this Application app)
        {
#if HOTAVALONIA_ENABLE && !HOTAVALONIA_DISABLE
            AvaloniaHotReload.Disable(app);
#endif
        }
    }
}

#nullable restore
#pragma warning restore
