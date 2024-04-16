namespace Mnes.Core.Utility;

public readonly struct Byte5
{
   readonly byte _value = 0;

   public const byte MinValue = 0;
   public const byte MaxValue = 0b_0001_1111;

   public Byte5(int value) =>
      _value = (byte)(value & MaxValue);

   public static implicit operator int(Byte5 d) => d._value;
   public static explicit operator Byte5(int b) => new(b);
   public static Byte5 operator ++(Byte5 v1) => new(v1._value + 1);
   public static Byte5 operator --(Byte5 v1) => new(v1._value - 1);
   public static Byte5 operator +(Byte5 v1, Byte5 v2) => new(v1._value + v2._value);
   public static Byte5 operator -(Byte5 v1, Byte5 v2) => new(v1._value - v2._value);
   public static Byte5 operator *(Byte5 v1, Byte5 v2) => new(v1._value * v2._value);
   public static Byte5 operator /(Byte5 v1, Byte5 v2) => new(v1._value / v2._value);

   public override string ToString() =>
      _value.ToString();
}
