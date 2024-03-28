namespace Mnes.Core.Machine.PPU;

// https://www.nesdev.org/wiki/PPU_registers
public sealed class Ppu
{
   public readonly PpuRegisters Registers = new();
   byte[] Vram = new byte[0x2000];
   byte[] Oam = new byte[0x100];

   public void SetPowerUpState() {
      Registers.SetPowerUpState();
      Array.Clear(Vram);
      Array.Clear(Oam);
   }

   public void Tick() {

   }
}
