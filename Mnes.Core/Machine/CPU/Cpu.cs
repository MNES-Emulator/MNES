using Mnes.Core.Machine.Logging;
using Mnes.Core.Utility;
using static Mnes.Core.Machine.CPU.CpuInstruction;
using static Mnes.Core.Machine.CPU.CpuRegisters;

namespace Mnes.Core.Machine.CPU;

// https://www.masswerk.at/6502/6502_instruction_set.html
public sealed class Cpu {
   readonly MachineState _machine;
   readonly CpuInstruction[] _instructions = new CpuInstruction[256];

   CpuInstruction _currentInstruction;
   int _current_instruction_cycle;
   long _cycle_counter = 6;

   // temp values used to store data across clock cycles within a single instruction
   ushort _tmp_u;

   // Add additional cycles after an instruction
   int _add_cycles;

   // Skip cycles (used by OAM DMA when copying a page over to VRAM)
   public int SuspendCycles;

   // values that are recorded during logging
   ushort _log_pc;
   byte? _log_d1;
   byte? _log_d2;
   long _log_cyc;
   string _log_message;
   CpuRegisterLog _log_cpu;
   long _log_inst_count;

   public CpuRegisters Registers { get; } = new();

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

   static void PUSH(MachineState m, byte value) {
      if (m.Cpu.Registers.S == 0) throw new Exception("NES stack overflow.");
      m[(ushort)(m.Cpu.Registers.S-- + 0x0100)] = value;
   }

   static byte PULL(MachineState m) {
      if (m.Cpu.Registers.S == 0xFF) throw new Exception("NES stack underflow.");
      return m[(ushort)(++m.Cpu.Registers.S + 0x0100)];
   }

   static void PUSH_ushort(MachineState m, ushort value) {
      PUSH(m, (byte)(value >> 8));
      PUSH(m, (byte)value);
   }

   static ushort PULL_ushort(MachineState m) {
      ushort value = PULL(m);
      value |= (ushort)(PULL(m) << 8);
      return value;
   }

   static ushort GetIndexedZeroPageIndirectAddress(MachineState m, byte arg, RegisterType r) {
      // X-Indexed Zero Page Indirect https://www.pagetable.com/c64ref/6502/?tab=3#(a8,X)
      var x_target = (byte)(arg + m.Cpu.Registers[r]);

      // "var target = m.ReadUShort(x_target);", except both indexes must be in zero page
      var b_l = m[x_target];
      ushort b_h = m[(ushort)((x_target + 1) % 256)];
      b_h <<= 8;
      b_h |= b_l;
      var target = b_h;

      if (m.Settings.System.DebugMode) m.Cpu._log_message =
         $"(${arg:X2},{r.Name}) = @ {x_target:X2} = {target:X4} = {m[target]:X2}";

      return target;
   }

   static ushort GetZeroPageIndirectIndexedAddress(MachineState m, byte arg, RegisterType r) {
      var z_p_address_1 = arg;
      var z_p_address_2 = (byte)((arg + 1) % 256);
      var sum = m[z_p_address_1] + m.Cpu.Registers[r];
      var carry = (sum & 0b_1_0000_0000) > 0 ? 1 : 0;
      var l_byte = (byte)sum;
      var h_byte = m[z_p_address_2] + carry;
      var target = (ushort)((h_byte << 8) | l_byte);

      // This output is wrong but it doesn't actually effect anything
      if (m.Settings.System.DebugMode) m.Cpu._log_message =
        $"(${arg:X2}),{r.Name} = {target:X4} @ {target:X4} = {m[target]:X2}";

      return target;
   }

   static ushort GetIndexedAbsoluteAddress(MachineState m, ushort arg, RegisterType r) {
      var address = (ushort)(arg + m.Cpu.Registers[r]);
      if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${arg:X4},{r.Name} @ {address:X4} = {m[address]:X2}";
      return address;
   }

   static ushort GetIndexedZeroPageAddress(MachineState m, byte arg, RegisterType r) {
      var address = (byte)(arg + m.Cpu.Registers[r]);
      if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${arg:X2},{r.Name} @ {arg:X2} = {m[address]:X2}";
      return address;
   }

   #endregion

   // Some opcodes do similar things
   #region Generic Opcode Methods
   static void OpSetFlag(MachineState m, StatusFlag flag) {
      m.Cpu.Registers.P |= flag;
      m.Cpu.Registers.PC++;
   }

