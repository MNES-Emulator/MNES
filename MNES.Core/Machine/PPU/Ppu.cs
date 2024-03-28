namespace Mnes.Core.Machine.PPU;

// https://www.nesdev.org/wiki/PPU_registers
public sealed class Ppu
{
   readonly MachineState machine;
   public readonly PpuRegisters Registers;
   byte[] Vram = new byte[0x2000];
   byte[] Oam = new byte[0x100];

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
