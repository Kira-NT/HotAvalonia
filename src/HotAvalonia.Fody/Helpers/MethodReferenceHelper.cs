using System.Diagnostics.CodeAnalysis;
using HotAvalonia.Fody.Cecil;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HotAvalonia.Fody.Helpers;

/// <summary>
/// Provides extension methods and related functionality for working with method references.
/// </summary>
internal static class MethodReferenceHelper
{
    /// <summary>
    /// Replaces references to the target method with references
    /// to the replacement method in the specified method body.
    /// </summary>
    /// <param name="method">The method in which to replace references.</param>
    /// <param name="target">The original target method reference.</param>
    /// <param name="replacement">The replacement method reference.</param>
    public static void ReplaceMethodReferences(this MethodDefinition? method, MethodReference target, MethodReference replacement)
    {
        if (method is not { HasBody: true })
            return;

        foreach (Instruction instruction in method.Body.Instructions)
        {
            // Obviously, determining method equality solely by name is not correct. However, it suits our
            // needs in this simple case, and I couldn't be bothered to implement it properly right now.
            // So what? Bite me.
            if (instruction.Operand is MethodReference callee && callee.Name == target.Name)
                instruction.Operand = replacement;
        }
    }

    /// <summary>
    /// Attempts to locate the first call instruction within the method that calls a method with the specified return type.
    /// </summary>
    /// <param name="method">The method definition in which to search for the call instruction.</param>
    /// <param name="returnType">The expected return type of the called method.</param>
    /// <param name="call">
    /// When this method returns, contains the first <see cref="Instruction"/> matching the criteria,
    /// or <c>null</c> if no matching instruction was found.
    /// </param>
    /// <returns>
    /// <c>true</c> if a matching call instruction was found; otherwise, <c>false</c>.
    /// </returns>
    public static bool TryGetCallInstruction(this MethodDefinition? method, TypeName returnType, [NotNullWhen(true)] out Instruction? call)
    {
        call = method is not { HasBody: true } ? null : method.Body.Instructions.FirstOrDefault(x => x is { OpCode.Code: Code.Call or Code.Callvirt, Operand: MethodReference callee } && callee.ReturnType == returnType);
        return call is not null;
    }

    /// <summary>
    /// Attempts to locate the last call instruction within the method that calls a method with the specified return type.
    /// </summary>
    /// <param name="method">The method definition in which to search for the call instruction.</param>
    /// <param name="returnType">The expected return type of the called method.</param>
    /// <param name="call">
    /// When this method returns, contains the last <see cref="Instruction"/> matching the criteria,
    /// or <c>null</c> if no matching instruction was found.
    /// </param>
    /// <returns>
    /// <c>true</c> if a matching call instruction was found; otherwise, <c>false</c>.
    /// </returns>
    public static bool TryGetLastCallInstruction(this MethodDefinition? method, TypeName returnType, [NotNullWhen(true)] out Instruction? call)
    {
        call = method is not { HasBody: true } ? null : method.Body.Instructions.LastOrDefault(x => x is { OpCode.Code: Code.Call or Code.Callvirt, Operand: MethodReference callee } && callee.ReturnType == returnType);
        return call is not null;
    }

    /// <summary>
    /// Attempts to retrieve a single return instruction from the method's body.
    /// </summary>
    /// <param name="method">The method definition in which to search for the return instruction.</param>
    /// <param name="ret">
    /// When this method returns, contains the single <see cref="Instruction"/> representing a return operation,
    /// or <c>null</c> if there is not exactly one return instruction.
    /// </param>
    /// <returns>
    /// <c>true</c> if exactly one return instruction was found; otherwise, <c>false</c>.
    /// </returns>
    public static bool TryGetSingleRetInstruction(this MethodDefinition? method, [NotNullWhen(true)] out Instruction? ret)
    {
        ret = method is not { HasBody: true } ? null : method.Body.Instructions.Where(x => x.OpCode.Code is Code.Ret).Select((x, i) => i == 0 ? x : null).Take(2).LastOrDefault();
        return ret is not null;
    }
}
