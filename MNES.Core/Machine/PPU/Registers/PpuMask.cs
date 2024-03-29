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
      Greyscale = (value & 0b_0000_0001) > 0;
      ShowBgInLeft8PixelsOfScreen = (value & 0b_0000_0010) > 0;
      ShowSpritesInLeft8PixelsOfScreen = (value & 0b_0000_0100) > 0;
      ShowBg = (value & 0b_0000_1000) > 0;
      ShowSprites = (value & 0b_0001_0000) > 0;
      EmphasizeRed = (value & 0b_0010_0000) > 0;
      EmphasizeGreen = (value & 0b_0100_0000) > 0;
      EmphasizeBlue = (value & 0b_1000_0000) > 0;

      Machine.Ppu.Registers.OpenBus = value;
   }

   public override byte CpuRead() {
      Machine.Ppu.Registers.OpenBus = Value;
      return Value;
   }

   public override void SetPowerUpState() =>
      CpuWrite(0);
}
