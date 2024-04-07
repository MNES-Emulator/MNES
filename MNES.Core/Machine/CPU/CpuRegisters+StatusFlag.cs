using System.Collections.Immutable;

namespace Mnes.Core.Machine.CPU;

partial class CpuRegisters {
   public sealed class StatusFlag {
      static readonly List<StatusFlag> sValues = new();

      /// <summary> The C flag. </summary>
      public static readonly StatusFlag Carry = new('C', 0b_0000_0001);
      /// <summary> The Z flag. </summary>
      public static readonly StatusFlag Zero = new('Z', 0b_0000_0010);
      /// <summary> The I flag. </summary>
      public static readonly StatusFlag InterruptDisable = new('I', 0b_0000_0100);
      /// <summary> The D flag. </summary>
      public static readonly StatusFlag Decimal = new('D', 0b_0000_1000);
      /// <summary> The B flag. </summary>
      public static readonly StatusFlag BFlag = new('B', 0b_0001_0000);
      /// <summary> The 1 flag. </summary>
      public static readonly StatusFlag _1 = new('1', 0b_0010_0000);
      /// <summary> The V flag. </summary>
      public static readonly StatusFlag Overflow = new('V', 0b_0100_0000);
      /// <summary> The N flag. </summary>
      public static readonly StatusFlag Negative = new('N', 0b_1000_0000);

      public static IReadOnlyList<StatusFlag> Values { get; } = sValues.ToImmutableList();

      public char Acronym { get; }
      public byte Bits { get; }

      StatusFlag(
         char acronym,
         byte bits
      ) {
         Acronym = acronym;
         Bits = bits;

         sValues.Add(this);
      }

      public static byte operator |(byte bits, StatusFlag flag) => (byte)(bits | flag.Bits);
      public static byte operator |(StatusFlag flag, byte bits) => bits | flag;
      public static byte operator |(StatusFlag a, StatusFlag b) => a.Bits | b;

      public static byte operator &(byte bits, StatusFlag flag) => (byte)(bits & flag.Bits);
      public static byte operator &(StatusFlag flag, byte bits) => bits & flag;

      public static byte operator ~(StatusFlag me) => (byte)~me.Bits;

      public bool IsSet(byte bits) =>
         (bits & Bits) != 0;
   }
}
