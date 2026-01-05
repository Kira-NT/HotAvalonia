using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Styling;
using HotAvalonia.Helpers;
using HotAvalonia.Reflection.Inject;

namespace HotAvalonia.Xaml;

/// <summary>
/// Represents a successfully compiled XAML document.
/// </summary>
public sealed class CompiledXamlDocument : IEquatable<CompiledXamlDocument>
{
    /// <summary>
    /// The field reference for <c>StyledElement._stylesApplied</c>.
    /// </summary>
    private static readonly FieldInfo? s_stylesAppliedField = typeof(StyledElement).GetInstanceField("_stylesApplied", typeof(bool));

    /// <summary>
    /// The property reference for <c>AvaloniaObject.InheritanceParent</c>.
    /// </summary>
    private static readonly PropertyInfo? s_inheritanceParentProperty = typeof(AvaloniaObject).GetInstanceProperty("InheritanceParent");

    /// <summary>
    /// The URI associated with the XAML document.
    /// </summary>
    private readonly Uri _uri;

    /// <summary>
    /// The type of the root element produced by the XAML document.
    /// </summary>
    private readonly Type _rootType;

    /// <summary>
    /// The method used to create a new instance of the XAML document's root control.
    /// </summary>
    private readonly MethodBase _build;

    /// <summary>
    /// The method used to populate an existing instance of the root control.
    /// </summary>
    private readonly MethodInfo _populate;

    /// <summary>
    /// An optional field representing an override for the populate method.
    /// </summary>
    private readonly FieldInfo? _populateOverride;

    /// <summary>
    /// An optional action used to refresh the root control after loading.
    /// </summary>
    private readonly Action<object>? _refresh;

    /// <inheritdoc cref="CompiledXamlDocument(Uri, MethodBase, MethodInfo, FieldInfo?, Action{object}?)"/>
    public CompiledXamlDocument(string uri, MethodBase build, MethodInfo populate)
        : this(new Uri(uri), build, populate, null, null)
    {
    }

    /// <inheritdoc cref="CompiledXamlDocument(Uri, MethodBase, MethodInfo, FieldInfo?, Action{object}?)"/>
    public CompiledXamlDocument(Uri uri, MethodBase build, MethodInfo populate)
        : this(uri, build, populate, null, null)
    {
    }

    /// <inheritdoc cref="CompiledXamlDocument(Uri, MethodBase, MethodInfo, CompiledXamlDocument)"/>
    public CompiledXamlDocument(string? uri, MethodBase? build, MethodInfo? populate, CompiledXamlDocument baseDocument)
        : this(
            uri is null ? baseDocument._uri : new(uri),
            build ?? baseDocument._build,
            populate ?? baseDocument._populate,
            baseDocument._populateOverride,
            baseDocument._refresh)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompiledXamlDocument"/> class,
    /// using values from the specified base document where necessary.
    /// </summary>
    /// <param name="uri">The URI associated with the XAML document.</param>
    /// <param name="build">The method used to create a new instance of the root control.</param>
    /// <param name="populate">The method used to populate an existing root control.</param>
    /// <param name="baseDocument">The base document from which default values are taken if any of the other parameters are <c>null</c>.</param>
    public CompiledXamlDocument(Uri? uri, MethodBase? build, MethodInfo? populate, CompiledXamlDocument baseDocument)
        : this(
            uri ?? baseDocument._uri,
            build ?? baseDocument._build,
            populate ?? baseDocument._populate,
            baseDocument._populateOverride,
            baseDocument._refresh)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompiledXamlDocument"/> class.
    /// </summary>
    /// <param name="uri">The URI associated with the XAML document.</param>
    /// <param name="build">The method used to create a new instance of the root control.</param>
    /// <param name="populate">The method used to populate an existing root control.</param>
    /// <param name="populateOverride">An optional field representing an override for the populate method.</param>
    /// <param name="refresh">A delegate that defines a refresh action for the root control.</param>
    internal CompiledXamlDocument(
        Uri uri,
        MethodBase build,
        MethodInfo populate,
        FieldInfo? populateOverride = null,
        Action<object>? refresh = null)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(build);
        ArgumentNullException.ThrowIfNull(populate);

