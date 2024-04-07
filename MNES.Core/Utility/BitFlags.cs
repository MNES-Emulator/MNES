namespace Mnes.Core.Utility;

public static class BitFlags {
   static readonly byte[] _flags = { F0, F1, F2, F3, F4, F5, F6, F7 };
   static readonly byte[] _masks = { M0, M1, M2, M3, M4, M5, M6, M7 };

   public const byte F0 = 1 << 0;
   public const byte F1 = 1 << 1;
   public const byte F2 = 1 << 2;
   public const byte F3 = 1 << 3;
   public const byte F4 = 1 << 4;
   public const byte F5 = 1 << 5;
   public const byte F6 = 1 << 6;
   public const byte F7 = 1 << 7;

   public const byte M0 = 0b1111_1110;
   public const byte M1 = 0b1111_1101;
   public const byte M2 = 0b1111_1011;
   public const byte M3 = 0b1111_0111;
   public const byte M4 = 0b1110_1111;
   public const byte M5 = 0b1101_1111;
   public const byte M6 = 0b1011_1111;
   public const byte M7 = 0b0111_1111;

   public static bool HasFlag(byte b, int index) => (b & _flags[index]) > 0;
   public static void SetFlag(ref byte b, int index) => b |= _flags[index];
   public static void SetFlag(ref byte b, int index, bool value) {
      if (value) SetFlag(ref b, index);
      else RemoveFlag(ref b, index);
   }

   public static void RemoveFlag(ref byte b, int index) => b &= _masks[index];
}
