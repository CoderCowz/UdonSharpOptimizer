using System.Collections.Generic;
using UdonSharp.Compiler.Assembly;
using UdonSharp.Compiler.Assembly.Instructions;
using UdonSharp.Compiler.Emit;

namespace UdonSharpOptimizer.Optimizations
{
    internal class OPTTailCall : BaseOptimization
    {
        protected override string GUILabel => "Tail Calls";

        public override bool Enabled => OptimizerSettings.Instance.EnableTCO;

        public override void ProcessInstruction(Optimizer optimizer, List<AssemblyInstruction> instrs, int i)
        {
            // Tail call optimization
            // TODO: Properly verify this jump is to a method
            if (instrs[i] is JumpInstruction && instrs[i + 1] is RetInstruction rInst && instrs[i - 1] is Comment cInst && cInst.Comment.StartsWith("Calling "))
            {
                // Locate the corresponding push above
                int pushIdx = -1;
                for (int j = i - 1; j >= 0; j--)
                {
                    if (instrs[j] is PushInstruction pInst && pInst.PushValue.Flags == Value.ValueFlags.InternalGlobal && pInst.PushValue.DefaultValue is uint val && pInst.PushValue.UniqueID.StartsWith("__gintnl_RetAddress_") && val == rInst.InstructionAddress)
                    {
                        pushIdx = j;
                        break;
                    }
                    else if (optimizer.HasJump(j))
                    {
                        break;
                    }
                }
                if (pushIdx != -1)
                {
                    PushInstruction pInst = (PushInstruction)instrs[pushIdx];
                    instrs[pushIdx] = optimizer.TransferInstr(new Comment($"OPTTailCall: Tail call optimization, removed PUSH {pInst.PushValue.UniqueID}"), instrs[pushIdx]);
                    if (!optimizer.HasJump(rInst))
                    {
                        instrs[i + 1] = optimizer.TransferInstr(new Comment($"OPTTailCall: Tail call optimization, removed RET {rInst.RetValRef.UniqueID}"), rInst);
                        CountRemoved(optimizer, 4); // PUSH & PUSH + COPY + JUMP_INDIRECT
                    }
                    else
                    {
                        CountRemoved(optimizer, 1); // PUSH
                    }
                }
            }
        }
    }
}
