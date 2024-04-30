using Mnes.Core.Machine.Logging;
using static Mnes.Core.Machine.CPU.CpuRegisters;

namespace Mnes.Core.Machine.CPU;

// https://www.masswerk.at/6502/6502_instruction_set.html
public sealed class Cpu {
   readonly MachineState _machine;

   CpuInstruction? _current_instruction;
   int _current_instruction_cycle;
   long _cycle_counter;

   // temp values used to store data across clock cycles within a single instruction
   internal ushort _tmp_u;

   // Add additional cycles after an instruction
   internal int _add_cycles;

   // Skip cycles (used by OAM DMA when copying a page over to VRAM)
   public int SuspendCycles;

   // values that are recorded during logging
   internal InstructionLog _log;
   long _log_inst_count;

   public CpuRegisters Registers { get; } = new();

   public Cpu(MachineState machine) {
      _machine = machine;
   }

   public void SetPowerUpState() {
      Registers.PC = 0x34;
      Registers.X = 0;
      Registers.Y = 0;
      Registers.A = 0;

      _cycle_counter = 6;
      Registers.P = StatusFlag._1 | StatusFlag.InterruptDisable;
      Registers.S = 0xFD;
   }

   public void Tick() {
      _cycle_counter++;
      if (_current_instruction == null) {
         if (SuspendCycles > 0) {
            SuspendCycles--;
            return;
         }
         _current_instruction = CpuInstruction.FromOpcode(_machine[Registers.PC]) ?? throw new NotImplementedException($"{Registers.PC:X4}: Opcode {_machine[Registers.PC]:X2} not implemented. {CpuInstruction.ImplementedCount}/151 opcodes are implemented!");
         if (_machine.Settings.System.DebugMode) RecordLogValues();
      } else {
         if (_current_instruction_cycle < _current_instruction.Process.Length) _current_instruction.Process[_current_instruction_cycle++](_machine);
         if (_current_instruction_cycle == _current_instruction.Process.Length) {
            if (_add_cycles > 0) {
               _add_cycles--;
               return;
            }
            if (_machine.Settings.System.DebugMode) {
               _machine.Logger.Log(_log, _machine.Settings.System.DebugShowStatusFlags);
               _log.Message = null;
            }
            _machine.Callbacks.OnNesInstructionExecute?.Invoke(_current_instruction);
            _machine.Callbacks.OnCpuExecute?.Invoke();
            _current_instruction = null;
            _current_instruction_cycle = 0;
            if (_machine.Ppu.NMI_output) {
               // handle interrupt
               CpuInstruction.PUSH(_machine, _machine.Cpu.Registers.P);
               CpuInstruction.PUSH_ushort(_machine, _machine.Cpu.Registers.PC);
               _machine.Cpu.Registers.PC = _machine.ReadUShort(0xFFFA);
               _machine.Ppu.NMI_output = false;
               _machine.Ppu.NMI_occurred = true;
            }
         }
      }

      void RecordLogValues() {
         _log.Instruction = _current_instruction;
         _log.Address = Registers.PC;
         _log.Param1 = _current_instruction.Bytes < 2 ? null : _machine[(ushort)(Registers.PC + 1)];
         _log.Param2 = _current_instruction.Bytes < 3 ? null : _machine[(ushort)(Registers.PC + 2)];
         _log.ClockCycle = _cycle_counter;
         _log.CpuRegisters = Registers.GetLog();
         _log.PpuCycle = _machine.Ppu.Cycle;
         _log.PpuScanline = _machine.Ppu.Scanline;
         _log_inst_count++;
      }
   }
}
