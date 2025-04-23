using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using UdonSharp.Compiler.Assembly;
using UdonSharp.Compiler.Assembly.Instructions;
using UdonSharp.Compiler.Emit;
using UnityEngine;

namespace UdonSharpOptimizer.Optimizations
{
    internal class OPTDeadFunction : BaseOptimization
    {
        protected override string GUILabel => "Dead Functions";

        public override bool Enabled => OptimizerSettings.Instance.EnableDFO;
        
        private readonly ConcurrentDictionary<int, Dictionary<uint, bool>> _knownFunctionsJumps = new();
        private readonly ConcurrentDictionary<int, List<uint>> _knownFunctionsData = new();

        public override void ProcessInstruction(Optimizer optimizer, List<AssemblyInstruction> instrs, int i)
        {
            var inst = instrs[i];
            _knownFunctionsData.TryGetValue(instrs.GetHashCode(), out var dict2);
            if (dict2 != null)
            {
                if (dict2.Contains(inst.InstructionAddress))
                {
                    var comment = new Comment("OPTDeadFunction Data");
                    instrs[i] = optimizer.TransferInstr(comment, inst);
                    return;
                }
            }
            _knownFunctionsJumps.TryGetValue(instrs.GetHashCode(), out var dict);
            if (dict != null)
            {
                if (dict.ContainsKey(inst.InstructionAddress) && dict[inst.InstructionAddress])
                {
                    var comment = new Comment("OPTDeadFunction Call");
                    instrs[i] = optimizer.TransferInstr(comment, inst);
                }
            }
        }

        public override void PrePass(Optimizer optimizer, List<AssemblyInstruction> instrs)
        {
            for (var i = 0; i < instrs.Count; i++)
            {
                if (instrs[i] is JumpInstruction funcJump && instrs[i - 1] is Comment cInst &&
                    cInst.Comment.StartsWith("Calling "))
                {
                    // TODO Account for methods that return values and take params
                    var addr = funcJump.JumpTarget.Address;
                    var headMethod = -1;
                    for (var j = 0; j < instrs.Count; j++)
                    {
                        if (instrs[j].InstructionAddress != addr) continue;
                        headMethod = j;
                        break;
                    }

                    if (headMethod != -1)
                    {
                        var afterComments = -1;
                        for (var j = headMethod; j < instrs.Count; j++)
                        {
                            if (instrs[j] is Comment) continue;
                            afterComments = j;
                            break;
                        }

                        if (afterComments != -1)
                        {
                            var inst = instrs[afterComments];
                            var dict = _knownFunctionsJumps.GetOrAdd(instrs.GetHashCode(), new Dictionary<uint, bool>());
                            if (!dict.ContainsKey(inst.InstructionAddress))
                            {
                                if (inst is RetInstruction)
                                {
                                    var dict2 = _knownFunctionsData.GetOrAdd(instrs.GetHashCode(), new List<uint>());

                                    var methodName = new StringBuilder();
                                    var nameLoc = afterComments - 2;
                                    if (nameLoc > 0 && instrs[nameLoc] is Comment comment)
                                    {
                                        methodName.Append(comment.Comment);
                                    }

                                    Debug.LogWarning(
                                        $"[Experimental Optimizer] [{methodName}] Method Address points to:\n{inst}");
                                    dict.Add(inst.InstructionAddress, true);
                                    dict2.Add(inst.InstructionAddress);
                                }
                                else
                                {
                                    dict.Add(inst.InstructionAddress, false);
                                }
                            }

                            if (!dict.ContainsKey(funcJump.InstructionAddress))
                            {
                                dict.Add(funcJump.InstructionAddress, dict[inst.InstructionAddress]);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[Experimental Optimizer] Method Address does not point into list!: {addr}");
                    }
                }
            }
        }

        public override void Cleanup(Optimizer optimizer, List<AssemblyInstruction> instrs)
        {
            _knownFunctionsJumps.TryRemove(instrs.GetHashCode(), out _);
            _knownFunctionsData.TryRemove(instrs.GetHashCode(), out _);
        }
    }
}