using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotAvalonia.Helpers;
using HotAvalonia.Reflection.Inject;

namespace HotAvalonia.Xaml;

/// <summary>
/// Represents a successfully compiled XAML document.
/// </summary>
public sealed class CompiledXamlDocument : IEquatable<CompiledXamlDocument>
{
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
    public CompiledXamlDocument(
        string uri,
        MethodBase build,
        MethodInfo populate)
        : this(new Uri(uri), build, populate, null, null)
    {
    }

    /// <inheritdoc cref="CompiledXamlDocument(Uri, MethodBase, MethodInfo, FieldInfo?, Action{object}?)"/>
    public CompiledXamlDocument(
        Uri uri,
        MethodBase build,
        MethodInfo populate)
        : this(uri, build, populate, null, null)
    {
    }

    /// <inheritdoc cref="CompiledXamlDocument(Uri, MethodBase, MethodInfo, FieldInfo?, Action{object}?)"/>
    internal CompiledXamlDocument(
        string uri,
        MethodBase build,
        MethodInfo populate,
        FieldInfo? populateOverride = null,
        Action<object>? refresh = null)
        : this(new Uri(uri), build, populate, populateOverride, refresh)
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
        _ = uri ?? throw new ArgumentNullException(nameof(uri));
        _ = build ?? throw new ArgumentNullException(nameof(build));
        _ = populate ?? throw new ArgumentNullException(nameof(populate));

        if (!XamlScanner.IsBuildMethod(build))
            throw new ArgumentException("The provided method does not meet the build method criteria.", nameof(build));

        if (!XamlScanner.IsPopulateMethod(populate))
            throw new ArgumentException("The provided method does not meet the populate method criteria.", nameof(populate));

        if (populateOverride is not null && !XamlScanner.IsPopulateOverrideField(populateOverride))
            throw new ArgumentException("The provided field does not meet the populate override criteria.", nameof(populateOverride));

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
    /// Loads a control from XAML markup and initializes it.
    /// </summary>
    /// <param name="xaml">The XAML markup to populate the control with.</param>
    /// <param name="rootControl">An optional existing instance of the root control to be populated.</param>
    /// <param name="compiledPopulateMethod">The newly compiled populate method, if the compilation was successful.</param>
    internal object? Load(string xaml, object? rootControl, out MethodInfo? compiledPopulateMethod)
    {
        rootControl = AvaloniaControlHelper.Load(xaml, _uri, rootControl, _rootType.Assembly, out compiledPopulateMethod);
        if (rootControl is not null)
            Refresh(rootControl);

        return rootControl;
    }

    /// <summary>
    /// Creates a new instance of the root control representing this document.
    /// </summary>
    /// <param name="serviceProvider">
    /// An optional service provider used to resolve dependencies during the creation process.
    /// </param>
    /// <returns>A newly created instance of the root control.</returns>
    public object Build(IServiceProvider? serviceProvider = null)
        => AvaloniaControlHelper.Build(_build, serviceProvider);

    /// <inheritdoc cref="Populate(IServiceProvider?, object)"/>
    public void Populate(object rootControl)
        => Populate(serviceProvider: null, rootControl);

    /// <inheritdoc cref="Populate(IServiceProvider?, object, MethodBase)"/>
    public void Populate(IServiceProvider? serviceProvider, object rootControl)
        => Populate(serviceProvider, rootControl, _populate);

    /// <summary>
    /// Populates the specified root control.
    /// </summary>
    /// <param name="serviceProvider">
    /// The service provider used to resolve dependencies during the population process.
    /// </param>
    /// <param name="rootControl">The root control to be populated.</param>
    /// <param name="populateMethod">The method used to populate the control.</param>
    internal void Populate(IServiceProvider? serviceProvider, object rootControl, MethodBase populateMethod)
    {
        AvaloniaControlHelper.Populate(populateMethod, serviceProvider, rootControl);
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
        injection = null;
        if (_populateOverride is null)
            return false;

        return AvaloniaControlHelper.TryInjectPopulateOverride(_populateOverride, populate, out injection);
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
    {
        LoggingHelper.Log(rootControl, "Refreshing...");
        _refresh?.Invoke(rootControl);
    }

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
}
