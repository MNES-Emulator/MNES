namespace Mnes.Core.Machine.Logging;

public readonly struct CpuRegisterLog {
   /// <summary> The accumulator. </summary>
   public readonly byte A;

   /// <summary> The X register. </summary>
   public readonly byte X;

   /// <summary> The Y register. </summary>
   public readonly byte Y;

   /// <summary> The program counter. </summary>
   public readonly ushort PC;

   /// <summary> The stack pointer. </summary>
   public readonly byte S;

   /// <summary> The status register. </summary>
   public readonly byte P;

   public override string ToString() =>
      $"A:{A:X2} X:{X:X2} Y:{Y:X2} P:{P:X2} S:{S:X2}";

   public CpuRegisterLog(CpuRegisters r) {
      A = r.A;
      X = r.X;
      Y = r.Y;
      PC = r.PC;
      S = r.S;
      P = r.P;
   }
}
