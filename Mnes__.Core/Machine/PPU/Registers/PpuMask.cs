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
      Greyscale = value.HasBit(0);
      ShowBgInLeft8PixelsOfScreen = value.HasBit(1);
      ShowSpritesInLeft8PixelsOfScreen = value.HasBit(2);
      ShowBg = value.HasBit(3);
      ShowSprites = value.HasBit(4);
      EmphasizeRed = value.HasBit(5);
      EmphasizeGreen = value.HasBit(6);
      EmphasizeBlue = value.HasBit(7);

      Machine.Ppu.Registers.OpenBus = value;
   }

   public override byte CpuRead() {
      Machine.Ppu.Registers.OpenBus = Value;
      return Value;
   }

   public override void SetPowerUpState() =>
      CpuWrite(0);
}
