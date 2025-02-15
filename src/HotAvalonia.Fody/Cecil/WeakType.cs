namespace HotAvalonia.Fody.Cecil;

/// <summary>
/// Represents a weak reference to a type.
/// </summary>
internal sealed class WeakType
{
    /// <summary>
    /// The underlying <see cref="Type"/> instance, if available.
    /// </summary>
    private readonly Type? _type;

    /// <summary>
    /// The full name of the type.
    /// </summary>
    private readonly string _fullName;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeakType"/> class.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to represent.</param>
    public WeakType(Type type)
    {
        _type = type;
        _fullName = type.IsGenericType && !type.IsGenericTypeDefinition ? type.ToString() : type.FullName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WeakType"/> class.
    /// </summary>
    /// <param name="fullName">The full name of the type.</param>
    public WeakType(string fullName)
    {
        _type = null;
        _fullName = fullName.Trim();
    }

    /// <summary>
    /// Implicitly converts a <see cref="Type"/> to a <see cref="WeakType"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to convert.</param>
    /// <returns>A new <see cref="WeakType"/> instance representing the specified type.</returns>
    public static implicit operator WeakType(Type type) => new(type);

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="WeakType"/>.
    /// </summary>
    /// <param name="fullName">The full name of the type.</param>
    /// <returns>A new <see cref="WeakType"/> instance representing the specified type name.</returns>
    public static implicit operator WeakType(string fullName) => new(fullName);

    /// <summary>
    /// Gets the full name of the type.
    /// </summary>
    public string FullName => _fullName;

    /// <summary>
    /// Gets a value indicating whether the type is generic.
    /// </summary>
    public bool IsGenericType => _type?.IsGenericType ?? _fullName.Contains('`');

    /// <summary>
    /// Gets a value indicating whether the type is a generic type definition.
    /// </summary>
    public bool IsGenericTypeDefinition => _type?.IsGenericTypeDefinition ?? (_fullName.Contains('`') && !_fullName.EndsWith("]"));

    /// <summary>
    /// Gets the generic type definition of the current type.
    /// </summary>
    /// <returns>A <see cref="WeakType"/> representing the generic type definition.</returns>
    public WeakType GetGenericTypeDefinition()
    {
        if (IsGenericTypeDefinition)
            return this;

        if (_type is not null)
            return new(_type.GetGenericTypeDefinition());

        int i = _fullName.IndexOf('[');
        if (i < 0)
            throw new InvalidOperationException("This operation is only valid on generic types.");

        return new(_fullName.Substring(0, i));
    }

    /// <summary>
    /// Gets the generic arguments of the current type.
    /// </summary>
    /// <returns>An array of <see cref="WeakType"/> representing the generic arguments.</returns>
    public WeakType[] GetGenericArguments()
    {
        if (!IsGenericType)
            return [];

        if (_type is not null)
            return Array.ConvertAll(_type.GetGenericArguments(), static x => new WeakType(x));

        return ParseGenericArguments(_fullName).Select(static x => new WeakType(x)).ToArray();
    }

    /// <summary>
    /// Parses the generic arguments from a type name.
    /// </summary>
    /// <param name="typeName">The type name containing generic argument information.</param>
    /// <returns>An enumerable of strings representing the generic arguments.</returns>
    private static IEnumerable<string> ParseGenericArguments(string typeName)
    {
        int start = typeName.IndexOf('[') + 1;
        if (start <= 0)
            yield break;

        int depth = 0;
        for (int i = start; i < typeName.Length; i++)
        {
            i = typeName.IndexOfAny([',', '[', ']'], i);
            if (i < 0)
                break;

            switch (typeName[i])
            {
                case ',':
                    if (depth == 0)
                    {
                        yield return typeName.Substring(start, i - start);
                        start = i + 1;
                    }
                    break;

                case '[':
                    depth++;
                    break;

                case ']':
                    if (--depth < 0)
                    {
                        yield return typeName.Substring(start, i - start);
                        yield break;
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Determines whether two <see cref="WeakType"/> instances have the same full name.
    /// </summary>
    /// <param name="left">The left <see cref="WeakType"/> instance.</param>
    /// <param name="right">The right <see cref="WeakType"/> instance.</param>
    /// <returns><c>true</c> if the full names are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(WeakType? left, WeakType? right) => left?._fullName == right?._fullName;

    /// <summary>
    /// Determines whether two <see cref="WeakType"/> instances do not have the same full name.
    /// </summary>
    /// <param name="left">The left <see cref="WeakType"/> instance.</param>
    /// <param name="right">The right <see cref="WeakType"/> instance.</param>
    /// <returns><c>true</c> if the full names are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(WeakType? left, WeakType? right) => left?._fullName != right?._fullName;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is WeakType other && other._fullName == _fullName;

    /// <inheritdoc/>
    public override int GetHashCode() => _fullName.GetHashCode();

    /// <inheritdoc/>
    public override string ToString() => _fullName;
}
