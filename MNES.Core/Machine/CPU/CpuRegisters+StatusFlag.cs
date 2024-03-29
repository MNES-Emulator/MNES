using System.Collections.Immutable;

namespace Mnes.Core.Machine.CPU;

partial class CpuRegisters {
   public readonly record struct StatusFlag {
      static readonly List<StatusFlag> sValues = new();

      /// <summary> The C flag. </summary>
      public static readonly StatusFlag Carry = new(0b_0000_0001);
      /// <summary> The Z flag. </summary>
      public static readonly StatusFlag Zero = new(0b_0000_0010);
      /// <summary> The I flag. </summary>
      public static readonly StatusFlag InterruptDisable = new(0b_0000_0100);
      /// <summary> The D flag. </summary>
      public static readonly StatusFlag Decimal = new(0b_0000_1000);
      /// <summary> The B flag. </summary>
      public static readonly StatusFlag BFlag = new(0b_0001_0000);
      /// <summary> The 1 flag. </summary>
      public static readonly StatusFlag _1 = new(0b_0010_0000);
      /// <summary> The V flag. </summary>
      public static readonly StatusFlag Overflow = new(0b_0100_0000);
      /// <summary> The N flag. </summary>
      public static readonly StatusFlag Negative = new(0b_1000_0000);

      public static IReadOnlyList<StatusFlag> Values { get; } = sValues.ToImmutableList();

      public byte Bits { get; }

      public StatusFlag()
      : this(Carry.Bits) {
      }

      StatusFlag(
         byte bits
      ) {
         Bits = bits;

         sValues.Add(this);
      }

      public static byte operator |(byte bits, StatusFlag flag) => (byte)(bits | flag.Bits);
      public static byte operator |(StatusFlag flag, byte bits) => bits | flag;
      public static byte operator |(StatusFlag a, StatusFlag b) => a | b.Bits;

      public static byte operator &(byte bits, StatusFlag flag) => (byte)(bits & flag.Bits);
      public static byte operator &(StatusFlag flag, byte bits) => bits & flag;

      public static byte operator ~(StatusFlag me) => (byte)~me.Bits;

      public bool IsSet(
         byte bits
      ) =>
         (bits & Bits) != 0;
   }
}
