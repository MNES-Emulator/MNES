namespace Mnes.Core.Utility;

/// <summary> Number with only 3 bits. </summary>
public readonly struct Byte3 {
   readonly ushort _value = 0;

   // TODO: this seems to not be implemented yet
   public Byte3(ushort value) =>
      _value = (ushort)(value & 0b_0111_1111_1111_1111);

   public static implicit operator ushort(Byte3 d) => d._value;
   public static explicit operator Byte3(ushort b) => new(b);
}
