' <auto-generated>
'   This file has been automatically added to your project by the "HotAvalonia.Extensions" NuGet package
'   (https://nuget.org/packages/HotAvalonia.Extensions).
'
'   Please see https://github.com/Kir-Antipov/HotAvalonia for more information.
' </auto-generated>

#Region "License"
' MIT License
'
' Copyright (c) 2023-2024 Kir_Antipov
'
' Permission is hereby granted, free of charge, to any person obtaining a copy
' of this software and associated documentation files (the "Software"), to deal
' in the Software without restriction, including without limitation the rights
' to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
' copies of the Software, and to permit persons to whom the Software is
' furnished to do so, subject to the following conditions:
'
' The above copyright notice and this permission notice shall be included in all
' copies or substantial portions of the Software.
'
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
' IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
' FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
' AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
' LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
' OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
' SOFTWARE.
#End Region

#Disable Warning

Imports System
Imports System.Diagnostics
Imports System.Diagnostics.CodeAnalysis
Imports System.IO
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports Avalonia

Namespace Global.HotAvalonia
    ''' <summary>
    ''' Indicates that the decorated method should be called whenever the associated Avalonia control is hot reloaded.
    ''' </summary>
    ''' <remarks>
    ''' This attribute is intended to be applied to parameterless instance methods of Avalonia controls.
    ''' When the control is hot reloaded, the method marked with this attribute is executed.
    ''' This can be used to refresh or update the control's state in response to hot reload events.
    '''
    ''' <br/><br/>
    '''
    ''' The method must meet the following requirements:
    ''' <list type="bullet">
    '''   <item>It must be an instance method (i.e., not static).</item>
    '''   <item>It must not have any parameters.</item>
    ''' </list>
    '''
    ''' Example usage:
    ''' <code>
    ''' <AvaloniaHotReload>
    ''' Private Sub Initialize()
    '''     ' Code to initialize or refresh
    '''     ' the control during hot reload.
    ''' End Sub
    ''' </code>
    ''' </remarks>
    <ExcludeFromCodeCoverage>
    <Conditional("ENABLE_XAML_HOT_RELOAD")>
    <AttributeUsage(AttributeTargets.Method)>
    Friend NotInheritable Class AvaloniaHotReloadAttribute
        Inherits Attribute
    End Class

    ''' <summary>
    ''' Provides extension methods for enabling and disabling hot reload functionality for Avalonia applications.
    ''' </summary>
    <ExcludeFromCodeCoverage>
    Friend Module AvaloniaHotReloadExtensions
