namespace Mnes.Core.Machine.PPU.Registers;

public sealed class PpuData : Register {
   public PpuData(MachineState m) : base(m) { }

   public override void CpuWrite(byte value) {
      Machine.Ppu.Vram[Machine.Ppu.Registers.PpuAddr.Address] = value;
      Machine.Ppu.Registers.PpuAddr.Address += Machine.Ppu.Registers.PPUCTRL.VramAddressIncrement;
      Machine.Ppu.Registers.OpenBus = value;
   }

   public override byte CpuRead() {
      var value = Machine.Ppu.Vram[Machine.Ppu.Registers.PpuAddr.Address];
      Machine.Ppu.Registers.PpuAddr.Address += Machine.Ppu.Registers.PPUCTRL.VramAddressIncrement;
      Machine.Ppu.Registers.OpenBus = value;
      return value;
   }
}