   static void OpClearFlag(MachineState m, StatusFlag flag) {
      m.Cpu.Registers.ClearFlag(flag);
      m.Cpu.Registers.PC++;
   }

   static void OpBranchOnFlagRelative(MachineState m, StatusFlag flag) {
      var offset = Convert.ToInt32((sbyte)m[(ushort)(m.Cpu.Registers.PC + 1)]);
      if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${m.Cpu.Registers.PC + offset + 2:X4}";

      if (m.Cpu.Registers.HasFlag(flag)) {
         var pc = (int)m.Cpu.Registers.PC;
         pc += offset;
         m.Cpu.Registers.PC = (ushort)pc;
      }

      m.Cpu._add_cycles = 1; // Todo: Add another if branch is on another page
      m.Cpu.Registers.PC += 2;
   }

   static void OpBranchOnClearFlagRelative(MachineState m, StatusFlag flag) {
      var offset = Convert.ToInt32((sbyte)m[(ushort)(m.Cpu.Registers.PC + 1)]);
      if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${m.Cpu.Registers.PC + offset + 2:X4}";

      if (!m.Cpu.Registers.HasFlag(flag)) {
         var pc = (int)m.Cpu.Registers.PC;
         pc += offset;
         m.Cpu.Registers.PC = (ushort)pc;
      }

      m.Cpu._add_cycles = 1; // Todo: Add another if branch is on another page
      m.Cpu.Registers.PC += 2;
   }

   static void OpCompare(MachineState m, byte r, byte mem) {
      m.Cpu.Registers.SetFlag(StatusFlag.Negative, ((r - mem) & BitFlags.F7) > 0);
      m.Cpu.Registers.SetFlag(StatusFlag.Zero, r == mem);
      m.Cpu.Registers.SetFlag(StatusFlag.Carry, mem <= r);
   }

   static void OpAddCarry(MachineState m, byte value) {
      var a = m.Cpu.Registers.A;
      var sum = a + value + (m.Cpu.Registers.HasFlag(StatusFlag.Carry)? 1 : 0);
      var carry = sum > 0xFF;
      var overflow = (~(a ^ value) & (a ^ sum) & 0x80) > 0;
      m.Cpu.Registers.A = (byte)sum;
      m.Cpu.Registers.SetFlag(StatusFlag.Carry, carry);
      m.Cpu.Registers.SetFlag(StatusFlag.Overflow, overflow);
   }

   static void OpRollingLeftShiftMem(MachineState m, ushort target) {
      var prev_carry = m.Cpu.Registers.HasFlag(StatusFlag.Carry);
      var c_flag = m[target].HasBit(7);
      m[target] <<= 1;
      if (prev_carry) m[target] |= BitFlags.F0;
      m.Cpu.Registers.UpdateFlags(m[target]);
      m.Cpu.Registers.SetFlag(StatusFlag.Carry, c_flag);
   }

   static void OpRollingRightShiftMem(MachineState m, ushort target) {
      var prev_carry = m.Cpu.Registers.HasFlag(StatusFlag.Carry);
      var c_flag = m[target].HasBit(0);
      m[target] >>= 1;
      if (prev_carry) m[target] |= BitFlags.F7;
      m.Cpu.Registers.UpdateFlags(m[target]);
      m.Cpu.Registers.SetFlag(StatusFlag.Carry, c_flag);
   }

   static void OpIncMem(MachineState m, ushort target) {
      m[target]++;
      m.Cpu.Registers.UpdateFlags(m[target]);
   }

   static void OpDecMem(MachineState m, ushort target) {
      m[target]--;
      m.Cpu.Registers.UpdateFlags(m[target]);
   }

   static void OpBit(MachineState m, byte value) {
      m.Cpu.Registers.SetFlag(StatusFlag.Zero, (m.Cpu.Registers.A & value) == 0);
      m.Cpu.Registers.SetFlag(StatusFlag.Negative, value.HasBit(7));
      m.Cpu.Registers.SetFlag(StatusFlag.Overflow, value.HasBit(6));
   }
   #endregion

