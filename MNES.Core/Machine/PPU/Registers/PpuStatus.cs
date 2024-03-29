namespace Mnes.Core.Machine.PPU.Registers;

public sealed class PpuStatus : Register {
   public PpuStatus(MachineState m) : base(m) { }

   public bool SpriteOverflow { get; set; }
   public bool Sprite0Hit { get; set; }
   public bool VBlankHasStarted { get; set; }

   public override void CpuWrite(byte value) {
      Machine.Ppu.Registers.OpenBus = value;
   }

   public override byte CpuRead() {
      var res = Machine.Ppu.Registers.OpenBus & 0b_0001_1111;
      res |= SpriteOverflow ? 0b_0010_0000 : 0b_0000_0000;
      res |= Sprite0Hit ? 0b_0100_0000 : 0b_0000_0000;
      res |= VBlankHasStarted ? 0b_1000_0000 : 0b_0000_0000;
      Machine.Ppu.Registers.OpenBus = (byte)res;
      VBlankHasStarted = false;
      Machine.Ppu.W = false;
      return (byte)res;
   }

   public override void SetPowerUpState() {
      SpriteOverflow = false;
      Sprite0Hit = false;
      VBlankHasStarted = false;
      base.SetPowerUpState();
   }
}
