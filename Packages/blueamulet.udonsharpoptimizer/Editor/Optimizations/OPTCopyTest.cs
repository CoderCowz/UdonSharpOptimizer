﻿using System.Collections.Generic;
using UdonSharp.Compiler.Assembly;
using UdonSharp.Compiler.Assembly.Instructions;

namespace UdonSharpOptimizer.Optimizations
{
    internal class OPTCopyTest : BaseOptimization
    {
        protected override string GUILabel => "Copy Test";

        public override bool Enabled => OptimizerSettings.Instance.CopyAndTest;

        public override void ProcessInstruction(Optimizer optimizer, List<AssemblyInstruction> instrs, int i)
        {
            // Remove Copy: Copy + JumpIf
            if (instrs[i] is CopyInstruction cInst && i < instrs.Count - 1 && instrs[i + 1] is JumpIfFalseInstruction jifInst)
            {
                if (Optimizer.IsPrivate(cInst.TargetValue) && cInst.TargetValue.UniqueID == jifInst.ConditionValue.UniqueID && !optimizer.HasJump(jifInst) && !optimizer.ReadScan(n => n == i + 1, cInst.TargetValue))
                {
                    instrs[i] = optimizer.TransferInstr(Optimizer.CopyComment("OPTCopyTest", cInst), cInst);
                    instrs[i + 1] = optimizer.TransferInstr(new JumpIfFalseInstruction(jifInst.JumpTarget, cInst.SourceValue), jifInst);
                    CountRemoved(optimizer, 3); // PUSH, PUSH, COPY
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
