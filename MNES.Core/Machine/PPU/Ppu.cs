using Mnes.Core.Utility;

namespace Mnes.Core.Machine.PPU;

// https://www.nesdev.org/wiki/PPU_registers
public sealed class Ppu {
   readonly MachineState machine;
   public readonly PpuRegisters Registers;
   public readonly byte[] Vram = new byte[0x2000];
   public readonly byte[] Oam = new byte[0x100];
   readonly PpuMapper mapper;

   // NMI interrupt https://www.nesdev.org/wiki/NMI
   public bool NMI_occurred;
   public bool NMI_output;

   // Internal registers
   public Ushort15 V;
   public Ushort15 T;
   public Byte3 X;
   public bool W;

   public const int SCREEN_WIDTH = 256;
   public const int SCREEN_HEIGHT = 240;

   public readonly uint[] RawBitmap = new uint[SCREEN_WIDTH * SCREEN_HEIGHT];

   const int SCANLINE_WIDTH = 341;
   const int SCANLINE_HEIGHT = 261;

   int x_pos;
   int y_pos;

   public Ppu(MachineState m) {
      machine = m;
      Registers = new(m);
      mapper = new(m, this);
   }

   public void SetPowerUpState() {
      Registers.SetPowerUpState();
      Array.Clear(Vram);
      Array.Clear(Oam);
   }

   // https://www.nesdev.org/wiki/PPU_rendering
   public void Tick() {
      if (x_pos == 1)
      {
         if (y_pos == 241)
         {
            Registers.PpuStatus.VBlankHasStarted = true;
            NMI_output = true;
         }
         // leave here
      }
      IncrementDot();
   }

   void IncrementDot() {
      x_pos++;
      if (x_pos == SCANLINE_WIDTH) { x_pos = 0; y_pos++; }
      if (y_pos == SCANLINE_HEIGHT) { y_pos = 0; }
   }
}
