using Mnes.Core.Utility;

namespace Mnes.Core.Machine.PPU.Registers;

public sealed class PpuMask : Register {
   public PpuMask(MachineState m) : base(m) { }

   public bool Greyscale { get; private set; }
   public bool ShowBgInLeft8PixelsOfScreen { get; private set; }
   public bool ShowSpritesInLeft8PixelsOfScreen { get; private set; }
   public bool ShowBg { get; private set; }
   public bool ShowSprites { get; private set; }
   public bool EmphasizeRed { get; private set; }
   public bool EmphasizeGreen { get; private set; }
   public bool EmphasizeBlue { get; private set; }

   public override void CpuWrite(byte value) {
      Greyscale = (value & BitFlags.F0) > 0;
      ShowBgInLeft8PixelsOfScreen = (value & BitFlags.F1) > 0;
      ShowSpritesInLeft8PixelsOfScreen = (value & BitFlags.F2) > 0;
      ShowBg = (value & BitFlags.F3) > 0;
      ShowSprites = (value & BitFlags.F4) > 0;
      EmphasizeRed = (value & BitFlags.F5) > 0;
      EmphasizeGreen = (value & BitFlags.F6) > 0;
      EmphasizeBlue = (value & BitFlags.F7) > 0;

      Machine.Ppu.Registers.OpenBus = value;
   }

   public override byte CpuRead() {
      Machine.Ppu.Registers.OpenBus = Value;
      return Value;
   }

   public override void SetPowerUpState() =>
      CpuWrite(0);
}
