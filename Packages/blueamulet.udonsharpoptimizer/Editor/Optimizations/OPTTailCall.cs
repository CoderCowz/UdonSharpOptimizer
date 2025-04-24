﻿using System.Collections.Generic;
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
            if (instrs[i] is JumpInstruction && instrs[i - 1] is Comment cInst && cInst.Comment.StartsWith("Calling "))
            {
                // Try to locate a return after the call
                RetInstruction rInst = null;
                JumpInstruction jInst = null;
                if (instrs[i + 1] is RetInstruction inst)
                {
                    rInst = inst;
                }
                else if (instrs[i + 1] is JumpInstruction inst2 && optimizer.GetJumpTarget(inst2.JumpTarget.Address) is RetInstruction inst3)
                {
                    rInst = inst3;
                    jInst = inst2;
                }

                if (rInst == null)
                {
                    return;
                }
                // Locate the corresponding push above
                int pushIdx = -1;
                uint afterCall = instrs[i + 1].InstructionAddress;
                for (int j = i - 1; j >= 0; j--)
                {
                    if (instrs[j] is PushInstruction pInst && pInst.PushValue.Flags == Value.ValueFlags.InternalGlobal && pInst.PushValue.DefaultValue is uint val && pInst.PushValue.UniqueID.StartsWith("__gintnl_RetAddress_") && val == afterCall)
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
                    CountRemoved(optimizer, 1); // PUSH
                    if (jInst != null)
                    {
                        if (!optimizer.HasJump(jInst))
                        {
                            instrs[i + 1] = optimizer.TransferInstr(new Comment($"OPTTailCall: Tail call optimization, removed JUMP to RET"), jInst);
                            CountRemoved(optimizer, 1); // JUMP
                        }
                    }
                    else if (!optimizer.HasJump(rInst))
                    {
                        instrs[i + 1] = optimizer.TransferInstr(new Comment($"OPTTailCall: Tail call optimization, removed RET {rInst.RetValRef.UniqueID}"), rInst);
                        CountRemoved(optimizer, 3); // RET (PUSH + COPY + JUMP_INDIRECT)
                    }
                }
            }
        }
        
        public override void PrePass(Optimizer optimizer, List<AssemblyInstruction> instrs)
        {
            
        }
        
        public override void Cleanup(Optimizer optimizer, List<AssemblyInstruction> instrs)
        {
            
        }
    }
}
