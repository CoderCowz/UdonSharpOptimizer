﻿using System.Collections.Generic;
using UdonSharp.Compiler.Assembly;

namespace UdonSharpOptimizer.Optimizations
{
    internal interface IBaseOptimization
    {
        bool Enabled { get; }

        void ResetStats();

        void OnGUI();

        void ProcessInstruction(Optimizer optimizer, List<AssemblyInstruction> instrs, int i);
        
        void PrePass(Optimizer optimizer, List<AssemblyInstruction> instrs);
        
        void Cleanup(Optimizer optimizer, List<AssemblyInstruction> instrs);
    }
}
