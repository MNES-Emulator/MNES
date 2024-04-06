using System.Collections.Immutable;

namespace Mnes.Core.Machine.CPU;

partial class CpuRegisters {
   public readonly record struct StatusFlag {
      static readonly List<StatusFlag> sValues = new();

      /// <summary> The C flag. </summary>
      public static readonly StatusFlag Carry = new(0, 0b_0000_0001);
      /// <summary> The Z flag. </summary>
      public static readonly StatusFlag Zero = new(1, 0b_0000_0010);
      /// <summary> The I flag. </summary>
      public static readonly StatusFlag InterruptDisable = new(2, 0b_0000_0100);
      /// <summary> The D flag. </summary>
      public static readonly StatusFlag Decimal = new(3, 0b_0000_1000);
      /// <summary> The B flag. </summary>
      public static readonly StatusFlag BFlag = new(4, 0b_0001_0000);
      /// <summary> The 1 flag. </summary>
      public static readonly StatusFlag _1 = new(5, 0b_0010_0000);
      /// <summary> The V flag. </summary>
      public static readonly StatusFlag Overflow = new(6, 0b_0100_0000);
      /// <summary> The N flag. </summary>
      public static readonly StatusFlag Negative = new(7, 0b_1000_0000);

      public static IReadOnlyList<StatusFlag> Values { get; } = sValues.ToImmutableList();

      readonly int _index;

      public byte Bits { get; }

      public char Acronym => _index switch {
         0 => 'C',
         1 => 'Z',
         2 => 'I',
         3 => 'D',
         4 => 'B',
         5 => '1',
         6 => 'V',
         7 => 'N',
         _ => throw new Exception()
      };

      public StatusFlag()
      : this(Carry._index, Carry.Bits) {
      }

      StatusFlag(
         int index,
         byte bits
      ) {
         _index = index;
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
