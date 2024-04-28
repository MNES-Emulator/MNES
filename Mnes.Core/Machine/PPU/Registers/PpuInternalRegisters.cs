using Mnes.Core.Utility;

namespace Mnes.Core.Machine.PPU.Registers;

// https://www.nesdev.org/wiki/PPU_registers
public sealed class PpuInternalRegisters {
   /// <summary> During rendering, used for the scroll position.
   /// Outside of rendering, used as the current VRAM address. </summary>
   public Ushort15 V;
   /// <summary> During rendering, specifies the starting coarse-x
   /// scroll for the next scanline and the starting y scroll for
   /// the screen. Outside of rendering, holds the scroll or VRAM
   /// address before transferring it to v. </summary>
   public Ushort15 T;
   /// <summary> The fine-x position of the current scroll, used
   /// during rendering alongside v. </summary>
   public Byte3 X;
   /// <summary> Toggles on each write to either PPUSCROLL or PPUADDR,
   /// indicating whether this is the first or second write. Clears
   /// on reads of PPUSTATUS. Sometimes called the 'write latch' or
   /// 'write toggle'. </summary>
   public bool W;

   // https://www.nesdev.org/wiki/PPU_scrolling#Quick_coarse_X/Y_split
   //  First Second
   //  /¯¯¯¯¯¯¯¯¯\ /¯¯¯¯¯¯¯\
   // 0 0yy NN YY YYY XXXXX
   //   ||| || || ||| +++++-- coarse X scroll
   //   ||| || ++-+++-------- coarse Y scroll
   //   ||| ++--------------- nametable select
   //   +++------------------ fine Y scroll

   public Byte5 CoarseX {
      get => (Byte5)(int)V;
      set {
         V &= (Ushort15)0b_111_1111_1110_0000;
         V |= (Ushort15)(int)value;
      }
   }

   public Byte5 CoarseY {
      get => (Byte5)(V >> 5);
      set {
         V &= (Ushort15)0b_111_1100_0001_1111;
         V |= (Ushort15)(value << 5);
      }
   }

   public Byte2 NameTableSelect {
      get => (Byte2)(V >> 10);
      set {
         V &= (Ushort15)0b_111_0011_1111_1111;
         V |= (Ushort15)(value << 10);
      }
   }

   public Byte3 FineY {
      get => (Byte3)(V >> 12);
      set {
         V &= (Ushort15)0b_000_1111_1111_1111;
         V |= (Ushort15)(value << 12);
      }
   }

   void SwitchHorizontalNameTable() =>
      V ^= (Ushort15)0b_000_0100_0000_0000;

   void SwitchVerticalNameTable() =>
      V ^= (Ushort15)0b_000_1000_0000_0000;

   public void ReloadScrollX() =>
      V = (Ushort15)((V & 0b_111_1011_1110_0000) | (T & 0b_000_0100_0001_1111));

   public void ReloadScrollY() =>
      V = (Ushort15)((V & 0b_000_0100_0001_1111) | (T & 0b_111_1011_1110_0000));

   public void IncrementScrollX() {
      if (++CoarseX == 0)
         SwitchHorizontalNameTable();
   }

   public void IncrementScrollY() {
      if (++FineY == 0) {
         if (CoarseY++ == 29) {
            CoarseY = (Byte5)0;
            SwitchVerticalNameTable();
         }
      }
   }
}
