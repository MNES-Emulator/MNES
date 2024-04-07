using System.Collections.Immutable;
using Mnes.Core.Utility;

namespace Mnes.Core.Machine.CPU;

partial class CpuRegisters {
   public sealed class StatusFlag {
      static readonly List<StatusFlag> sValues = new();

      /// <summary> The C flag. </summary>
      public static readonly StatusFlag Carry = new('C', BitFlags._1);
      /// <summary> The Z flag. </summary>
      public static readonly StatusFlag Zero = new('Z', BitFlags._2);
      /// <summary> The I flag. </summary>
      public static readonly StatusFlag InterruptDisable = new('I', BitFlags._3);
      /// <summary> The D flag. </summary>
      public static readonly StatusFlag Decimal = new('D', BitFlags._4);
      /// <summary> The B flag. </summary>
      public static readonly StatusFlag BFlag = new('B', BitFlags._5);
      /// <summary> The 1 flag. </summary>
      public static readonly StatusFlag _1 = new('1', BitFlags._6);
      /// <summary> The V flag. </summary>
      public static readonly StatusFlag Overflow = new('V', BitFlags._7);
      /// <summary> The N flag. </summary>
      public static readonly StatusFlag Negative = new('N', BitFlags._8);

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
