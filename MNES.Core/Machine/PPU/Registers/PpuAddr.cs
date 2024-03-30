namespace Mnes.Core.Machine.PPU.Registers;

public sealed class PpuAddr : Register {
   public PpuAddr(MachineState m) : base(m) { }

   // Valid addresses are $0000–$3FFF; higher addresses will be mirrored down.
   public byte UpperByte { get; set; }
   public byte LowerByte { get; set; }

   public override void CpuWrite(byte value) {
      Machine.Ppu.Registers.OpenBus = value;
      if (Machine.Ppu.W) LowerByte = value;
      else UpperByte = value;
      Machine.Ppu.W = !Machine.Ppu.W;
   }

   public override byte CpuRead() =>
      Machine.Ppu.Registers.OpenBus;
}
