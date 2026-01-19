#if NETSTANDARD2_0
using System.Runtime.Serialization;

namespace System.Runtime.CompilerServices;

internal static class RuntimeHelpersPolyfill
{
    extension(RuntimeHelpers)
    {
        public static object GetUninitializedObject(Type type)
            => FormatterServices.GetUninitializedObject(type);
    }
}
#endif
