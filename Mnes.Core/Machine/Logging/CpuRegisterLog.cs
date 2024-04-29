using Mnes.Core.Machine.CPU;
using static Mnes.Core.Machine.CPU.CpuRegisters;

namespace Mnes.Core.Machine.Logging;

public readonly struct CpuRegisterLog {
   /// <summary> The accumulator. </summary>
   public byte A { get; init; }

   /// <summary> The X register. </summary>
   public byte X { get; init; }

   /// <summary> The Y register. </summary>
   public byte Y { get; init; }

   /// <summary> The program counter. </summary>
   public ushort PC { get; init; }

   /// <summary> The stack pointer. </summary>
   public byte S { get; init; }

   /// <summary> The status register. </summary>
   public byte P { get; init; }

   public override string ToString() =>
      GetDebugString(false);

   public string GetDebugString(bool show_status_flags) =>
      $"A:{A:X2} X:{X:X2} Y:{Y:X2} P:{P:X2} {(show_status_flags ? $"({GetStatusString()}) " : "")}SP:{S:X2}";

   string GetStatusString() {
      var p = P;
      var acronyms = string.Join("", StatusFlag.Values.Select(x => $"{x.Acronym}").Reverse());
      return $"{acronyms} {string.Join("", StatusFlag.Values.Select(x => x.IsSet(p) ? "1" : "0").Reverse())}";
   }

   public CpuRegisterLog(
      CpuRegisters r
   ) {
      A = r.A;
      X = r.X;
      Y = r.Y;
      PC = r.PC;
      S = r.S;
      P = r.P;
   }
}
