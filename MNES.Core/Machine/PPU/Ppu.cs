using Mnes.Core.Utility;

namespace Mnes.Core.Machine.PPU;

// https://www.nesdev.org/wiki/PPU_registers
public sealed class Ppu {
   readonly MachineState machine;
   public readonly PpuRegisters Registers;
   public readonly byte[] Vram = new byte[0x2000];
   public readonly byte[] Oam = new byte[0x100];

   // NMI interrupt https://www.nesdev.org/wiki/NMI
   public bool NMI_occurred;
   public bool NMI_output;

   // Internal registers
   public Ushort15 V;
   public Ushort15 T;
   public Byte3 X;
   public bool W;

   public Ppu(MachineState m) {
      machine = m;
      Registers = new(m);
   }

   public void SetPowerUpState() {
      Registers.SetPowerUpState();
      Array.Clear(Vram);
      Array.Clear(Oam);
   }

   public void Tick() {

   }
}
