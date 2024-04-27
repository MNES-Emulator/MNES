using Mnes.Core.Utility;

namespace Mnes.Core.Machine.PPU.Registers;

// https://www.nesdev.org/wiki/PPU_registers
public class PpuInternalRegisters {
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
   public Byte5 CoarseX => (Byte5)(int)V;
   public Byte5 CoarseY => (Byte5)(V >> 5);
}
