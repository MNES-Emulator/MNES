namespace Mnes.Core.Machine.PPU.Registers;

public sealed class PpuScroll : Register {
   public PpuScroll(MachineState m) : base(m) { }

   public bool XHighBit { get; set; }
   public bool YHighBit { get; set; }


   public override void CpuWrite(byte value) {
      Machine.Ppu.Registers.OpenBus = value;
   }

   public override byte CpuRead() {
      Machine.Ppu.Registers.OpenBus = Value;
      return Value;
   }
}