        if (!XamlScanner.IsBuildMethod(build))
            ArgumentException.Throw(nameof(build), "The provided method does not meet the build method criteria.");

        if (!XamlScanner.IsPopulateMethod(populate))
            ArgumentException.Throw(nameof(populate), "The provided method does not meet the populate method criteria.");

        if (populateOverride is not null && !XamlScanner.IsPopulateOverrideField(populateOverride))
            ArgumentException.Throw(nameof(populateOverride), "The provided field does not meet the populate override criteria.");

        _uri = uri;
        _build = build;
        _populate = populate;
        _populateOverride = populateOverride;
        _refresh = refresh;
        _rootType = build is MethodInfo buildInfo ? buildInfo.ReturnType : build.DeclaringType;
    }

    /// <summary>
    /// Gets the URI associated with the current document.
    /// </summary>
    public Uri Uri => _uri;

    /// <summary>
    /// Gets the type of the root element of the current document.
    /// </summary>
    public Type RootType => _rootType;

    /// <summary>
    /// Gets the method used to create a new instance of the root control.
    /// </summary>
    public MethodBase BuildMethod => _build;

    /// <summary>
    /// Gets the method used to populate an existing root control.
    /// </summary>
    public MethodInfo PopulateMethod => _populate;

    /// <summary>
    /// Creates a new instance of the root control representing this document.
    /// </summary>
    /// <param name="serviceProvider">
    /// An optional service provider used to resolve dependencies during the creation process.
    /// </param>
    /// <returns>A newly created instance of the root control.</returns>
    [OverloadResolutionPriority(-1)]
    public object Build(IServiceProvider? serviceProvider = null)
        => Build([serviceProvider]);

    /// <inheritdoc cref="Build(IServiceProvider?)"/>
    /// <param name="services">A list of services required to construct the control.</param>
    public object Build(params ReadOnlySpan<object?> services)
    {
        IServiceProvider? serviceProvider = null;
        LinkedList<object?>? serviceList = null;
        foreach (object? service in services)
        {
            if (serviceProvider is null && service is IServiceProvider parentServiceProvider)
            {
                serviceProvider = XamlIlRuntimeHelpers.CreateRootServiceProviderV3(parentServiceProvider);
            }
            else
            {
                (serviceList ??= new()).AddLast(service);
            }
        }

        ParameterInfo[] parameters = _build.GetParameters();
        object?[] args = parameters.Length == 0 ? null! : new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            Type argType = parameters[i].ParameterType;
            bool serviceFound = false;
            LinkedListNode<object?>? serviceNode = serviceList?.First;
            while (serviceNode is not null)
            {
                object? service = serviceNode.Value;
                if (service is null ? !argType.IsValueType : argType.IsAssignableFrom(service.GetType()))
                {
                    args[i] = service;
                    serviceList!.Remove(serviceNode);
                    serviceFound = true;
                    break;
                }
            }
            if (serviceFound)
                continue;

            serviceProvider ??= XamlIlRuntimeHelpers.CreateRootServiceProviderV2();
            args[i] = argType.IsAssignableFrom(serviceProvider.GetType()) ? serviceProvider : serviceProvider.GetService(argType);
        }
        return _build.Invoke(null, args) ?? throw new InvalidOperationException();
    }

    /// <inheritdoc cref="Populate(IServiceProvider?, object)"/>
    public void Populate(object rootControl)
        => Populate(serviceProvider: null, rootControl);

    /// <summary>
    /// Populates the specified root control.
    /// </summary>
    /// <param name="serviceProvider">
    /// The service provider used to resolve dependencies during the population process.
    /// </param>
    /// <param name="rootControl">The root control to be populated.</param>
    public void Populate(IServiceProvider? serviceProvider, object rootControl)
    {
        ArgumentNullException.ThrowIfNull(rootControl);

        _populate.Invoke(null, [serviceProvider ?? XamlIlRuntimeHelpers.CreateRootServiceProviderV2(), rootControl]);
    }

    /// <inheritdoc cref="Reload(IServiceProvider?, object)"/>
    public void Reload(object rootControl)
        => Reload(serviceProvider: null, rootControl);

    /// <summary>
    /// Reloads an already populated control.
    /// </summary>
    /// <inheritdoc cref="Populate(IServiceProvider?, object)"/>
    public void Reload(IServiceProvider? serviceProvider, object rootControl)
    {
        ArgumentNullException.ThrowIfNull(rootControl);

        Reset(rootControl, out Action restore);
        _populate.Invoke(null, [serviceProvider ?? XamlIlRuntimeHelpers.CreateRootServiceProviderV2(), rootControl]);
        restore();

        Refresh(rootControl);
    }

    /// <summary>
    /// Attempts to override the populate action.
    /// </summary>
    /// <param name="populate">The action to use for the populate override injection.</param>
    /// <param name="injection">
    /// When this method returns, contains the injection result if the override was successfully injected;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if the populate override was successfully injected; otherwise, <c>false</c>.</returns>
    internal bool TryOverridePopulate(Action<IServiceProvider?, object> populate, [NotNullWhen(true)] out IInjection? injection)
    {
        ArgumentNullException.ThrowIfNull(populate);

        if (_populateOverride is null)
        {
            injection = null;
            return false;
        }

        injection = new OverridePopulateInjection(_populateOverride, populate);
        return true;
    }

    /// <summary>
    /// Refreshes the specified root control, if provided.
    /// </summary>
    /// <remarks>
    /// Some things (e.g., cached named control references) are not a part of
    /// the population routine, so we need to sort those out manually.
    /// </remarks>
    /// <param name="rootControl">The root control to refresh.</param>
    public void Refresh(object rootControl)
        => _refresh?.Invoke(rootControl);

    /// <inheritdoc/>
    public override string ToString()
        => $"{nameof(CompiledXamlDocument)} {{ {nameof(Uri)} = {Uri}, {nameof(RootType)} = {RootType} }}";

    /// <inheritdoc/>
    public override bool Equals(object obj)
        => obj is CompiledXamlDocument other && Equals(other);

    /// <inheritdoc/>
    public bool Equals(CompiledXamlDocument other)
        => other._uri == _uri && other._build == _build && other._populate == _populate;

    /// <inheritdoc/>
    public override int GetHashCode()
        => _rootType.GetHashCode();

    /// <summary>
    /// Clears resources, styles, and other data from an Avalonia control.
    /// </summary>
    /// <param name="control">The control to clear.</param>
    private static void Clear(object? control)
    {
        if (control is StyledElement)
            s_stylesAppliedField?.SetValue(control, false);

        (ICollection<KeyValuePair<object, object?>>? resources, ICollection<IStyle>? styles) = control switch
        {
            Application x => (x.Resources, x.Styles),
            StyledElement x => (x.Resources, x.Styles),
            Styles x => (x.Resources, x),
            StyleBase x => (x.Resources, x as ICollection<IStyle> ?? (x as IStyleHost)?.Styles),
            IStyleHost x => (x as ICollection<KeyValuePair<object, object?>>, x.Styles),
            _ => (control as ICollection<KeyValuePair<object, object?>>, control as ICollection<IStyle>),
        };
        resources?.Clear();
        styles?.Clear();
    }

    /// <summary>
    /// Fully resets the state of an Avalonia control and
    /// provides a callback to restore its original state.
    /// </summary>
    /// <param name="control">The control to reset.</param>
    /// <param name="restore">When this method returns, contains a callback to restore the control's original state.</param>
    private static void Reset(object? control, out Action restore)
    {
        Detach(control, out ILogical? logicalParent, out AvaloniaObject? inheritanceParent);
        Clear(control);
        restore = () => Attach(control, logicalParent, inheritanceParent);
    }

    /// <summary>
    /// Detaches an Avalonia control from its logical and inheritance parents.
    /// </summary>
    /// <param name="control">The control to detach.</param>
    /// <param name="logicalParent">
    /// When this method returns, contains the control's logical parent, or <c>null</c> if it has none.
    /// </param>
    /// <param name="inheritanceParent">
    /// When this method returns, contains the control's inheritance parent, or <c>null</c> if it has none.
    /// </param>
    private static void Detach(object? control, out ILogical? logicalParent, out AvaloniaObject? inheritanceParent)
    {
        logicalParent = (control as ILogical)?.GetLogicalParent();
        inheritanceParent = control is AvaloniaObject
            ? s_inheritanceParentProperty?.GetValue(control) as AvaloniaObject
            : null;

        (control as ISetLogicalParent)?.SetParent(null);
        (control as ISetInheritanceParent)?.SetParent(null);
    }

    /// <summary>
    /// Attaches an Avalonia control to the specified logical and inheritance parents.
    /// </summary>
    /// <param name="control">The control to attach.</param>
    /// <param name="logicalParent">The logical parent to attach the control to.</param>
    /// <param name="inheritanceParent">The inheritance parent to attach the control to.</param>
    private static void Attach(object? control, ILogical? logicalParent, AvaloniaObject? inheritanceParent)
    {
        if (logicalParent is not null && control is ISetLogicalParent logical)
            logical.SetParent(logicalParent);

        if (inheritanceParent is not null && control is ISetInheritanceParent inheritance)
            inheritance.SetParent(inheritanceParent);
    }
}

