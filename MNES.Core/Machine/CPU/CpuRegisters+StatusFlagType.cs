namespace Mnes.Core.Machine.CPU;

partial class CpuRegisters {
   public readonly struct StatusFlagType {
      /// <summary> The C flag. </summary>
      public static readonly StatusFlagType Carry = new(0b_0000_0001);
      /// <summary> The Z flag. </summary>
      public static readonly StatusFlagType Zero = new(0b_0000_0010);
      /// <summary> The I flag. </summary>
      public static readonly StatusFlagType InterruptDisable = new(0b_0000_0100);
      /// <summary> The D flag. </summary>
      public static readonly StatusFlagType Decimal = new(0b_0000_1000);
      /// <summary> The B flag. </summary>
      public static readonly StatusFlagType BFlag = new(0b_0001_0000);
      /// <summary> The 1 flag. </summary>
      public static readonly StatusFlagType _1 = new(0b_0010_0000);
      /// <summary> The V flag. </summary>
      public static readonly StatusFlagType Overflow = new(0b_0100_0000);
      /// <summary> The N flag. </summary>
      public static readonly StatusFlagType Negative = new(0b_1000_0000);

      public byte Bits { get; }

      StatusFlagType(
         byte bits
      ) =>
         Bits = bits;

      public static byte operator |(byte bits, StatusFlagType flag) => (byte)(bits | flag.Bits);
      public static byte operator |(StatusFlagType flag, byte bits) => bits | flag;
      public static byte operator |(StatusFlagType a, StatusFlagType b) => a | b.Bits;

      public static byte operator &(byte bits, StatusFlagType flag) => (byte)(bits & flag.Bits);
      public static byte operator &(StatusFlagType flag, byte bits) => bits & flag;

      public static byte operator ~(StatusFlagType me) => (byte)~me.Bits;

      public bool IsSet(
         byte bits
      ) =>
         (bits & Bits) != 0;
   }
}
