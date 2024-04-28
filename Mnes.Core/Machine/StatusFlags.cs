using Mnes.Core.Utility;

namespace Mnes.Core.Machine;

public static class StatusFlags {
   public const byte CARRY = BitFlags.F0;
   public const byte ZERO = BitFlags.F1;
   public const byte INTERRUPT_DISABLE = BitFlags.F2;
   public const byte DECIMAL = BitFlags.F3;

   public const byte B = BitFlags.F4;
   public const byte UNUSED = BitFlags.F5;
   public const byte OVERFLOW = BitFlags.F6;
   public const byte NEGATIVE = BitFlags.F7;
}
