﻿using System.Collections.Generic;

namespace IL2Asm.Optimizer.x86_RealMode
{
    public class SimplifyConstants
    {
        private static char[] _split = new char[] { ' ', ';' };

        private static List<string> aluInstructions = new List<string>(
            new string[] { "add", "sub", "and", "or", "xor", "not", "neg" }
            );

        public class MovInstruction
        {
            public int Line;
            public string Constant;

            public MovInstruction()
            {
                Constant = null;
            }

            public void Reset()
            {
                Constant = null;
            }
        }

        public static void ProcessAssembly(List<string> assembly)
        {
            Dictionary<string, MovInstruction> registers = new Dictionary<string, MovInstruction>();
            registers.Add("ax", new MovInstruction());
            registers.Add("bx", new MovInstruction());
            registers.Add("cx", new MovInstruction());
            registers.Add("dx", new MovInstruction());
            registers.Add("si", new MovInstruction());
            registers.Add("di", new MovInstruction());
            //registers.Add("bp", new MovInstruction());

            for (int i = 0; i < assembly.Count; i++)
            {
                var instruction = assembly[i].Trim();

                // any time we could have jumped here then reset the register state
                if (instruction.EndsWith(":") /*|| instruction.StartsWith("j")*/ || instruction.StartsWith("call"))
                {
                    foreach (var register in registers) register.Value.Reset();
                    continue;
                }

                var split = instruction.Split(_split);
                for (int j = 0; j < split.Length; j++) split[j] = split[j].Trim(',');

                if (split[0] == "mov")
                {
                    if (registers.ContainsKey(split[1]))
                    {
                        if (int.TryParse(split[2], out int constant))
                        {
                            registers[split[1]].Constant = split[2];
                            registers[split[1]].Line = i;

                            continue;
                        }
                        else if (split[2] == "cx" || split[2] == "dx" || split[2] == "di" || split[2] == "si")
                        {
                            registers[split[1]].Constant = split[2];
                            registers[split[1]].Line = i;

                            continue;
                        }
                        else registers[split[1]].Reset();
                    }
                }
                
                if (split[0] == "pop")
                {
                    if (split[1] == "bp") continue;
                    if (split[1] == "es") continue;
                    registers[split[1]].Reset();
                }
                else
                {
                    //if (instruction.Contains("[")) continue;    // too complex for now

                    bool madeChanges = false;
                    for (int j = 2; j < split.Length; j++)
                    {
                        if (registers.TryGetValue(split[j], out var reg) && reg.Constant != null)
                        {
                            split[j] = reg.Constant.ToString();
                            reg.Reset();
                            madeChanges = true;

                            assembly[reg.Line] = "; " + assembly[reg.Line];
                        }
                    }
                    
                    if (madeChanges)
                    {
                        string result = string.Format($"    {split[0]} {split[1]}");
                        if (split[1].StartsWith("[") && (split[2] == "-" || split[2] == "+")) 
                            result = $"    {split[0]} word {split[1]} {split[2]} {split[3]}, {split[4]}";
                        else if (split.Length == 3) result += ", " + split[2] + " ; " + assembly[i];
                        assembly[i] = result;
                    }
                }
                
                if (aluInstructions.Contains(split[0]))
                {
                    registers["ax"].Reset();
                }
            }
        }
    }
}