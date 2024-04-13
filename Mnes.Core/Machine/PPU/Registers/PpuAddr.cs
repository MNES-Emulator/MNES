using Mnes.Core.Utility;

namespace Mnes.Core.Machine.PPU.Registers;

public sealed class PpuAddr : Register {
   public PpuAddr(MachineState m) : base(m) { }

   const ushort LowAnd = 0b_00000000_11111111;
   const ushort HighAnd = 0b_11111111_00000000;

   // Valid addresses are $0000–$3FFF; higher addresses will be mirrored down.
   public override void CpuWrite(byte value) {
      Machine.Ppu.Registers.OpenBus = value;

      var w = Machine.Ppu.Registers.Internal.W;
      var ushort_value = w ? value : value << 8;
      Machine.Ppu.Registers.Internal.V = (Ushort15)((Machine.Ppu.Registers.Internal.V & (w ? HighAnd : LowAnd)) | ushort_value);

      // Remove once validated
      // Debug.WriteLine($"Wrote to PpuAddr: value: {value}, result = {Address}, lower: {w}");

      Machine.Ppu.Registers.Internal.W = !Machine.Ppu.Registers.Internal.W;
   }

   public override byte CpuRead() =>
      Machine.Ppu.Registers.OpenBus;
}
