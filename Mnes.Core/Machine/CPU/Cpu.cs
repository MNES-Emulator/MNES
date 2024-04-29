using Mnes.Core.Machine.Logging;
using static Mnes.Core.Machine.CPU.CpuRegisters;

namespace Mnes.Core.Machine.CPU;

// https://www.masswerk.at/6502/6502_instruction_set.html
public sealed class Cpu {
   readonly MachineState _machine;

   CpuInstruction? _currentInstruction;
   int _current_instruction_cycle;
   long _cycle_counter = 6;

   // temp values used to store data across clock cycles within a single instruction
   internal ushort _tmp_u;

   // Add additional cycles after an instruction
   internal int _add_cycles;

   // Skip cycles (used by OAM DMA when copying a page over to VRAM)
   public int SuspendCycles;

   // values that are recorded during logging
   internal ushort _log_pc;
   internal byte? _log_d1;
   internal byte? _log_d2;
   internal long _log_cyc;
   internal string? _log_message;
   internal CpuRegisterLog _log_cpu;
   internal long _log_inst_count;
   internal int _log_ppu_cycle;
   internal int _log_ppu_scanline;

   public CpuRegisters Registers { get; } = new();

   public Cpu(MachineState machine) {
      _machine = machine;
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
         _currentInstruction = CpuInstruction.FromOpcode(opcode);
         if (_currentInstruction == null) {
            throw new NotImplementedException($"{Registers.PC:X4}: Opcode {opcode:X2} not implemented. {CpuInstruction.ImplementedCount}/151 opcodes are implemented!");
         }
         if (_machine.Settings.System.DebugMode) {
            _log_pc = Registers.PC;
            _log_d1 = _currentInstruction.Bytes < 2 ? null : _machine[(ushort)(Registers.PC + 1)];
            _log_d2 = _currentInstruction.Bytes < 3 ? null : _machine[(ushort)(Registers.PC + 2)];
            _log_cyc = _cycle_counter;
            _log_cpu = Registers.GetLog();
            _log_inst_count++;
            _log_ppu_cycle = _machine.Ppu.Cycle;
            _log_ppu_scanline = _machine.Ppu.Scanline;
         }
      } else {
         if (_current_instruction_cycle < _currentInstruction.Process.Length) _currentInstruction.Process[_current_instruction_cycle++](_machine);
         if (_current_instruction_cycle == _currentInstruction.Process.Length) {
            if (_add_cycles > 0) {
               _add_cycles--;
               return;
            }
            if (_machine.Settings.System.DebugMode) {
               if (_log_message is { } message) {
                  _machine.Logger.Log(
                     new(
                        _currentInstruction, 
                        _log_pc, 
                        _log_d1, 
                        _log_d2, 
                        _log_cpu, 
                        _log_cyc,
                        message, 
                        _log_ppu_cycle, 
                        _log_ppu_scanline), 
                     _machine.Settings.System.DebugShowStatusFlags);
                  _log_message = null;
               }
            }
            _machine.Callbacks.OnNesInstructionExecute?.Invoke(_currentInstruction);
            _machine.Callbacks.OnCpuExecute?.Invoke();
            _currentInstruction = null;
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
   }
}
