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
        public readonly CpuRegisters Registers = new();
        readonly MachineState machine;
        readonly CpuInstruction[] instructions = new CpuInstruction[256];

        CpuInstruction CurrentInstruction;
        int CurrentInstructionCycle;
        long CycleCounter = 6;

        // temp values used to store data across clock cycles within a single instruction
        ushort tmp_u;

        // values that are recorded during logging
        ushort log_pc;
        byte? log_d1;
        byte? log_d2;
        long log_cyc;
        string log_message;

        #region Utility Functions
        /// <summary> Set PC lower byte. </summary>
        static void PCL(MachineState m, byte value) {
            m.Cpu.Registers.PC &= 0b_1111_1111_0000_0000;
            m.Cpu.Registers.PC |= value;
        }

        /// <summary> Set PC higher byte. </summary>
        static void PCH(MachineState m, byte value) {
            m.Cpu.Registers.PC &= 0b_0000_0000_1111_1111;
            m.Cpu.Registers.PC |= (ushort)(value << 8);
        }

        static void PUSH(MachineState m, byte value)
        {
            m[m.Cpu.Registers.S++] = value;
        }

        #endregion

        static readonly CpuInstruction[] GetInstructionsUnordered = new CpuInstruction[] {
            
            new() { Name = "JMP", OpCode = 0x4C, Bytes = 3, Process = new ProcessDelegate[] { 
                m => { 
                    m.Cpu.tmp_u = m.Cpu.Registers.PC; 
                    PCL(m, m[(ushort)(m.Cpu.tmp_u + 1)]); 
                },
                m => { 
                    PCH(m, m[(ushort)(m.Cpu.tmp_u + 2)]);
                    if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${m.Cpu.Registers.PC:X4}";
                },
            } },

            new() { Name = "LDX", OpCode = 0xA2, Bytes = 2, Process = new ProcessDelegate[] { 
                m => { 
                    m.Cpu.Registers.X = m[(ushort)(m.Cpu.Registers.PC + 1)];
                    m.Cpu.Registers.PC += 2;
                    if (m.Settings.System.DebugMode) m.Cpu.log_message = $"#${m.Cpu.Registers.X:X2}";
                },
            } },

            new() { Name = "STX", OpCode = 0x86, Bytes = 2, Process = new ProcessDelegate[] {
                m => { },
                m => {
                    var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
                    m[target] = m.Cpu.Registers.X;
                    m.Cpu.Registers.PC += 2;
                    if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X2} = {m.Cpu.Registers.X:X2}";
                },
            } },

            new() { Name = "JSR", OpCode = 0x20, Bytes = 3, Process = new ProcessDelegate[] {
                m => { },
                m => { },
                m => { },
                m => { },
                m => { },
                m => {
                    m.Cpu.tmp_u = m.Cpu.Registers.PC;
                    PUSH(m, (byte)m.Cpu.tmp_u);
                    PUSH(m, (byte)(m.Cpu.tmp_u >> 8));
                    PCL(m, m[(ushort)(m.Cpu.tmp_u + 1)]);
                    PCH(m, m[(ushort)(m.Cpu.tmp_u + 2)]);
                    if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${m.Cpu.Registers.PC:X4}";
                },
            } },

            new() { Name = "NOP", OpCode = 0xEA, Bytes = 1, Process = new ProcessDelegate[] {
                m => { m.Cpu.Registers.PC++; },
            } },

            new() { Name = "SEC", OpCode = 0x38, Bytes = 1, Process = new ProcessDelegate[] {
                m => { 
                    m.Cpu.Registers.P |= (byte)CpuRegisters.StatusFlagType.Carry;
                    m.Cpu.Registers.PC++;
                },
            } },

            //new() { Name = "", OpCode = 0x00, Bytes = 0, Process = new ProcessDelegate[] {
            //    m => { },
            //} },
        };

        public Cpu(MachineState machine)
        {
            this.machine = machine;
            foreach (var i in GetInstructionsUnordered) {
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

            // I just put these here
            Registers.P = (byte)CpuRegisters.StatusFlagType._1 | (byte)CpuRegisters.StatusFlagType.InerruptDisable;
            Registers.S = 0xFD;
        }

        public void Tick()
        {
            CycleCounter++;
            if (CurrentInstruction == null)
            {
                var opcode = machine[Registers.PC];
                CurrentInstruction = instructions[opcode];
                if (CurrentInstruction == null) {
                    var op_x = opcode.ToString("X2");
                    throw new NotImplementedException($"Opcode {opcode:X2} not implemented.");
                }
                if (machine.Settings.System.DebugMode)
                {
                    log_pc = Registers.PC;
                    log_d1 = CurrentInstruction.Bytes < 2 ? null : machine[(ushort)(Registers.PC + 1)];
                    log_d2 = CurrentInstruction.Bytes < 3 ? null : machine[(ushort)(Registers.PC + 2)];
                    log_cyc = CycleCounter;
                }
            }
            else
            {
                CurrentInstruction.Process[CurrentInstructionCycle++](machine);
                if (CurrentInstructionCycle == CurrentInstruction.Process.Length)
                {
                    if (machine.Settings.System.DebugMode) {
                        machine.Logger.Log(new(CurrentInstruction, log_pc, log_d1, log_d2, Registers.GetLog(), log_cyc, log_message));
                        log_message = null;
                    }
                    if (CurrentInstruction.Unfinished)
                    {
                        throw new NotImplementedException($"Opcode {CurrentInstruction.OpCode:X2} marked as not fully implemented.");
                    }
                    CurrentInstruction = null;
                    CurrentInstructionCycle = 0;
                }
            }
        }
    }
}
