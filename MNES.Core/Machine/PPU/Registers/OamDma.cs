namespace Mnes.Core.Machine.PPU.Registers;

public sealed class OamDma : Register {
   public OamDma(MachineState m) : base(m) { }

   public override void CpuWrite(byte value) {
      Machine.Ppu.Registers.OpenBus = value;
      Machine.Cpu.SuspendCycles = 513;
      ushort page = (ushort)(value << 8);
      var oam_address = Machine.Ppu.Registers.OamAddr.Get();
      for (ushort i = page; i < page + 0xFF; i++) {
         Machine.Ppu.Oam[(oam_address + i) % 0xFF] =
            Machine[(ushort)(page + i)];
      }
   }

   public override byte CpuRead() =>
      Machine.Ppu.Registers.OpenBus;
}