/// <summary>
/// Provides functionality to override the population mechanism of Avalonia controls using a custom delegate.
/// </summary>
/// <remarks>
/// This class specifically targets the hidden <c>!XamlIlPopulateOverride</c> field to hijack
/// the logic of control population, allowing for a fallback mechanism whenever proper
/// injection techniques are not available.
/// </remarks>
file sealed class OverridePopulateInjection : IInjection
{
    /// <summary>
    /// The field to inject the new population logic into.
    /// </summary>
    private readonly FieldInfo _populateOverride;

    /// <summary>
    /// The populate action to override the original one with.
    /// </summary>
    private readonly Delegate? _populate;

    /// <summary>
    /// The previous value of the <c>!XamlIlPopulateOverride</c> field before it was overridden.
    /// </summary>
    private object? _previousPopulateOverride;

    /// <summary>
    /// Initializes a new instance of the <see cref="OverridePopulateInjection"/> class.
    /// </summary>
    /// <param name="populateOverride">The field to inject the new population logic into.</param>
    /// <param name="populate">The populate action to override the original one with.</param>
    public OverridePopulateInjection(FieldInfo populateOverride, Action<IServiceProvider, object> populate)
    {
        _populateOverride = populateOverride;

        if (populateOverride.FieldType == typeof(Action<object>))
        {
            _populate = (object control) =>
            {
                populate(XamlIlRuntimeHelpers.CreateRootServiceProviderV2(), control);
                _populateOverride.SetValue(null, _populate);
            };
        }
        else if (populateOverride.FieldType == typeof(Action<IServiceProvider?, object>))
        {
            _populate = (IServiceProvider? serviceProvider, object control) =>
            {
                populate(serviceProvider ?? XamlIlRuntimeHelpers.CreateRootServiceProviderV2(), control);
                _populateOverride.SetValue(null, _populate);
            };
        }

        Apply();
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="OverridePopulateInjection"/> class.
    /// Reverts the method injection if not already done.
    /// </summary>
    ~OverridePopulateInjection()
    {
        Undo();
    }

    /// <summary>
    /// Applies the method injection.
    /// </summary>
    public void Apply()
    {
        _previousPopulateOverride = _populateOverride.GetValue(null);
        _populateOverride.SetValue(null, _populate);
    }

    /// <summary>
    /// Reverts all the effects caused by the method injection.
    /// </summary>
    public void Undo()
    {
        _populateOverride.SetValue(null, _previousPopulateOverride);
        _previousPopulateOverride = null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Undo();
        GC.SuppressFinalize(this);
    }
}
