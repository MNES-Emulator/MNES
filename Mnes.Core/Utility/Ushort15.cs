namespace Mnes.Core.Utility;

/// <summary> Number with only 15 bits. </summary>
public readonly struct Ushort15 {
   readonly ushort _value = 0;

   public Ushort15(int value) =>
      _value = (ushort)(value & 0b_0111_1111_1111_1111);

   public static implicit operator ushort(Ushort15 d) => d._value;
   public static explicit operator Ushort15(ushort b) => new(b);

   public static Ushort15 operator +(Ushort15 v1, Ushort15 v2) => new(v1._value + v2._value);
   public static Ushort15 operator -(Ushort15 v1, Ushort15 v2) => new(v1._value - v2._value);
   public static Ushort15 operator *(Ushort15 v1, Ushort15 v2) => new(v1._value * v2._value);
   public static Ushort15 operator /(Ushort15 v1, Ushort15 v2) => new(v1._value / v2._value);
}
