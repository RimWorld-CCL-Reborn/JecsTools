using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace PawnShields
{
    public static class HarmonyExtensions
    {
        public static void SafeInsertRange(this List<CodeInstruction> instructions, int insertionIndex, IEnumerable<CodeInstruction> newInstructions,
            IEnumerable<Label> labelsToTransfer = null, IEnumerable<ExceptionBlock> blocksToTransfer = null)
        {
            var origInstruction = instructions[insertionIndex];
            instructions.InsertRange(insertionIndex, newInstructions);
            var newInstruction = instructions[insertionIndex];
            if (labelsToTransfer == null)
                newInstruction.labels.AddRange(origInstruction.labels.PopAll());
            else
                newInstruction.labels.AddRange(labelsToTransfer.Where(origInstruction.labels.Remove));
            if (blocksToTransfer == null)
                newInstruction.blocks.AddRange(origInstruction.blocks.PopAll());
            else
                newInstruction.blocks.AddRange(blocksToTransfer.Where(origInstruction.blocks.Remove));
        }

        public static bool IsBrfalse(this CodeInstruction instruction)
        {
            return instruction.opcode == OpCodes.Brfalse_S || instruction.opcode == OpCodes.Brfalse;
        }

        public static bool IsLdloc(this CodeInstruction instruction, Type localType, MethodBase method = null)
        {
            return instruction.IsLdloc() && LocalVar.From(instruction, method).LocalType == localType;
        }

        public static bool IsStloc(this CodeInstruction instruction, Type localType, MethodBase method = null)
        {
            return instruction.IsStloc() && LocalVar.From(instruction, method).LocalType == localType;
        }
    }

    public class LocalVar : LocalVariableInfo
    {
        private readonly bool pinned;
        private readonly int index;
        private readonly Type type;

        public override bool IsPinned => pinned;

        public override int LocalIndex => index;

        public override Type LocalType => type;

        protected LocalVar(bool pinned, int index, Type type)
        {
            this.pinned = pinned;
            this.index = index;
            this.type = type;
        }

        public static LocalVar From(CodeInstruction instruction, MethodBase method)
        {
            if (!(instruction.operand is LocalVariableInfo localVar)) // also matches LocalBuilder
            {
                int index;
                if (instruction.opcode == OpCodes.Ldloc_0 || instruction.opcode == OpCodes.Stloc_0)
                    index = 0;
                else if (instruction.opcode == OpCodes.Ldloc_1 || instruction.opcode == OpCodes.Stloc_1)
                    index = 1;
                else if (instruction.opcode == OpCodes.Ldloc_2 || instruction.opcode == OpCodes.Stloc_2)
                    index = 2;
                else if (instruction.opcode == OpCodes.Ldloc_3 || instruction.opcode == OpCodes.Stloc_3)
                    index = 3;
                else
                    return null;
                localVar = method.GetMethodBody().LocalVariables[index];
            }
            return new LocalVar(localVar.IsPinned, localVar.LocalIndex, localVar.LocalType);
        }

        public CodeInstruction ToLdloc()
        {
            switch (index)
            {
                case 0:
                    return new CodeInstruction(OpCodes.Ldloc_0);
                case 1:
                    return new CodeInstruction(OpCodes.Ldloc_1);
                case 2:
                    return new CodeInstruction(OpCodes.Ldloc_2);
                case 3:
                    return new CodeInstruction(OpCodes.Ldloc_3);
                default:
                    return new CodeInstruction(index <= byte.MaxValue ? OpCodes.Ldloc_S : OpCodes.Ldloc, index);
            }
        }

        public CodeInstruction ToLdloca()
        {
            return new CodeInstruction(index <= byte.MaxValue ? OpCodes.Ldloca_S : OpCodes.Ldloca, index);
        }

        public CodeInstruction ToStloc()
        {
            switch (index)
            {
                case 0:
                    return new CodeInstruction(OpCodes.Stloc_0);
                case 1:
                    return new CodeInstruction(OpCodes.Stloc_1);
                case 2:
                    return new CodeInstruction(OpCodes.Stloc_2);
                case 3:
                    return new CodeInstruction(OpCodes.Stloc_3);
                default:
                    return new CodeInstruction(index <= byte.MaxValue ? OpCodes.Stloc_S : OpCodes.Stloc, index);
            }
        }

        public override bool Equals(object obj)
        {
            return obj is LocalVar other && index == other.index;
        }

        public override int GetHashCode()
        {
            return index;
        }

        public override string ToString()
        {
            if (pinned)
            {
                return $"{type} ({index}) (pinned)";
            }
            return $"{type} ({index})";
        }
    }
}
