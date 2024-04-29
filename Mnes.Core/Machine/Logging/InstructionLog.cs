using Mnes.Core.Machine.CPU;

namespace Mnes.Core.Machine.Logging;

public readonly struct InstructionLog {
   public readonly CpuInstruction Instruction;
   public readonly ushort Address;
   public readonly byte? Param1;
   public readonly byte? Param2;
   public readonly CpuRegisterLog CpuRegisters;
   public readonly long ClockCycle;
   public readonly string Message;
   public readonly int PpuCycle;
   public readonly int PpuScanline;

   public InstructionLog(
      CpuInstruction instruction,
      ushort address,
      byte? param1,
      byte? param2,
      CpuRegisterLog log,
      long clock_cycle,
      string message,
      int ppu_cycle,
      int ppu_scanline
   ) {
      Instruction = instruction;
      Address = address;
      Param1 = param1;
      Param2 = param2;
      CpuRegisters = log;
      ClockCycle = clock_cycle;
      Message = message;
      PpuCycle = ppu_cycle;
      PpuScanline = ppu_scanline;
   }

   public override string ToString() =>
      GetDebugString(false);

   public string GetDebugString(bool show_status_flags) =>
      $"{Address:X4}  " +
      $"{Instruction.OpCode:X2} {Param1?.ToString("X2") ?? ""} {Param2?.ToString("X2") ?? ""}".PadRight(10) +
      $"{Instruction.Name} {Message ?? ""}".PadRight(32) +
      $"{CpuRegisters.GetDebugString(show_status_flags)} PPU: {PpuScanline,3},{PpuCycle,3} CYC:{ClockCycle}";

   /// <summary>
   /// Converts a debug string back into a <see cref="InstructionLog"/>. The instruction log string can't have status flags in it, otherwise this will throw an exception.
   /// </summary>
   public static InstructionLog FromString(string s)
   {
      // forgive my sins
      string? null_if_empty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

      var address = Convert.ToUInt16(s[0..4], 16);
      var opcode = Convert.ToByte(s[6..8], 16);
      var param1 = Convert.ToByte(null_if_empty(s[9..11]) ?? "0", 16);
      var param2 = Convert.ToByte(null_if_empty(s[12..14]) ?? "0", 16);
      var name = s[16..19];
      var message = s[20..47].Trim();
      var a = Convert.ToByte(s[50..52], 16);
      var x = Convert.ToByte(s[55..57], 16);
      var y = Convert.ToByte(s[60..62], 16);
      var p = Convert.ToByte(s[65..67], 16);
      var sp = Convert.ToByte(s[71..73], 16);
      var ppu_scanline = Convert.ToByte(s[78..81].Trim(), 10);
      var ppu_cycle = Convert.ToByte(s[82..85].Trim(), 10);
      var clock_cycle = Convert.ToInt64(s[90..], 10);

      throw new NotImplementedException();
   }
}
