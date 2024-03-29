namespace Mnes.Core.Machine.CPU;

partial class CpuRegisters {
   public enum StatusFlagType {
      /// <summary> The C flag. </summary>
      Carry = 0b_0000_0001,
      /// <summary> The Z flag. </summary>
      Zero = 0b_0000_0010,
      /// <summary> The I flag. </summary>
      InterruptDisable = 0b_0000_0100,
      /// <summary> The D flag. </summary>
      Decimal = 0b_0000_1000,
      /// <summary> The B flag. </summary>
      BFlag = 0b_0001_0000,
      /// <summary> The 1 flag. </summary>
      _1 = 0b_0010_0000,
      /// <summary> The V flag. </summary>
      Overflow = 0b_0100_0000,
      /// <summary> The N flag. </summary>
      Negative = 0b_1000_0000,
   }
}
