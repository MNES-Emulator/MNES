using Mnes.Core.Utility;

namespace Mnes.Core.Machine.CPU;

// https://www.nesdev.org/wiki/PPU_OAM
public struct OamSprite {
   // Y position of top of sprite
   byte b0;
   // Tile index number
   byte b1;
   byte b2;
   byte b3;

   public OamSprite(byte b0, byte b1, byte b2, byte b3) {
      this.b0 = b0;
      this.b1 = b1;
      this.b2 = b2;
      this.b3 = b3;
   }

   public readonly byte Index8x8 => b1;
   public readonly byte X => b3;
   public readonly byte Y => b0;

   // attributes
   public readonly byte Palette => (byte)(b2 & 0b_0000_0011);
   public readonly bool Priority => (b2 & BitFlags.F5) > 1;
   public readonly bool HorizontalFlip => (b2 & BitFlags.F6) > 1;
   public readonly bool VerticalFlip => (b2 & BitFlags.F7) > 1;
}
