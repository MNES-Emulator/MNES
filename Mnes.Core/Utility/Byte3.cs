namespace Mnes.Core.Utility;

/// <summary> Number with only 3 bits. </summary>
public readonly struct Byte3 {
   readonly byte _value = 0;

   public const byte MinValue = 0;
   public const byte MaxValue = 0b_0111;

   public Byte3(int value) =>
      _value = (byte)(value & MaxValue);

   public static implicit operator int(Byte3 d) => d._value;
   public static explicit operator Byte3(int b) => new(b);
   public static Byte3 operator ++(Byte3 v1) => new(v1._value + 1);
   public static Byte3 operator --(Byte3 v1) => new(v1._value - 1);
   public static Byte3 operator +(Byte3 v1, Byte3 v2) => new(v1._value + v2._value);
   public static Byte3 operator -(Byte3 v1, Byte3 v2) => new(v1._value - v2._value);
   public static Byte3 operator *(Byte3 v1, Byte3 v2) => new(v1._value * v2._value);
   public static Byte3 operator /(Byte3 v1, Byte3 v2) => new(v1._value / v2._value);

   public override string ToString() =>
      _value.ToString();
}