#If ENABLE_XAML_HOT_RELOAD AndAlso Not DISABLE_XAML_HOT_RELOAD Then
        ''' <summary>
        ''' A mapping between Avalonia <see cref="Application"/> instances and their associated hot reload context.
        ''' </summary>
        Private ReadOnly s_apps As New ConditionalWeakTable(Of Application, IHotReloadContext)

        ''' <summary>
        ''' Enables hot reload functionality for the given Avalonia application.
        ''' </summary>
        ''' <param name="app">The Avalonia application instance for which hot reload should be enabled.</param>
        ''' <param name="projectLocator">The project locator used to find source directories of assemblies.</param>
        <DebuggerStepThrough>
        Private Sub EnableHotReload(ByVal app As Application, ByVal projectLocator As AvaloniaProjectLocator)
            Dim context As IHotReloadContext = Nothing
            If Not s_apps.TryGetValue(app, context) Then
                Dim appDomainContext As IHotReloadContext = AvaloniaHotReloadContext.FromAppDomain()
                Dim assetContext As IHotReloadContext = AvaloniaHotReloadContext.ForAssets()
                context = HotReloadContext.Combine(appDomainContext, assetContext)
                s_apps.Add(app, context)
            End If

            context.EnableHotReload()
        End Sub

        ''' <summary>
        ''' Creates a new instance of the <see cref="AvaloniaProjectLocator"/> class.
        ''' </summary>
        ''' <returns>A new instance of the <see cref="AvaloniaProjectLocator"/> class.</returns>
        <DebuggerStepThrough>
        Private Function CreateAvaloniaProjectLocator() As AvaloniaProjectLocator
            Return New AvaloniaProjectLocator()
        End Function

        ''' <summary>
        ''' Enables hot reload functionality for the given Avalonia application.
        ''' </summary>
        ''' <param name="app">The Avalonia application instance for which hot reload should be enabled.</param>
        ''' <param name="appFilePath">The file path of the application's main source file. Optional if the method called within the file of interest.</param>
        <DebuggerStepThrough>
        <Extension>
        Public Sub EnableHotReload(ByVal app As Application, <CallerFilePath> Optional ByVal appFilePath As String = Nothing)
            If app Is Nothing Then
                Throw New ArgumentNullException(NameOf(app))
            End If

            If Not String.IsNullOrEmpty(appFilePath) AndAlso Not File.Exists(appFilePath) Then
                Throw New FileNotFoundException("The corresponding XAML file could not be found.", appFilePath)
            End If

            Dim projectLocator as AvaloniaProjectLocator = CreateAvaloniaProjectLocator()
            If Not String.IsNullOrEmpty(appFilePath) Then
                projectLocator.AddHint(app.GetType(), appFilePath)
            End If

            EnableHotReload(app, projectLocator)
        End Sub

        ''' <summary>
        ''' Enables hot reload functionality for the given Avalonia application.
        ''' </summary>
        ''' <param name="app">The Avalonia application instance for which hot reload should be enabled.</param>
        ''' <param name="projectPathResolver">The callback function capable of resolving a project path for a given assembly.</param>
        <DebuggerStepThrough>
        <Extension>
        Public Sub EnableHotReload(ByVal app As Application, ByVal projectPathResolver As Func(Of Assembly, String))
            If app Is Nothing Then
                Throw New ArgumentNullException(NameOf(app))
            End If

            Dim projectLocator as AvaloniaProjectLocator = CreateAvaloniaProjectLocator()
            projectLocator.AddHint(projectPathResolver)

            EnableHotReload(app, projectLocator)
        End Sub

        ''' <summary>
        ''' Disables hot reload functionality for the given Avalonia application.
        ''' </summary>
        ''' <param name="app">The Avalonia application instance for which hot reload should be disabled.</param>
        <DebuggerStepThrough>
        <Extension>
        Public Sub DisableHotReload(ByVal app As Application)
            If app Is Nothing Then
                Throw New ArgumentNullException(NameOf(app))
            End If

            Dim context As IHotReloadContext = Nothing
            If s_apps.TryGetValue(app, context) Then
                context.DisableHotReload()
            End If
        End Sub
#Else
        ''' <summary>
        ''' Enables hot reload functionality for the given Avalonia application.
        ''' </summary>
        ''' <param name="app">The Avalonia application instance for which hot reload should be enabled.</param>
        ''' <param name="appFilePath">The file path of the application's main source file. Optional if the method called within the file of interest.</param>
        <Conditional("DEBUG")>
        <DebuggerStepThrough>
        <Extension>
        Public Sub EnableHotReload(ByVal app As Application, Optional ByVal appFilePath As String = Nothing)
        End Sub

        ''' <summary>
        ''' Enables hot reload functionality for the given Avalonia application.
        ''' </summary>
        ''' <param name="app">The Avalonia application instance for which hot reload should be enabled.</param>
        ''' <param name="projectPathResolver">The callback function capable of resolving a project path for a given assembly.</param>
        <Conditional("DEBUG")>
        <DebuggerStepThrough>
        <Extension>
        Public Sub EnableHotReload(ByVal app As Application, ByVal projectPathResolver As Func(Of Assembly, String))
        End Sub

        ''' <summary>
        ''' Disables hot reload functionality for the given Avalonia application.
        ''' </summary>
        ''' <param name="app">The Avalonia application instance for which hot reload should be disabled.</param>
        <Conditional("DEBUG")>
        <DebuggerStepThrough>
        <Extension>
        Public Sub DisableHotReload(ByVal app As Application)
        End Sub
#End If
    End Module
End Namespace

#Enable Warning
