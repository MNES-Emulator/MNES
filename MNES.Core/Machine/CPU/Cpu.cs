using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MNES.Core.Machine.CPU.CpuInstruction;

namespace MNES.Core.Machine.CPU
{
    // https://www.masswerk.at/6502/6502_instruction_set.html
    public class Cpu
    {
        public CpuRegisters Registers = new();
        MachineState machine;
        CpuInstruction[] instructions = new CpuInstruction[256];

        CpuInstruction[] GetInstructionsUnordered() => new CpuInstruction[] {
            new() { Name = "JMP", OpCode = 0x4C, Bytes = 3, Process = new ProcessDelegate[] { 
                (machine) => { Registers.PC++; },
            } },
            //new() { },
        };

        public Cpu(MachineState machine)
        {
            this.machine = machine;
            foreach (var i in GetInstructionsUnordered()) {
                if (instructions[i.OpCode] != null) throw new Exception($"Duplicate OpCodes: {instructions[i.OpCode].Name} and {i.Name}");
                instructions[i.OpCode] = i;
            }
        }

        public void SetPowerUpState()
        {
            Registers.PC = 0x34;
            Registers.X = 0;
            Registers.Y = 0;
            Registers.A = 0;
        }

        public void Tick()
        {
            var opcode = machine[Registers.PC];
        }
    }
}
