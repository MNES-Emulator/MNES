using Mnes.Core.Utility;

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
      res |= SpriteOverflow ? BitFlags.F5 : 0;
      res |= Sprite0Hit ? BitFlags.F6 : 0;
      res |= VBlankHasStarted ? BitFlags.F7 : 0;
      Machine.Ppu.Registers.OpenBus = (byte)res;
      VBlankHasStarted = false;
      Machine.Ppu.Registers.Internal.W = false;
      return (byte)res;
   }

   public override void SetPowerUpState() {
      SpriteOverflow = false;
      Sprite0Hit = false;
      VBlankHasStarted = false;
      base.SetPowerUpState();
   }
}
