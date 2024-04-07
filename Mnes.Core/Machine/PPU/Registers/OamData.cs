namespace Mnes.Core.Machine.PPU.Registers;

public sealed class OamData : Register {
   public OamData(MachineState m) : base(m) { }

   public override void CpuWrite(byte value) {
      Machine.Ppu.Oam[Machine.Ppu.Registers.OamAddr.GetAndInc()] = value;
      Machine.Ppu.Registers.OpenBus = value;
   }

   public override byte CpuRead() {
      Machine.Ppu.Registers.OpenBus = Machine.Ppu.Oam[Machine.Ppu.Registers.OamAddr.Get()];
      return Machine.Ppu.Registers.OpenBus;
   }
}
