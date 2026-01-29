#if !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;

namespace System.Net;

internal static class IPEndPointPolyfill
{
    extension(IPEndPoint)
    {
        public static bool TryParse(string s, [NotNullWhen(true)] out IPEndPoint? result)
        {
            int i = (s ??= "").LastIndexOf(':') switch
            {
                int x and > 0 when s[x - 1] == ']' || s.IndexOf(':') == x => x,
                _ => s.Length,
            };

            ushort port = 0;
            if (IPAddress.TryParse(s[..i], out IPAddress? ip) && (i == s.Length || ushort.TryParse(s[(i + 1)..], out port)))
            {
                result = new(ip, port);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public static IPEndPoint Parse(string s)
        {
            ArgumentNullException.ThrowIfNull(s);
            if (TryParse(s, out IPEndPoint? result))
                return result;

            FormatException.Throw();
            return null;
        }
    }
}
#endif
