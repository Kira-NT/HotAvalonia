using System.Diagnostics.CodeAnalysis;

namespace HotAvalonia.Fody.Helpers;

/// <summary>
/// Provides helper methods for working with strings.
/// </summary>
internal static class StringHelper
{
    /// <summary>
    /// Attempts to decode the provided Base64-encoded string into a byte array.
    /// </summary>
    /// <param name="value">The Base64-encoded string to decode.</param>
    /// <param name="bytes">
    /// When this method returns, contains the decoded byte array
    /// if the conversion succeeded; otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the string was successfully decoded;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool TryGetBase64Bytes(string value, [NotNullWhen(true)] out byte[]? bytes)
    {
        try
        {
            bytes = Convert.FromBase64String(value.Trim());
            return true;
        }
        catch
        {
            bytes = null;
            return false;
        }
    }
}
