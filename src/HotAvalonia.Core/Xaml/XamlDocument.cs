using System.Text;

namespace HotAvalonia.Xaml;

/// <summary>
/// Represents a XAML document and its contents.
/// </summary>
public readonly struct XamlDocument
{
    /// <summary>
    /// Gets the URI associated with the XAML document.
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// Gets the stream containing the XAML content.
    /// </summary>
    public Stream Stream { get; }

    /// <inheritdoc cref="XamlDocument(string, Stream)"/>
    public XamlDocument(Uri uri, Stream stream)
    {
        Uri = uri;
        Stream = stream;
    }

    /// <inheritdoc cref="XamlDocument(string, string)"/>
    public XamlDocument(Uri uri, string xaml)
    {
        Uri = uri;
        Stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XamlDocument"/> struct.
    /// </summary>
    /// <param name="uri">The URI associated with the document.</param>
    /// <param name="stream">The stream containing the XAML content.</param>
    public XamlDocument(string uri, Stream stream)
    {
        Uri = new(uri);
        Stream = stream;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XamlDocument"/> struct.
    /// </summary>
    /// <param name="uri">The URI associated with the document.</param>
    /// <param name="xaml">The string containing the XAML content.</param>
    public XamlDocument(string uri, string xaml)
    {
        Uri = new(uri);
        Stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml));
    }
}