   static readonly CpuInstruction[] instructions_unordered = new CpuInstruction[] {
      new() { Name = "JMP", OpCode = 0x4C, Bytes = 3, Process = new ProcessDelegate[] {
         m => {
            m.Cpu._tmp_u = m.Cpu.Registers.PC;
            PCL(m, m[(ushort)(m.Cpu._tmp_u + 1)]);
         },
         m => {
            PCH(m, m[(ushort)(m.Cpu._tmp_u + 2)]);
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${m.Cpu.Registers.PC:X4}";
         },
      } },

      new() { Name = "LDX", OpCode = 0xA2, Bytes = 2, Process = new ProcessDelegate[] {
         m => {
            m.Cpu.Registers.X = m[(ushort)(m.Cpu.Registers.PC + 1)];
            m.Cpu.Registers.PC += 2;
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"#${m.Cpu.Registers.X:X2}";
         },
      } },

      new() { Name = "STX", OpCode = 0x86, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => {
            var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X2} = {m[target]:X2}";
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
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${m.Cpu.Registers.PC:X4}";
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
            target |= (ushort)(PULL(m) << 8);
            m.Cpu.Registers.PC = target;
            m.Cpu.Registers.PC += 1;
         },
      } },

      new() { Name = "NOP", OpCode = 0xEA, Bytes = 1, Process = new ProcessDelegate[] {
         m => { m.Cpu.Registers.PC++; },
      } },

      new() { Name = "SEC", OpCode = 0x38, Bytes = 1, Process = new ProcessDelegate[] {
         m => OpSetFlag(m, StatusFlag.Carry),
      } },

      new() { Name = "BCS", OpCode = 0xB0, Bytes = 2, Process = new ProcessDelegate[] {
         m => OpBranchOnFlagRelative(m, StatusFlag.Carry),
      } },

      new() { Name = "CLC", OpCode = 0x18, Bytes = 1, Process = new ProcessDelegate[] {
         m => OpClearFlag(m, StatusFlag.Carry),
      } },

      new() { Name = "BCC", OpCode = 0x90, Bytes = 2, Process = new ProcessDelegate[] {
         m => OpBranchOnClearFlagRelative(m, StatusFlag.Carry),
      } },

      new() { Name = "LDA", OpCode = 0xA9, Bytes = 2, Process = new ProcessDelegate[] {
         m => {
            m.Cpu.Registers.A = m[(ushort)(m.Cpu.Registers.PC + 1)];
            m.Cpu.Registers.PC += 2;
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"#${m.Cpu.Registers.A:X2}";
         },
      } },

      new() { Name = "BEQ", OpCode = 0xF0, Bytes = 2, Process = new ProcessDelegate[] {
         m => OpBranchOnFlagRelative(m, StatusFlag.Zero),
      } },

      new() { Name = "BNE", OpCode = 0xD0, Bytes = 2, Process = new ProcessDelegate[] {
         m => OpBranchOnClearFlagRelative(m, StatusFlag.Zero),
      } },

      new() { Name = "STA", OpCode = 0x85, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => {
            var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X2} = {m[target]:X2}";
            m[target] = m.Cpu.Registers.A;
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "BIT", OpCode = 0x24, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => {
            var z_p_address = m[(ushort)(m.Cpu.Registers.PC + 1)];
            var value = m[z_p_address];
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${z_p_address:X2} = {value:X2}";
            OpBit(m, value);
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "BVS", OpCode = 0x70, Bytes = 2, Process = new ProcessDelegate[] {
         m => OpBranchOnFlagRelative(m, StatusFlag.Overflow),
      } },

      new() { Name = "BVC", OpCode = 0x50, Bytes = 2, Process = new ProcessDelegate[] {
         m => OpBranchOnClearFlagRelative(m, StatusFlag.Overflow),
      } },

      new() { Name = "BPL", OpCode = 0x10, Bytes = 2, Process = new ProcessDelegate[] {
         m => OpBranchOnClearFlagRelative(m, StatusFlag.Negative),
      } },

      new() { Name = "SEI", OpCode = 0x78, Bytes = 1, Process = new ProcessDelegate[] {
         m => OpSetFlag(m, StatusFlag.InterruptDisable),
      } },

      new() { Name = "SED", OpCode = 0xF8, Bytes = 1, Process = new ProcessDelegate[] {
         m => OpSetFlag(m, StatusFlag.Decimal),
      } },

      new() { Name = "PHP", OpCode = 0x08, Bytes = 1, Process = new ProcessDelegate[] {
         m => { },
         m => {
            var p = m.Cpu.Registers.P;
            p |= BitFlags.F5;
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
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"#${m[(ushort)(m.Cpu.Registers.PC + 1)]:X2}";
            OpCompare(m, m.Cpu.Registers.A, m[(ushort)(m.Cpu.Registers.PC + 1)]);
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "CLD", OpCode = 0xD8, Bytes = 1, Process = new ProcessDelegate[] {
         m => OpClearFlag(m, StatusFlag.Decimal),
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
            var b_flag = m.Cpu.Registers.HasFlag(StatusFlag.BFlag);
            m.Cpu.Registers.P = PULL(m);
            m.Cpu.Registers.SetFlag(StatusFlag.BFlag, b_flag);
            m.Cpu.Registers.PC++;
         },
      } },

      new() { Name = "BMI", OpCode = 0x30, Bytes = 2, Process = new ProcessDelegate[] {
         m => OpBranchOnFlagRelative(m, StatusFlag.Negative),
      } },

      new() { Name = "ORA", OpCode = 0x09, Bytes = 2, Process = new ProcessDelegate[] {
         m => {
            var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
            m.Cpu.Registers.A |= target;
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "CLV", OpCode = 0xB8, Bytes = 1, Process = new ProcessDelegate[] {
         m => OpClearFlag(m, StatusFlag.Overflow),
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
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"#${target:X2}";
            OpAddCarry(m, target);
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "LDY", OpCode = 0xA0, Bytes = 2, Process = new ProcessDelegate[] {
         m => {
            m.Cpu.Registers.Y = m[(ushort)(m.Cpu.Registers.PC + 1)];
            m.Cpu.Registers.PC += 2;
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"#${m.Cpu.Registers.Y:X2}";
         },
      } },

      new() { Name = "CPY", OpCode = 0xC0, Bytes = 2, Process = new ProcessDelegate[] {
         m => {
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"#${m[(ushort)(m.Cpu.Registers.PC + 1)]:X2}";
            OpCompare(m, m.Cpu.Registers.Y, m[(ushort)(m.Cpu.Registers.PC + 1)]);
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "CPX", OpCode = 0xE0, Bytes = 2, Process = new ProcessDelegate[] {
         m => {
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"#${m[(ushort)(m.Cpu.Registers.PC + 1)]:X2}";
            OpCompare(m, m.Cpu.Registers.X, m[(ushort)(m.Cpu.Registers.PC + 1)]);
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "SBC", OpCode = 0xE9, Bytes = 2, Process = new ProcessDelegate[] {
         m => {
            var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"#${target:X2}";
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
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X2} = {m.Cpu.Registers.X:X2}";
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
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X4} = {m.Cpu.Registers.X:X2}";
         },
      } },

      new() { Name = "LDA", OpCode = 0xAD, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
            m.Cpu.Registers.A = m[target];
            m.Cpu.Registers.PC += 3;
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X4} = {m.Cpu.Registers.A:X2}";
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
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X2} = {m.Cpu.Registers.A:X2}";
         },
      } },

      new() { Name = "RTI", OpCode = 0x40, Bytes = 1, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => { },
         m => { },
         m => {
            m.Cpu.Registers.PC = PULL_ushort(m);
            m.Cpu.Registers.P = PULL(m);
         },
      } },

      new() { Name = "LSR", OpCode = 0x4A, Bytes = 1, Process = new ProcessDelegate[] {
         m => {
            m.Cpu.Registers.A >>= 1;
            m.Cpu.Registers.SetFlag(StatusFlag.Carry, m.Cpu.Registers.A.HasBit(0));
            m.Cpu.Registers.PC += 1;
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"A";
         },
      } },

      new() { Name = "ASL", OpCode = 0x0A, Bytes = 1, Process = new ProcessDelegate[] {
         m => {
            var c_flag = m.Cpu.Registers.A.HasBit(7);
            m.Cpu.Registers.A <<= 1;
            m.Cpu.Registers.SetFlag(StatusFlag.Carry, c_flag);
            m.Cpu.Registers.PC += 1;
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"A";
         },
      } },

      new() { Name = "ROR", OpCode = 0x6A, Bytes = 1, Process = new ProcessDelegate[] {
         m => {
            var prev_carry = m.Cpu.Registers.HasFlag(StatusFlag.Carry);
            var c_flag = m.Cpu.Registers.A.HasBit(0);
            m.Cpu.Registers.A >>= 1;
            if (prev_carry) m.Cpu.Registers.A |= BitFlags.F7;
            m.Cpu.Registers.SetFlag(StatusFlag.Carry, c_flag);
            m.Cpu.Registers.PC += 1;
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"A";
         },
      } },

      new() { Name = "ROL", OpCode = 0x2A, Bytes = 1, Process = new ProcessDelegate[] {
         m => {
            var prev_carry = m.Cpu.Registers.HasFlag(StatusFlag.Carry);
            var c_flag = m.Cpu.Registers.A.HasBit(7);
            m.Cpu.Registers.A <<= 1;
            if (prev_carry) m.Cpu.Registers.A |= BitFlags.F0;
            m.Cpu.Registers.SetFlag(StatusFlag.Carry, c_flag);
            m.Cpu.Registers.PC += 1;
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"A";
         },
      } },

      new() { Name = "LDA", OpCode = 0xA5, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => {
            var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
            m.Cpu.Registers.A = m[target];
            m.Cpu.Registers.PC += 2;
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X2} = {m.Cpu.Registers.A:X2}";
         },
      } },

      new() { Name = "STA", OpCode = 0x8D, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => { },
         m => {
            var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X4} = {m[target]:X2}";
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
            var target = GetIndexedZeroPageIndirectAddress(m, arg, RegisterType.X);
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
            var target = GetIndexedZeroPageIndirectAddress(m, arg, RegisterType.X);
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
            var target = GetIndexedZeroPageIndirectAddress(m, arg, RegisterType.X);
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
            var target = GetIndexedZeroPageIndirectAddress(m, arg, RegisterType.X);
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
            var target = GetIndexedZeroPageIndirectAddress(m, arg, RegisterType.X);
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
            var target = GetIndexedZeroPageIndirectAddress(m, arg, RegisterType.X);
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
            var target = GetIndexedZeroPageIndirectAddress(m, arg, RegisterType.X);
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
            var target = GetIndexedZeroPageIndirectAddress(m, arg, RegisterType.X);
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
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X2} = {m.Cpu.Registers.Y:X2}";
         },
      } },

      new() { Name = "STY", OpCode = 0x84, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => {
            var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
            m[target] = m.Cpu.Registers.Y;
            m.Cpu.Registers.PC += 2;
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X2} = {m.Cpu.Registers.Y:X2}";
         },
      } },

      new() { Name = "LDX", OpCode = 0xA6, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => {
            var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
            m.Cpu.Registers.X = m[target];
            m.Cpu.Registers.PC += 2;
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X2} = {m.Cpu.Registers.X:X2}";
         },
      } },

      new() { Name = "ORA", OpCode = 0x05, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => {
            var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
            m.Cpu.Registers.A |= m[target];
            m.Cpu.Registers.PC += 2;
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X2} = {m[target]:X2}";
         },
      } },

      new() { Name = "AND", OpCode = 0x25, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => {
            var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
            m.Cpu.Registers.A &= m[target];
            m.Cpu.Registers.PC += 2;
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X2} = {m[target]:X2}";
         },
      } },

      new() { Name = "EOR", OpCode = 0x45, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => {
            var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
            m.Cpu.Registers.A ^= m[target];
            m.Cpu.Registers.PC += 2;
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X2} = {m[target]:X2}";
         },
      } },

      new() { Name = "ADC", OpCode = 0x65, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => {
            var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
            OpAddCarry(m, m[target]);
            m.Cpu.Registers.PC += 2;
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X2} = {m[target]:X2}";
         },
      } },

      new() { Name = "CMP", OpCode = 0xC5, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => {
            var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
            OpCompare(m, m.Cpu.Registers.A, m[target]);
            m.Cpu.Registers.PC += 2;
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X2} = {m[target]:X2}";
         },
      } },

      new() { Name = "SBC", OpCode = 0xE5, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => {
            var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X2} = {m[target]:X2}";
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
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X2} = {m[target]:X2}";
         },
      } },

      new() { Name = "CPY", OpCode = 0xC4, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => {
            var target = m[(ushort)(m.Cpu.Registers.PC + 1)];
            OpCompare(m, m.Cpu.Registers.Y, m[target]);
            m.Cpu.Registers.PC += 2;
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X2} = {m[target]:X2}";
         },
      } },

      new() { Name = "LSR", OpCode = 0x46, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => {
            var arg = m[(ushort)(m.Cpu.Registers.PC + 1)];
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${arg:X2} = {m[arg]:X2}";

            var c_flag = m[arg].HasBit(0);
            m[arg] >>= 1;
            m.Cpu.Registers.SetFlag(StatusFlag.Carry, c_flag);

            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "ASL", OpCode = 0x06, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => {
            var arg = m[(ushort)(m.Cpu.Registers.PC + 1)];
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${arg:X2} = {m[arg]:X2}";

            var c_flag = m[arg].HasBit(7);
            m[arg] <<= 1;
            m.Cpu.Registers.SetFlag(StatusFlag.Carry, c_flag);

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
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${arg:X2} = {m[arg]:X2}";

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
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${arg:X2} = {m[arg]:X2}";

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
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${arg:X2} = {m[arg]:X2}";

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
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${arg:X2} = {m[arg]:X2}";

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
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X4} = {m.Cpu.Registers.Y:X2}";
         },
      } },

      new() { Name = "STY", OpCode = 0x8C, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X4} = {m[target]:X2}";
            m[target] = m.Cpu.Registers.Y;
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "BIT", OpCode = 0x2C, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X4} = {m[target]:X2}";
            OpBit(m, m[target]);
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "ORA", OpCode = 0x0D, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X4} = {m[target]:X2}";
            m.Cpu.Registers.A |= m[target];
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "AND", OpCode = 0x2D, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X4} = {m[target]:X2}";
            m.Cpu.Registers.A &= m[target];
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "EOR", OpCode = 0x4D, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X4} = {m[target]:X2}";
            m.Cpu.Registers.A ^= m[target];
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "ADC", OpCode = 0x6D, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X4} = {m[target]:X2}";
            OpAddCarry(m, m[target]);
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "CMP", OpCode = 0xCD, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X4} = {m[target]:X2}";
            OpCompare(m, m.Cpu.Registers.A, m[target]);
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "SBC", OpCode = 0xED, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X4} = {m[target]:X2}";
            OpAddCarry(m, (byte)~m[target]);
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "CPX", OpCode = 0xEC, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X4} = {m[target]:X2}";
            OpCompare(m, m.Cpu.Registers.X, m[target]);
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "CPY", OpCode = 0xCC, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var target = m.ReadUShort(m.Cpu.Registers.PC + 1);
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X4} = {m[target]:X2}";
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
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X4} = {m[target]:X2}";
            var carry = (m[target] & 1) > 0;
            m[target] >>= 1;
            m.Cpu.Registers.SetFlag(StatusFlag.Carry, carry);
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
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X4} = {m[target]:X2}";
            var c_flag = m[target].HasBit(7);
            m[target] <<= 1;
            m.Cpu.Registers.SetFlag(StatusFlag.Carry, c_flag);
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
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X4} = {m[target]:X2}";
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
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X4} = {m[target]:X2}";
            OpRollingLeftShiftMem(m, target);
            m.Cpu.Registers.PC += 3;
         },
      } },

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
         if (m.Settings.System.DebugMode) m.Cpu._log_message = $"${target:X4} = {m[target]:X2}";
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

      new() { Name = "JMP", OpCode = 0x6C, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => { },
         m => { },
         m => {
            var address = m.ReadUShort(m.Cpu.Registers.PC + 1);
            var target = m.ReadUShortSamePage(address);
            m.Cpu.Registers.PC = target;
            if (m.Settings.System.DebugMode) m.Cpu._log_message = $"(${address:X4}) = {target:X4}";
         },
      } },

      new() { Name = "LDA", OpCode = 0xB9, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            m.Cpu.Registers.A = m[GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.Y)];
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "ORA", OpCode = 0x19, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            m.Cpu.Registers.A |= m[GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.Y)];
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "AND", OpCode = 0x39, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            m.Cpu.Registers.A &= m[GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.Y)];
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "EOR", OpCode = 0x59, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            m.Cpu.Registers.A ^= m[GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.Y)];
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "ADC", OpCode = 0x79, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            OpAddCarry(m, m[GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.Y)]);
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "CMP", OpCode = 0xD9, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            OpCompare(m, m.Cpu.Registers.A, m[GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.Y)]);
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "SBC", OpCode = 0xF9, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            OpAddCarry(m, (byte)~m[GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.Y)]);
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "STA", OpCode = 0x99, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            m[GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.Y)] = m.Cpu.Registers.A;
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "LDY", OpCode = 0xB4, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var address = GetIndexedZeroPageAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.X);
            m.Cpu.Registers.Y = m[address];
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "STY", OpCode = 0x94, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var address = GetIndexedZeroPageAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.X);
            m[address] = m.Cpu.Registers.Y;
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "ORA", OpCode = 0x15, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var address = GetIndexedZeroPageAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.X);
            m.Cpu.Registers.A |= m[address];
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "AND", OpCode = 0x35, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var address = GetIndexedZeroPageAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.X);
            m.Cpu.Registers.A &= m[address];
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "EOR", OpCode = 0x55, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var address = GetIndexedZeroPageAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.X);
            m.Cpu.Registers.A ^= m[address];
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "ADC", OpCode = 0x75, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var address = GetIndexedZeroPageAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.X);
            OpAddCarry(m, m[address]);
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "CMP", OpCode = 0xD5, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var address = GetIndexedZeroPageAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.X);
            OpCompare(m, m.Cpu.Registers.A, m[address]);
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "SBC", OpCode = 0xF5, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var address = GetIndexedZeroPageAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.X);
            OpAddCarry(m, (byte)~m[address]);
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "LDA", OpCode = 0xB5, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var address = GetIndexedZeroPageAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.X);
            m.Cpu.Registers.A = m[address];
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "STA", OpCode = 0x95, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var address = GetIndexedZeroPageAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.X);
            m[address] = m.Cpu.Registers.A;
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "LSR", OpCode = 0x56, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => { },
         m => { },
         m => {
            var address = GetIndexedZeroPageAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.X);
            var carry = (m[address] & 1) > 0;
            m[address] >>= 1;
            m.Cpu.Registers.SetFlag(StatusFlag.Carry, carry);
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "ASL", OpCode = 0x16, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => { },
         m => { },
         m => {
            var address = GetIndexedZeroPageAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.X);
            var carry = m[address].HasBit(7);
            m[address] <<= 1;
            m.Cpu.Registers.SetFlag(StatusFlag.Carry, carry);
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "ROR", OpCode = 0x76, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => { },
         m => { },
         m => {
            var address = GetIndexedZeroPageAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.X);
            OpRollingRightShiftMem(m, address);
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "ROL", OpCode = 0x36, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => { },
         m => { },
         m => {
            var address = GetIndexedZeroPageAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.X);
            OpRollingLeftShiftMem(m, address);
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "INC", OpCode = 0xF6, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => { },
         m => { },
         m => {
            var address = GetIndexedZeroPageAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.X);
            OpIncMem(m, address);
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "DEC", OpCode = 0xD6, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => { },
         m => { },
         m => {
            var address = GetIndexedZeroPageAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.X);
            OpDecMem(m, address);
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "LDX", OpCode = 0xB6, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var address = GetIndexedZeroPageAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.Y);
            m.Cpu.Registers.X = m[address];
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "STX", OpCode = 0x96, Bytes = 2, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var address = GetIndexedZeroPageAddress(m, m[(ushort)(m.Cpu.Registers.PC + 1)], RegisterType.Y);
            m[address] = m.Cpu.Registers.X;
            m.Cpu.Registers.PC += 2;
         },
      } },

      new() { Name = "LDY", OpCode = 0xBC, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            m.Cpu.Registers.Y = m[GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.X)];
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "ORA", OpCode = 0x1D, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            m.Cpu.Registers.A |= m[GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.X)];
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "AND", OpCode = 0x3D, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            m.Cpu.Registers.A &= m[GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.X)];
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "EOR", OpCode = 0x5D, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            m.Cpu.Registers.A ^= m[GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.X)];
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "ADC", OpCode = 0x7D, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            OpAddCarry(m, m[GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.X)]);
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "CMP", OpCode = 0xDD, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            OpCompare(m, m.Cpu.Registers.A, m[GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.X)]);
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "SBC", OpCode = 0xFD, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            OpAddCarry(m, (byte)~m[GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.X)]);
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "LDA", OpCode = 0xBD, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            m.Cpu.Registers.A = m[GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.X)];
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "STA", OpCode = 0x9D, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var address = GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.X);
            m[address] = m.Cpu.Registers.A;
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "LSR", OpCode = 0x5E, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => { },
         m => { },
         m => {
            if (m.Cpu._log_inst_count == 4784)
            {

            }

            var address = GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.X);
            var carry = (m[address] & 1) > 0;
            m[address] >>= 1;
            m.Cpu.Registers.SetFlag(StatusFlag.Carry, carry);
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "ASL", OpCode = 0x1E, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => { },
         m => { },
         m => {
            var address = GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.X);
            var carry = m[address].HasBit(7);
            m[address] <<= 1;
            m.Cpu.Registers.SetFlag(StatusFlag.Carry, carry);
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "ROR", OpCode = 0x7E, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => { },
         m => { },
         m => { },
         m => {
            var address = GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.X);
            OpRollingRightShiftMem(m, address);
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "ROL", OpCode = 0x3E, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => { },
         m => { },
         m => { },
         m => {
            var address = GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.X);
            OpRollingLeftShiftMem(m, address);
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "INC", OpCode = 0xFE, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => { },
         m => { },
         m => { },
         m => {
            var address = GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.X);
            OpIncMem(m, address);
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "DEC", OpCode = 0xDE, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => { },
         m => { },
         m => { },
         m => {
            var address = GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.X);
            OpDecMem(m, address);
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "LDX", OpCode = 0xBE, Bytes = 3, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => {
            var address = GetIndexedAbsoluteAddress(m, m.ReadUShort(m.Cpu.Registers.PC + 1), RegisterType.Y);
            m.Cpu.Registers.X = m[address];
            m.Cpu.Registers.PC += 3;
         },
      } },

      new() { Name = "BRK", OpCode = 0x00, Bytes = 1, Process = new ProcessDelegate[] {
         m => { },
         m => { },
         m => { },
         m => { },
         m => { },
         m => {
            PUSH_ushort(m, m.Cpu.Registers.PC);
            PUSH(m, m.Cpu.Registers.P);
            m.Cpu.Registers.SetFlag(StatusFlag.InterruptDisable);
            m.Cpu.Registers.PC += 1;
         },
      } },
   };

   public Cpu(MachineState machine) {
      _machine = machine;
      foreach (var i in instructions_unordered) {
         if (_instructions[i.OpCode] != null) throw new Exception($"Duplicate OpCodes: {_instructions[i.OpCode].Name} and {i.Name}");
         _instructions[i.OpCode] = i;
      }
   }

   public void SetPowerUpState() {
      Registers.PC = 0x34;
      Registers.X = 0;
      Registers.Y = 0;
      Registers.A = 0;

      // I just put InterruptDisable/0xFD here because these seem to be set in the nestest.log file by the time it gets here but I haven't found anything in documentation to say why
      Registers.P = StatusFlag._1 | StatusFlag.InterruptDisable;
      Registers.S = 0xFD;
   }

   public void Tick() {
      _cycle_counter++;
      if (_currentInstruction == null) {
         if (SuspendCycles > 0) {
            SuspendCycles--;
            return;
         }
         var opcode = _machine[Registers.PC];
         _currentInstruction = _instructions[opcode];
         if (_currentInstruction == null) {
            throw new NotImplementedException($"{Registers.PC:X4}: Opcode {opcode:X2} not implemented. {instructions_unordered.Length}/151 opcodes are implemented!");
         }
         if (_machine.Settings.System.DebugMode) {
            _log_pc = Registers.PC;
            _log_d1 = _currentInstruction.Bytes < 2 ? null : _machine[(ushort)(Registers.PC + 1)];
            _log_d2 = _currentInstruction.Bytes < 3 ? null : _machine[(ushort)(Registers.PC + 2)];
            _log_cyc = _cycle_counter;
            _log_cpu = Registers.GetLog();
            _log_inst_count++;
         }
      } else {
         if (_current_instruction_cycle < _currentInstruction.Process.Length) _currentInstruction.Process[_current_instruction_cycle++](_machine);
         if (_current_instruction_cycle == _currentInstruction.Process.Length) {
            if (_add_cycles > 0) {
               _add_cycles--;
               return;
            }
            if (_machine.Settings.System.DebugMode) {
               _machine.Logger.Log(new(_currentInstruction, _log_pc, _log_d1, _log_d2, _log_cpu, _log_cyc, _log_message));
               _log_message = null;
            }
            _currentInstruction = null;
            _current_instruction_cycle = 0;
            if (_machine.Ppu.NMI_output) {
               // handle interrupt
               PUSH(_machine, _machine.Cpu.Registers.P);
               PUSH_ushort(_machine, _machine.Cpu.Registers.PC);
               _machine.Cpu.Registers.PC = _machine.ReadUShort(0xFFFA);
               _machine.Ppu.NMI_output = false;
               _machine.Ppu.NMI_occurred = true;
            }
         }
      }
   }
}
