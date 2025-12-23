#if !NET9_0_OR_GREATER
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property, Inherited = false)]
internal sealed class OverloadResolutionPriorityAttribute(int priority) : Attribute
{
    public int Priority { get; } = priority;
}
#endif
