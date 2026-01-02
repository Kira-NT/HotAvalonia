#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_0_OR_GREATER
namespace System.Collections.Generic;

internal static class KeyValuePairPolyfill
{
    extension<TKey, TValue>(KeyValuePair<TKey, TValue> pair)
    {
        public void Deconstruct(out TKey key, out TValue value)
        {
            key = pair.Key;
            value = pair.Value;
        }
    }
}
#endif
