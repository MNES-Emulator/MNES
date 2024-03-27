using MNES.Core.Machine.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Mnes.Core.Machine.CPU.CpuInstruction;
using static Mnes.Core.Machine.CpuRegisters;

namespace Mnes.Core.Machine.CPU;

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

    // Add additional cycles after an instruction
    int add_cycles;

    // values that are recorded during logging
    ushort log_pc;
    byte? log_d1;
    byte? log_d2;
    long log_cyc;
    string log_message;
    CpuRegisterLog log_cpu;
    long log_inst_count;

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
        m[(ushort)(m.Cpu.Registers.S-- + 0x0100)] = value;
    }

    static void PUSH_ushort(MachineState m, ushort value)
    {
        PUSH(m, (byte)(value >> 8));
        PUSH(m, (byte)value);
    }

    static ushort PULL_ushort(MachineState m)
    {
        ushort value = PULL(m);
        value |= (ushort)((ushort)PULL(m) << 8);
        return value;
    }

    static byte PULL(MachineState m) =>
        m[(ushort)(++m.Cpu.Registers.S + 0x0100)];

    static ushort GetIndexedZeroPageIndirectAddress(MachineState m, byte arg, byte r)
    {
        // X-Indexed Zero Page Indirect https://www.pagetable.com/c64ref/6502/?tab=3#(a8,X)
        var x_target = (byte)(arg + r);

        // "var target = m.ReadUShort(x_target);", except both indexes must be in zero page
        var b_l = m[x_target];
        ushort b_h = m[(ushort)((x_target + 1) % 256)];
        b_h <<= 8;
        b_h |= b_l;
        var target = b_h;

        if (m.Settings.System.DebugMode) m.Cpu.log_message =
            $"(${arg:X2},X) = @ {x_target:X2} = {target:X4} = {m[target]:X2}";

        return target;
    }

   static ushort GetZeroPageIndirectIndexedAddress(MachineState m, byte arg, RegisterType r)
   {
      byte z_p_address_1 = arg;
      byte z_p_address_2 = (byte)((arg + 1) % 256);
      var sum = m[z_p_address_1] + m.Cpu.Registers[r];
      var carry = (sum & 0b_1_0000_0000) > 0 ? 1 : 0;
      var l_byte = (byte)sum;
      var h_byte = m[z_p_address_2] + carry;
      var target = (ushort)((h_byte << 8) | l_byte);

      // This output is wrong but it doesn't actually effect anything
      if (m.Settings.System.DebugMode) m.Cpu.log_message =
         $"(${arg:X2}),{r} = {target:X4} @ {target:X4} = {m[target]:X2}";

      return target;
   }

    #endregion

    // Some opcodes do similar things
    #region Generic Opcode Methods
    static void OpSetFlag(MachineState m, StatusFlagType flag)
    {
        m.Cpu.Registers.P |= (byte)flag;
        m.Cpu.Registers.PC++;
    }

    static void OpClearFlag(MachineState m, StatusFlagType flag)
    {
        m.Cpu.Registers.ClearFlag(flag);
        m.Cpu.Registers.PC++;
    }

    static void OpBranchOnFlagRelative(MachineState m, StatusFlagType flag)
    {
        if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${m.Cpu.Registers.PC + m[(ushort)(m.Cpu.Registers.PC + 1)] + 2:X4}";

        if (m.Cpu.Registers.HasFlag(flag))
        {
            m.Cpu.Registers.PC += m[(ushort)(m.Cpu.Registers.PC + 1)];
        }

        m.Cpu.add_cycles = 1; // Todo: Add another if branch is on another page
        m.Cpu.Registers.PC += 2;
    }

    static void OpBranchOnClearFlagRelative(MachineState m, StatusFlagType flag)
    {
        if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${m.Cpu.Registers.PC + m[(ushort)(m.Cpu.Registers.PC + 1)] + 2:X4}";

        if (!m.Cpu.Registers.HasFlag(flag))
        {
            m.Cpu.Registers.PC += m[(ushort)(m.Cpu.Registers.PC + 1)];
        }
        m.Cpu.add_cycles = 1; // Todo: Add another if branch is on another page
        m.Cpu.Registers.PC += 2;
    }

    static void OpCompare(MachineState m, byte r, byte mem)
    {
        m.Cpu.Registers.SetFlag(StatusFlagType.Negative, ((r - mem) & 0b_1000_0000) > 0);
        m.Cpu.Registers.SetFlag(StatusFlagType.Zero, r == mem);
        m.Cpu.Registers.SetFlag(StatusFlagType.Carry, mem <= r);
    }

    static void OpAddCarry(MachineState m, byte value)
    {
        var a = m.Cpu.Registers.A;
        var sum = a + value + (m.Cpu.Registers.HasFlag(StatusFlagType.Carry)? 1 : 0);
        var carry = sum > 0xFF;
        var overflow = (~(a ^ value) & (a ^ sum) & 0x80) > 0;
        m.Cpu.Registers.A = (byte)sum;
        m.Cpu.Registers.SetFlag(StatusFlagType.Carry, carry);
        m.Cpu.Registers.SetFlag(StatusFlagType.Overflow, overflow);
    }

    static void OpRollingLeftShiftMem(MachineState m, ushort target)
    {
        var prev_carry = m.Cpu.Registers.HasFlag(StatusFlagType.Carry);
        var c_flag = (m[target] & 0b_1000_0000) > 0;
        m[target] <<= 1;
        if (prev_carry) m[target] |= 0b_0000_0001;
        m.Cpu.Registers.UpdateFlags(m[target]);
        m.Cpu.Registers.SetFlag(StatusFlagType.Carry, c_flag);
    }

    static void OpRollingRightShiftMem(MachineState m, ushort target)
    {
        var prev_carry = m.Cpu.Registers.HasFlag(StatusFlagType.Carry);
        var c_flag = (m[target] & 0b_0000_0001) > 0;
        m[target] >>= 1;
        if (prev_carry) m[target] |= 0b_1000_0000;
        m.Cpu.Registers.UpdateFlags(m[target]);
        m.Cpu.Registers.SetFlag(StatusFlagType.Carry, c_flag);
    }
    // Todo; make sure all address setting functions are setting flags properly
    static void OpIncMem(MachineState m, ushort target)
    {
        m[target]++;
        m.Cpu.Registers.UpdateFlags(m[target]);
    }

    static void OpDecMem(MachineState m, ushort target)
    {
        m[target]--;
        m.Cpu.Registers.UpdateFlags(m[target]);
    }

    static void OpBit(MachineState m, byte value)
    {
        m.Cpu.Registers.SetFlag(StatusFlagType.Zero, (m.Cpu.Registers.A & value) == 0);
        m.Cpu.Registers.SetFlag(StatusFlagType.Negative, (value & 0b_1000_0000) > 0);
        m.Cpu.Registers.SetFlag(StatusFlagType.Overflow, (value & 0b_0100_0000) > 0);
    }
    #endregion

    static readonly CpuInstruction[] instructions_unordered = new CpuInstruction[] {

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
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X2} = {m[target]:X2}";
                m[target] = m.Cpu.Registers.X;
                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "JSR", OpCode = 0x20, Bytes = 3, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => { },
            m => { },
            m => { },
            m => {
                PUSH_ushort(m, (ushort)(m.Cpu.Registers.PC + 2));
                m.Cpu.Registers.PC = m.ReadUShort(m.Cpu.Registers.PC + 1);
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${m.Cpu.Registers.PC:X4}";
            },
        } },

        new() { Name = "RTS", OpCode = 0x60, Bytes = 1, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => { },
            m => { },
            m => { },
            m => {
                ushort target = PULL(m);
                target |= (ushort)((ushort)PULL(m) << 8);
                m.Cpu.Registers.PC = target;
                m.Cpu.Registers.PC += 1;
            },
        } },

        new() { Name = "NOP", OpCode = 0xEA, Bytes = 1, Process = new ProcessDelegate[] {
            m => { m.Cpu.Registers.PC++; },
        } },

        new() { Name = "SEC", OpCode = 0x38, Bytes = 1, Process = new ProcessDelegate[] {
            m => OpSetFlag(m, StatusFlagType.Carry),
        } },

        new() { Name = "BCS", OpCode = 0xB0, Bytes = 2, Process = new ProcessDelegate[] {
            m => OpBranchOnFlagRelative(m, StatusFlagType.Carry),
        } },

        new() { Name = "CLC", OpCode = 0x18, Bytes = 1, Process = new ProcessDelegate[] {
            m => OpClearFlag(m, StatusFlagType.Carry),
        } },

        new() { Name = "BCC", OpCode = 0x90, Bytes = 2, Process = new ProcessDelegate[] {
            m => OpBranchOnClearFlagRelative(m, StatusFlagType.Carry),
        } },

        new() { Name = "LDA", OpCode = 0xA9, Bytes = 2, Process = new ProcessDelegate[] {
            m => {
                m.Cpu.Registers.A = m[(ushort)(m.Cpu.Registers.PC + 1)];
                m.Cpu.Registers.PC += 2;
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"#${m.Cpu.Registers.A:X2}";
            },
        } },

        new() { Name = "BEQ", OpCode = 0xF0, Bytes = 2, Process = new ProcessDelegate[] {
            m => OpBranchOnFlagRelative(m, StatusFlagType.Zero),
        } },

        new() { Name = "BNE", OpCode = 0xD0, Bytes = 2, Process = new ProcessDelegate[] {
            m => OpBranchOnClearFlagRelative(m, StatusFlagType.Zero),
        } },

        new() { Name = "STA", OpCode = 0x85, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => {
                var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X2} = {m[target]:X2}";
                m[target] = m.Cpu.Registers.A;
                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "BIT", OpCode = 0x24, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => {
                var z_p_address = m[(ushort)(m.Cpu.Registers.PC + 1)];
                var value = m[z_p_address];
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${z_p_address:X2} = {value:X2}";
                OpBit(m, value);
                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "BVS", OpCode = 0x70, Bytes = 2, Process = new ProcessDelegate[] {
            m => OpBranchOnFlagRelative(m, StatusFlagType.Overflow),
        } },

        new() { Name = "BVC", OpCode = 0x50, Bytes = 2, Process = new ProcessDelegate[] {
            m => OpBranchOnClearFlagRelative(m, StatusFlagType.Overflow),
        } },

        new() { Name = "BPL", OpCode = 0x10, Bytes = 2, Process = new ProcessDelegate[] {
            m => OpBranchOnClearFlagRelative(m, StatusFlagType.Negative),
        } },

        new() { Name = "SEI", OpCode = 0x78, Bytes = 1, Process = new ProcessDelegate[] {
            m => OpSetFlag(m, StatusFlagType.InerruptDisable),
        } },

        new() { Name = "SED", OpCode = 0xF8, Bytes = 1, Process = new ProcessDelegate[] {
            m => OpSetFlag(m, StatusFlagType.Decimal),
        } },

        new() { Name = "PHP", OpCode = 0x08, Bytes = 1, Process = new ProcessDelegate[] {
            m => { },
            m => {
                var p = m.Cpu.Registers.P;
                p |= 0b_0010_0000;
                PUSH(m, p);
                m.Cpu.Registers.PC++;
            },
        } },

        new() { Name = "PLA", OpCode = 0x68, Bytes = 1, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => {
                m.Cpu.Registers.A = PULL(m);
                m.Cpu.Registers.PC++;
            },
        } },

        new() { Name = "AND", OpCode = 0x29, Bytes = 2, Process = new ProcessDelegate[] {
            m => {
                var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
                m.Cpu.Registers.A &= target;
                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "CMP", OpCode = 0xC9, Bytes = 2, Process = new ProcessDelegate[] {
            m => {
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"#${m[(ushort)(m.Cpu.Registers.PC + 1)]:X2}";
                OpCompare(m, m.Cpu.Registers.A, m[(ushort)(m.Cpu.Registers.PC + 1)]);
                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "CLD", OpCode = 0xD8, Bytes = 1, Process = new ProcessDelegate[] {
            m => OpClearFlag(m, StatusFlagType.Decimal),
        } },

        new() { Name = "PHA", OpCode = 0x48, Bytes = 1, Process = new ProcessDelegate[] {
            m => { },
            m => {
                PUSH(m, m.Cpu.Registers.A);
                m.Cpu.Registers.PC++;
            },
        } },

        new() { Name = "PLP", OpCode = 0x28, Bytes = 1, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => {
                var b_flag = m.Cpu.Registers.HasFlag(StatusFlagType.BFlag);
                m.Cpu.Registers.P = PULL(m);
                m.Cpu.Registers.SetFlag(StatusFlagType.BFlag, b_flag);
                m.Cpu.Registers.PC++;
            },
        } },

        new() { Name = "BMI", OpCode = 0x30, Bytes = 2, Process = new ProcessDelegate[] {
            m => OpBranchOnFlagRelative(m, StatusFlagType.Negative),
        } },

        new() { Name = "ORA", OpCode = 0x09, Bytes = 2, Process = new ProcessDelegate[] {
            m => {
                var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
                m.Cpu.Registers.A |= target;
                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "CLV", OpCode = 0xB8, Bytes = 1, Process = new ProcessDelegate[] {
            m => OpClearFlag(m, StatusFlagType.Overflow),
        } },

        new() { Name = "EOR", OpCode = 0x49, Bytes = 2, Process = new ProcessDelegate[] {
            m => {
                var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
                m.Cpu.Registers.A ^= target;
                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "ADC", OpCode = 0x69, Bytes = 2, Process = new ProcessDelegate[] {
            m => {
                var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"#${target:X2}";
                OpAddCarry(m, target);
                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "LDY", OpCode = 0xA0, Bytes = 2, Process = new ProcessDelegate[] {
            m => {
                m.Cpu.Registers.Y = m[(ushort)(m.Cpu.Registers.PC + 1)];
                m.Cpu.Registers.PC += 2;
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"#${m.Cpu.Registers.Y:X2}";
            },
        } },

        new() { Name = "CPY", OpCode = 0xC0, Bytes = 2, Process = new ProcessDelegate[] {
            m => {
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"#${m[(ushort)(m.Cpu.Registers.PC + 1)]:X2}";
                OpCompare(m, m.Cpu.Registers.Y, m[(ushort)(m.Cpu.Registers.PC + 1)]);
                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "CPX", OpCode = 0xE0, Bytes = 2, Process = new ProcessDelegate[] {
            m => {
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"#${m[(ushort)(m.Cpu.Registers.PC + 1)]:X2}";
                OpCompare(m, m.Cpu.Registers.X, m[(ushort)(m.Cpu.Registers.PC + 1)]);
                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "SBC", OpCode = 0xE9, Bytes = 2, Process = new ProcessDelegate[] {
            m => {
                var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"#${target:X2}";
                OpAddCarry(m, (byte)~target);
                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "INY", OpCode = 0xC8, Bytes = 1, Process = new ProcessDelegate[] {
            m => {
                m.Cpu.Registers.Y++;
                m.Cpu.Registers.PC += 1;
            },
        } },

        new() { Name = "INX", OpCode = 0xE8, Bytes = 1, Process = new ProcessDelegate[] {
            m => {
                m.Cpu.Registers.X++;
                m.Cpu.Registers.PC += 1;
            },
        } },

        new() { Name = "DEY", OpCode = 0x88, Bytes = 1, Process = new ProcessDelegate[] {
            m => {
                m.Cpu.Registers.Y--;
                m.Cpu.Registers.PC += 1;
            },
        } },

        new() { Name = "DEX", OpCode = 0xCA, Bytes = 1, Process = new ProcessDelegate[] {
            m => {
                m.Cpu.Registers.X--;
                m.Cpu.Registers.PC += 1;
            },
        } },

        new() { Name = "TAY", OpCode = 0xA8, Bytes = 1, Process = new ProcessDelegate[] {
            m => {
                m.Cpu.Registers.Y = m.Cpu.Registers.A;
                m.Cpu.Registers.PC += 1;
            },
        } },

        new() { Name = "TAX", OpCode = 0xAA, Bytes = 1, Process = new ProcessDelegate[] {
            m => {
                m.Cpu.Registers.X = m.Cpu.Registers.A;
                m.Cpu.Registers.PC += 1;
            },
        } },

        new() { Name = "TYA", OpCode = 0x98, Bytes = 1, Process = new ProcessDelegate[] {
            m => {
                m.Cpu.Registers.A = m.Cpu.Registers.Y;
                m.Cpu.Registers.PC += 1;
            },
        } },

        new() { Name = "TXA", OpCode = 0x8A, Bytes = 1, Process = new ProcessDelegate[] {
            m => {
                m.Cpu.Registers.A = m.Cpu.Registers.X;
                m.Cpu.Registers.PC += 1;
            },
        } },

        new() { Name = "TSX", OpCode = 0xBA, Bytes = 1, Process = new ProcessDelegate[] {
            m => {
                m.Cpu.Registers.X = m.Cpu.Registers.S;
                m.Cpu.Registers.PC += 1;
            },
        } },


        new() { Name = "STX", OpCode = 0x8E, Bytes = 3, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => {
                var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
                m[target] = m.Cpu.Registers.X;
                m.Cpu.Registers.PC += 3;
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X2} = {m.Cpu.Registers.X:X2}";
            },
        } },

        new() { Name = "TXS", OpCode = 0x9A, Bytes = 1, Process = new ProcessDelegate[] {
            m => {
                m.Cpu.Registers.S = m.Cpu.Registers.X;
                m.Cpu.Registers.PC += 1;
            },
        } },

        new() { Name = "LDX", OpCode = 0xAE, Bytes = 3, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => {
                var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
                m.Cpu.Registers.X = m[target];
                m.Cpu.Registers.PC += 3;
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X4} = {m.Cpu.Registers.X:X2}";
            },
        } },

        new() { Name = "LDA", OpCode = 0xAD, Bytes = 3, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => {
                var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
                m.Cpu.Registers.A = m[target];
                m.Cpu.Registers.PC += 3;
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X4} = {m.Cpu.Registers.A:X2}";
            },
        } },

        new() { Name = "DEC", OpCode = 0xCE, Bytes = 3, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => { },
            m => { },
            m => {
                var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
               OpDecMem(m, target);
                m.Cpu.Registers.PC += 3;
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X2} = {m.Cpu.Registers.A:X2}";
            },
        } },

        new() { Name = "RTI", OpCode = 0x40, Bytes = 1, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => { },
            m => { },
            m => {
                m.Cpu.Registers.P = PULL(m);
                m.Cpu.Registers.PC = PULL_ushort(m);
            },
        } },

        new() { Name = "LSR", OpCode = 0x4A, Bytes = 1, Process = new ProcessDelegate[] {
            m => {
                var c_flag = (m.Cpu.Registers.A & 0b_0000_0001) > 0;
                m.Cpu.Registers.A >>= 1;
                m.Cpu.Registers.SetFlag(StatusFlagType.Carry, c_flag);
                m.Cpu.Registers.PC += 1;
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"A";
            },
        } },

        new() { Name = "ASL", OpCode = 0x0A, Bytes = 1, Process = new ProcessDelegate[] {
            m => {
                var c_flag = (m.Cpu.Registers.A & 0b_1000_0000) > 0;
                m.Cpu.Registers.A <<= 1;
                m.Cpu.Registers.SetFlag(StatusFlagType.Carry, c_flag);
                m.Cpu.Registers.PC += 1;
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"A";
            },
        } },

        new() { Name = "ROR", OpCode = 0x6A, Bytes = 1, Process = new ProcessDelegate[] {
            m => {
                var prev_carry = m.Cpu.Registers.HasFlag(StatusFlagType.Carry);
                var c_flag = (m.Cpu.Registers.A & 0b_0000_0001) > 0;
                m.Cpu.Registers.A >>= 1;
                if (prev_carry) m.Cpu.Registers.A |= 0b_1000_0000;
                m.Cpu.Registers.SetFlag(StatusFlagType.Carry, c_flag);
                m.Cpu.Registers.PC += 1;
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"A";
            },
        } },

        new() { Name = "ROL", OpCode = 0x2A, Bytes = 1, Process = new ProcessDelegate[] {
            m => {
                var prev_carry = m.Cpu.Registers.HasFlag(StatusFlagType.Carry);
                var c_flag = (m.Cpu.Registers.A & 0b_1000_0000) > 0;
                m.Cpu.Registers.A <<= 1;
                if (prev_carry) m.Cpu.Registers.A |= 0b_0000_0001;
                m.Cpu.Registers.SetFlag(StatusFlagType.Carry, c_flag);
                m.Cpu.Registers.PC += 1;
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"A";
            },
        } },

        new() { Name = "LDA", OpCode = 0xA5, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => {
                var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
                m.Cpu.Registers.A = m[target];
                m.Cpu.Registers.PC += 2;
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X2} = {m.Cpu.Registers.A:X2}";
            },
        } },

        new() { Name = "STA", OpCode = 0x8D, Bytes = 3, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => { },
            m => {
                var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X4} = {m[target]:X2}";
                m[target] = m.Cpu.Registers.A;
                m.Cpu.Registers.PC += 3;
            },
        } },

        new() { Name = "LDA", OpCode = 0xA1, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => { },
            m => { },
            m => {
                var arg = m[(ushort)(m.Cpu.Registers.PC + 1)];
                var target = GetIndexedZeroPageIndirectAddress(m, arg, m.Cpu.Registers.X);
                m.Cpu.Registers.A = m[target];
                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "STA", OpCode = 0x81, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => { },
            m => { },
            m => {
                var arg = m[(ushort)(m.Cpu.Registers.PC + 1)];
                var target = GetIndexedZeroPageIndirectAddress(m, arg, m.Cpu.Registers.X);
                m[target] = m.Cpu.Registers.A;
                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "ORA", OpCode = 0x01, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => { },
            m => { },
            m => {
                var arg = m[(ushort)(m.Cpu.Registers.PC + 1)];
                var target = GetIndexedZeroPageIndirectAddress(m, arg, m.Cpu.Registers.X);
                m.Cpu.Registers.A |= m[target];
                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "AND", OpCode = 0x21, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => { },
            m => { },
            m => {
                var arg = m[(ushort)(m.Cpu.Registers.PC + 1)];
                var target = GetIndexedZeroPageIndirectAddress(m, arg, m.Cpu.Registers.X);
                m.Cpu.Registers.A &= m[target];
                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "EOR", OpCode = 0x41, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => { },
            m => { },
            m => {
                var arg = m[(ushort)(m.Cpu.Registers.PC + 1)];
                var target = GetIndexedZeroPageIndirectAddress(m, arg, m.Cpu.Registers.X);
                m.Cpu.Registers.A ^= m[target];
                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "ADC", OpCode = 0x61, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => { },
            m => { },
            m => {
                var arg = m[(ushort)(m.Cpu.Registers.PC + 1)];
                var target = GetIndexedZeroPageIndirectAddress(m, arg, m.Cpu.Registers.X);
                OpAddCarry(m, m[target]);
                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "CMP", OpCode = 0xC1, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => { },
            m => { },
            m => {
                var arg = m[(ushort)(m.Cpu.Registers.PC + 1)];
                var target = GetIndexedZeroPageIndirectAddress(m, arg, m.Cpu.Registers.X);
                OpCompare(m, m.Cpu.Registers.A, m[target]);
                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "SBC", OpCode = 0xE1, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => { },
            m => { },
            m => {
                var arg = m[(ushort)(m.Cpu.Registers.PC + 1)];
                var target = GetIndexedZeroPageIndirectAddress(m, arg, m.Cpu.Registers.X);
                OpAddCarry(m, (byte)~m[target]);
                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "LDY", OpCode = 0xA4, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => {
                var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
                m.Cpu.Registers.Y = m[target];
                m.Cpu.Registers.PC += 2;
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X2} = {m.Cpu.Registers.Y:X2}";
            },
        } },

        new() { Name = "STY", OpCode = 0x84, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => {
                var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
                m[target] = m.Cpu.Registers.Y;
                m.Cpu.Registers.PC += 2;
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X2} = {m.Cpu.Registers.Y:X2}";
            },
        } },

        new() { Name = "LDX", OpCode = 0xA6, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => {
                var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
                m.Cpu.Registers.X = m[target];
                m.Cpu.Registers.PC += 2;
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X2} = {m.Cpu.Registers.X:X2}";
            },
        } },

        new() { Name = "ORA", OpCode = 0x05, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => {
                var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
                m.Cpu.Registers.A |= m[target];
                m.Cpu.Registers.PC += 2;
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X2} = {m[target]:X2}";
            },
        } },

        new() { Name = "AND", OpCode = 0x25, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => {
                var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
                m.Cpu.Registers.A &= m[target];
                m.Cpu.Registers.PC += 2;
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X2} = {m[target]:X2}";
            },
        } },

        new() { Name = "EOR", OpCode = 0x45, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => {
                var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
                m.Cpu.Registers.A ^= m[target];
                m.Cpu.Registers.PC += 2;
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X2} = {m[target]:X2}";
            },
        } },

        new() { Name = "ADC", OpCode = 0x65, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => {
                var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
                OpAddCarry(m, (byte)m[target]);
                m.Cpu.Registers.PC += 2;
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X2} = {m[target]:X2}";
            },
        } },

        new() { Name = "CMP", OpCode = 0xC5, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => {
                var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
                OpCompare(m, m.Cpu.Registers.A, m[target]);
                m.Cpu.Registers.PC += 2;
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X2} = {m[target]:X2}";
            },
        } },

        new() { Name = "SBC", OpCode = 0xE5, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => {
                var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X2} = {m[target]:X2}";
                OpAddCarry(m, (byte)~m[target]);
                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "CPX", OpCode = 0xE4, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => {
                var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
                OpCompare(m, m.Cpu.Registers.X, m[target]);
                m.Cpu.Registers.PC += 2;
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X2} = {m[target]:X2}";
            },
        } },

        new() { Name = "CPY", OpCode = 0xC4, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => {
                var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
                OpCompare(m, m.Cpu.Registers.Y, m[target]);
                m.Cpu.Registers.PC += 2;
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X2} = {m[target]:X2}";
            },
        } },

        new() { Name = "LSR", OpCode = 0x46, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => {
                var arg = m[(ushort)(m.Cpu.Registers.PC + 1)];
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${arg:X2} = {m[arg]:X2}";

                var c_flag = (m[arg] & 0b_0000_0001) > 0;
                m[arg] >>= 1;
                m.Cpu.Registers.SetFlag(StatusFlagType.Carry, c_flag);

                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "ASL", OpCode = 0x06, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => {
                var arg = m[(ushort)(m.Cpu.Registers.PC + 1)];
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${arg:X2} = {m[arg]:X2}";

                var c_flag = (m[arg] & 0b_1000_0000) > 0;
                m[arg] <<= 1;
                m.Cpu.Registers.SetFlag(StatusFlagType.Carry, c_flag);

                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "ROR", OpCode = 0x66, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => { },
            m => { },
            m => {
                var arg = m[(ushort)(m.Cpu.Registers.PC + 1)];
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${arg:X2} = {m[arg]:X2}";

                OpRollingRightShiftMem(m, arg);

                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "ROL", OpCode = 0x26, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => { },
            m => { },
            m => {
                var arg = m[(ushort)(m.Cpu.Registers.PC + 1)];
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${arg:X2} = {m[arg]:X2}";

                OpRollingLeftShiftMem(m, arg);

                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "INC", OpCode = 0xE6, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => { },
            m => { },
            m => {
                var arg = m[(ushort)(m.Cpu.Registers.PC + 1)];
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${arg:X2} = {m[arg]:X2}";

                OpIncMem(m, arg);

                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "DEC", OpCode = 0xC6, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => { },
            m => { },
            m => {
                var arg = m[(ushort)(m.Cpu.Registers.PC + 1)];
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${arg:X2} = {m[arg]:X2}";

                OpDecMem(m, arg);

                m.Cpu.Registers.PC += 2;
            },
        } },

        new() { Name = "LDY", OpCode = 0xAC, Bytes = 3, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => {
                var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
                m.Cpu.Registers.Y = m[target];
                m.Cpu.Registers.PC += 3;
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X4} = {m.Cpu.Registers.Y:X2}";
            },
        } },

        new() { Name = "STY", OpCode = 0x8C, Bytes = 3, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => {
                var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X4} = {m[target]:X2}";
                m[target] = m.Cpu.Registers.Y;
                m.Cpu.Registers.PC += 3;
            },
        } },

        new() { Name = "BIT", OpCode = 0x2C, Bytes = 3, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => {
                var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X4} = {m[target]:X2}";
                OpBit(m, m[target]);
                m.Cpu.Registers.PC += 3;
            },
        } },

        new() { Name = "ORA", OpCode = 0x0D, Bytes = 3, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => {
                var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X4} = {m[target]:X2}";
                m.Cpu.Registers.A |= m[target];
                m.Cpu.Registers.PC += 3;
            },
        } },

        new() { Name = "AND", OpCode = 0x2D, Bytes = 3, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => {
                var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X4} = {m[target]:X2}";
                m.Cpu.Registers.A &= m[target];
                m.Cpu.Registers.PC += 3;
            },
        } },

        new() { Name = "EOR", OpCode = 0x4D, Bytes = 3, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => {
                var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X4} = {m[target]:X2}";
                m.Cpu.Registers.A ^= m[target];
                m.Cpu.Registers.PC += 3;
            },
        } },

        new() { Name = "ADC", OpCode = 0x6D, Bytes = 3, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => {
                var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X4} = {m[target]:X2}";
                OpAddCarry(m, m[target]);
                m.Cpu.Registers.PC += 3;
            },
        } },

        new() { Name = "CMP", OpCode = 0xCD, Bytes = 3, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => {
                var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X4} = {m[target]:X2}";
                OpCompare(m, m.Cpu.Registers.A, m[target]);
                m.Cpu.Registers.PC += 3;
            },
        } },

        new() { Name = "SBC", OpCode = 0xED, Bytes = 3, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => {
                var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X4} = {m[target]:X2}";
                OpAddCarry(m, (byte)~m[target]);
                m.Cpu.Registers.PC += 3;
            },
        } },

        new() { Name = "CPX", OpCode = 0xEC, Bytes = 3, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => {
                var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X4} = {m[target]:X2}";
                OpCompare(m, m.Cpu.Registers.X, m[target]);
                m.Cpu.Registers.PC += 3;
            },
        } },

        new() { Name = "CPY", OpCode = 0xCC, Bytes = 3, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => {
                var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X4} = {m[target]:X2}";
                OpCompare(m, m.Cpu.Registers.Y, m[target]);
                m.Cpu.Registers.PC += 3;
            },
        } },

        new() { Name = "LSR", OpCode = 0x4E, Bytes = 3, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => { },
            m => { },
            m => {
                var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X4} = {m[target]:X2}";
                var carry = (m[target] & 1) > 0;
                m[target] >>= 1;
               m.Cpu.Registers.SetFlag(StatusFlagType.Carry, carry);
                m.Cpu.Registers.PC += 3;
            },
        } },

        new() { Name = "ASL", OpCode = 0x0E, Bytes = 3, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => { },
            m => { },
            m => {
                var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X4} = {m[target]:X2}";
                var c_flag = (m[target] & 0b_1000_0000) > 0;
                m[target] <<= 1;
               m.Cpu.Registers.SetFlag(StatusFlagType.Carry, c_flag);
                m.Cpu.Registers.PC += 3;
            },
        } },

        new() { Name = "ROR", OpCode = 0x6E, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => { },
            m => { },
            m => {
                var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X4} = {m[target]:X2}";
                OpRollingRightShiftMem(m, target);
                m.Cpu.Registers.PC += 3;
            },
        } },

        new() { Name = "ROL", OpCode = 0x2E, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => { },
            m => { },
            m => {
                var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X4} = {m[target]:X2}";
                OpRollingLeftShiftMem(m, target);
                m.Cpu.Registers.PC += 3;
            },
        } },

        // almost certainly broken
        new() { Name = "LDA", OpCode = 0xB1, Bytes = 2, Process = new ProcessDelegate[] {
           m => { },
           m => { },
           m => { },
           m => { },
           m => {
              var address = GetZeroPageIndirectIndexedAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.Y);
              m.Cpu.Registers.A = m[address];
              m.Cpu.Registers.PC += 2;
           },
        } },

         new() { Name = "INC", OpCode = 0xEE, Bytes = 2, Process = new ProcessDelegate[] {
            m => { },
            m => { },
            m => { },
            m => { },
            m => {
                var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
                if (m.Settings.System.DebugMode) m.Cpu.log_message = $"${target:X4} = {m[target]:X2}";
                OpIncMem(m, target);
                m.Cpu.Registers.PC += 3;
            },
        } },

         new() { Name = "ORA", OpCode = 0x11, Bytes = 2, Process = new ProcessDelegate[] {
           m => { },
           m => { },
           m => { },
           m => { },
           m => {
              var address = GetZeroPageIndirectIndexedAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.Y);
              m.Cpu.Registers.A |= m[address];
              m.Cpu.Registers.PC += 2;
           },
        } },

         new() { Name = "AND", OpCode = 0x31, Bytes = 2, Process = new ProcessDelegate[] {
           m => { },
           m => { },
           m => { },
           m => { },
           m => {
              var address = GetZeroPageIndirectIndexedAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.Y);
              m.Cpu.Registers.A &= m[address];
              m.Cpu.Registers.PC += 2;
           },
        } },

         new() { Name = "EOR", OpCode = 0x51, Bytes = 2, Process = new ProcessDelegate[] {
           m => { },
           m => { },
           m => { },
           m => { },
           m => {
              var address = GetZeroPageIndirectIndexedAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.Y);
              m.Cpu.Registers.A ^= m[address];
              m.Cpu.Registers.PC += 2;
           },
        } },

         new() { Name = "ADC", OpCode = 0x71, Bytes = 2, Process = new ProcessDelegate[] {
           m => { },
           m => { },
           m => { },
           m => { },
           m => {
              var address = GetZeroPageIndirectIndexedAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.Y);
              OpAddCarry(m, m[address]);
              m.Cpu.Registers.PC += 2;
           },
        } },

         new() { Name = "CMP", OpCode = 0xD1, Bytes = 2, Process = new ProcessDelegate[] {
           m => { },
           m => { },
           m => { },
           m => { },
           m => {
              var address = GetZeroPageIndirectIndexedAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.Y);
              OpCompare(m, m.Cpu.Registers.A, m[address]);
              m.Cpu.Registers.PC += 2;
           },
        } },

         new() { Name = "SBC", OpCode = 0xF1, Bytes = 2, Process = new ProcessDelegate[] {
           m => { },
           m => { },
           m => { },
           m => { },
           m => {
              var address = GetZeroPageIndirectIndexedAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.Y);
              OpAddCarry(m, (byte)~m[address]);
              m.Cpu.Registers.PC += 2;
           },
        } },

        new() { Name = "STA", OpCode = 0x91, Bytes = 2, Process = new ProcessDelegate[] {
           m => { },
           m => { },
           m => { },
           m => { },
           m => {
              var address = GetZeroPageIndirectIndexedAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.Y);
              m[address] = m.Cpu.Registers.A;
              m.Cpu.Registers.PC += 2;
           },
        } },
    };

    public Cpu(MachineState machine)
    {
        this.machine = machine;
        foreach (var i in instructions_unordered) {
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

        // I just put InerruptDisable/0xFD here because these seem to be set in the nestest.log file by the time it gets here but I haven't found anything in documentation to say why
        Registers.P = (byte)StatusFlagType._1 | (byte)StatusFlagType.InerruptDisable;
        Registers.S = 0xFD;
    }

    public void Tick()
    {
        CycleCounter++;
        if (CurrentInstruction == null)
        {
            var opcode = machine[Registers.PC];
            CurrentInstruction = instructions[opcode];
            if (CurrentInstruction == null)
            {
                var op_x = opcode.ToString("X2");
                //PrintCpuGrid();
                throw new NotImplementedException($"Opcode {opcode:X2} not implemented. {instructions_unordered.Length}/151 opcodes are implemented!");
            }
            if (machine.Settings.System.DebugMode)
            {
                log_pc = Registers.PC;
                log_d1 = CurrentInstruction.Bytes < 2 ? null : machine[(ushort)(Registers.PC + 1)];
                log_d2 = CurrentInstruction.Bytes < 3 ? null : machine[(ushort)(Registers.PC + 2)];
                log_cyc = CycleCounter;
                log_cpu = Registers.GetLog();
                log_inst_count++;
            }
        }
        else
        {
            if (CurrentInstructionCycle < CurrentInstruction.Process.Length) CurrentInstruction.Process[CurrentInstructionCycle++](machine);
            if (CurrentInstructionCycle == CurrentInstruction.Process.Length)
            {
                if (add_cycles > 0)
                {
                    add_cycles--;
                    return;
                }
                if (machine.Settings.System.DebugMode) {
                    machine.Logger.Log(new(CurrentInstruction, log_pc, log_d1, log_d2, log_cpu, log_cyc, log_message));
                    log_message = null;
                }
                CurrentInstruction = null;
                CurrentInstructionCycle = 0;
            }
        }
    }

    void PrintCpuGrid()
    {
        StringBuilder sb = new();

        string block = "■";

        for (int i = 0; i < 0xFF; i++)
        {
            if (instructions[i] != null) sb.Append(block);
            else sb.Append(' ');
            if ((i % 16) == 15) sb.Append('\n');

        }

        Debug.WriteLine(sb.ToString());
    }
}
