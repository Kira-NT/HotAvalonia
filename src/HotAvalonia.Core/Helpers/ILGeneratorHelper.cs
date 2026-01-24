using System.Reflection;
using System.Reflection.Emit;
using HotAvalonia.Reflection;

namespace HotAvalonia.Helpers;

/// <summary>
/// Provides helper methods to facilitate IL code emission.
/// </summary>
internal static class ILGeneratorHelper
{
    /// <summary>
    /// Declares a collection of local variables on the specified <see cref="ILGenerator"/>.
    /// </summary>
    /// <param name="generator">The <see cref="ILGenerator"/> on which to declare local variables.</param>
    /// <param name="variables">The collection of local variables to declare.</param>
    public static void DeclareLocals(this ILGenerator generator, IEnumerable<LocalVariableInfo> variables)
    {
        int previousVariableIndex = -1;
        foreach (LocalVariableInfo local in variables.OrderBy(static x => x.LocalIndex))
        {
            if (local.LocalIndex <= previousVariableIndex)
                ArgumentException.Throw(nameof(variables), $"Invalid or duplicate index: {local.LocalIndex}");

            while (++previousVariableIndex != local.LocalIndex)
                generator.DeclareLocal(typeof(byte));

            LocalBuilder builtIndex = generator.DeclareLocal(local.LocalType, local.IsPinned);
            if (local.LocalIndex != builtIndex.LocalIndex)
                throw new InvalidOperationException("Generator was modified; operation may not execute.");
        }
    }

    /// <summary>
    /// Defines a set of labels on the specified <see cref="ILGenerator"/>
    /// that correspond to the branch targets of an existing method body.
    /// </summary>
    /// <param name="il">The <see cref="ILGenerator"/> on which to define the labels.</param>
    /// <param name="source">A byte array containing the IL bytes of the source method body.</param>
    /// <returns>
    /// A <see cref="Dictionary{TKey, TValue}"/> that maps IL offsets from the source method body
    /// to the corresponding <see cref="Label"/> instances defined on the provided IL generator.
    /// </returns>
    public static Dictionary<int, Label> DefineLabels(this ILGenerator il, byte[]? source)
    {
        Dictionary<int, Label> labels = [];
        MethodBodyReader reader = new(source);
        while (reader.Next())
        {
            ReadOnlySpan<int> jumpTable = reader.OpCode.OperandType switch
            {
                OperandType.ShortInlineBrTarget => [reader.GetSByte()],
                OperandType.InlineBrTarget => [reader.GetInt32()],
                OperandType.InlineSwitch => reader.JumpTable,
                _ => [],
            };

            int offset = reader.BytesConsumed;
            foreach (int target in jumpTable)
            {
                int labelOffset = target + offset;
                if (!labels.ContainsKey(labelOffset))
                    labels[labelOffset] = il.DefineLabel();
            }
        }
        return labels;
    }
}
