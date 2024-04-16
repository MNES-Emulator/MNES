namespace Mnes.Core.Utility;

/// <summary> Number with only 2 bits. </summary>
public readonly struct Byte2 {
   readonly byte _value = 0;

   public const byte MinValue = 0;
   public const byte MaxValue = 0b_0000_0011;

   public Byte2(int value) =>
      _value = (byte)(value & MaxValue);

   public static implicit operator int(Byte2 d) => d._value;
   public static explicit operator Byte2(int b) => new(b);
   public static Byte2 operator ++(Byte2 v1) => new(v1._value + 1);
   public static Byte2 operator --(Byte2 v1) => new(v1._value - 1);
   public static Byte2 operator +(Byte2 v1, Byte2 v2) => new(v1._value + v2._value);
   public static Byte2 operator -(Byte2 v1, Byte2 v2) => new(v1._value - v2._value);
   public static Byte2 operator *(Byte2 v1, Byte2 v2) => new(v1._value * v2._value);
   public static Byte2 operator /(Byte2 v1, Byte2 v2) => new(v1._value / v2._value);

   public override string ToString() =>
      _value.ToString();
}
