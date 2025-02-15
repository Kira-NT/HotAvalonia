using Mono.Cecil;

namespace HotAvalonia.Fody.Cecil;

/// <summary>
/// Represents a type name.
/// </summary>
internal readonly struct TypeName : IEquatable<TypeName>, IEquatable<string>, IEquatable<Type>, IEquatable<WeakType>, IEquatable<TypeReference>
{
    /// <summary>
    /// Gets the full name of the type.
    /// </summary>
    public string FullName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeName"/> struct.
    /// </summary>
    /// <param name="fullName">The full name of the type.</param>
    private TypeName(string fullName) => FullName = fullName;

    /// <summary>
    /// Implicitly converts a <see cref="TypeName"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="typeName">The <see cref="TypeName"/> instance to convert.</param>
    /// <returns>The full name of the type as a string.</returns>
    public static implicit operator string(TypeName typeName) => typeName.FullName;

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="TypeName"/>.
    /// </summary>
    /// <param name="fullName">The full name of the type.</param>
    /// <returns>A new <see cref="TypeName"/> instance.</returns>
    public static implicit operator TypeName(string fullName) => new(fullName.Contains('[') ? FixGenericTypeName(fullName) : fullName);

    /// <summary>
    /// Implicitly converts a <see cref="Type"/> to a <see cref="TypeName"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to convert.</param>
    /// <returns>A new <see cref="TypeName"/> instance representing the specified type.</returns>
    public static implicit operator TypeName(Type type) => new(type.IsGenericType && !type.IsGenericTypeDefinition ? FixGenericTypeName(type.ToString()) : type.FullName);

    /// <summary>
    /// Implicitly converts a <see cref="WeakType"/> to a <see cref="TypeName"/>.
    /// </summary>
    /// <param name="type">The <see cref="WeakType"/> to convert.</param>
    /// <returns>A new <see cref="TypeName"/> instance representing the specified type.</returns>
    public static implicit operator TypeName(WeakType type) => new(type.IsGenericType && !type.IsGenericTypeDefinition ? FixGenericTypeName(type.ToString()) : type.FullName);

    /// <summary>
    /// Implicitly converts a <see cref="TypeReference"/> to a <see cref="TypeName"/>.
    /// </summary>
    /// <param name="type">The <see cref="TypeReference"/> to convert.</param>
    /// <returns>A new <see cref="TypeName"/> instance representing the specified type.</returns>
    public static implicit operator TypeName(TypeReference type) => new(type.IsGenericInstance ? type.ToString() : type.FullName);

    /// <summary>
    /// Determines whether two <see cref="TypeName"/> instances have the same full name.
    /// </summary>
    /// <param name="left">The left <see cref="TypeName"/> instance.</param>
    /// <param name="right">The right <see cref="TypeName"/> instance.</param>
    /// <returns><c>true</c> if the full names are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(TypeName left, TypeName right) => left.FullName == right.FullName;

    /// <summary>
    /// Determines whether two <see cref="TypeName"/> instances do not have the same full name.
    /// </summary>
    /// <param name="left">The left <see cref="TypeName"/> instance.</param>
    /// <param name="right">The right <see cref="TypeName"/> instance.</param>
    /// <returns><c>true</c> if the full names are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(TypeName left, TypeName right) => left.FullName != right.FullName;

    /// <inheritdoc/>
    public bool Equals(TypeName other) => this == other;

    /// <inheritdoc/>
    public bool Equals(string other) => this == other;

    /// <inheritdoc/>
    public bool Equals(Type other) => this == other;

    /// <inheritdoc/>
    public bool Equals(WeakType other) => this == other;

    /// <inheritdoc/>
    public bool Equals(TypeReference other) => this == other;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj switch
    {
        TypeName cecilType => cecilType == this,
        Type type => type == this,
        WeakType type => type == this,
        string fullName => fullName == this,
        TypeReference typeRef => typeRef == this,
        _ => false,
    };

    /// <inheritdoc/>
    public override int GetHashCode() => FullName.GetHashCode();

    /// <inheritdoc/>
    public override string ToString() => FullName;

    /// <summary>
    /// Replaces square brackets with angle brackets in generic type names.
    /// </summary>
    /// <remarks>
    /// Cecil is weird sometimes and uses '&lt;&gt;' instead of '[]' for some reason.
    /// </remarks>
    /// <param name="name">The type name potentially containing square brackets.</param>
    /// <returns>The fixed type name with angle brackets instead of square brackets.</returns>
    private static string FixGenericTypeName(string name) => name.Replace('[', '<').Replace(']', '>');
}
