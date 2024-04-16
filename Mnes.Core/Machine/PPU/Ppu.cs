using Mnes.Core.Utility;

namespace Mnes.Core.Machine.PPU;

// https://www.nesdev.org/wiki/PPU_registers
public sealed class Ppu {
   readonly MachineState machine;
   public readonly PpuRegisters Registers;
   public readonly PpuPalette Palette;
   public readonly byte[] Vram = new byte[0x1000]; // Is this supposed to be 0x1000? 0x800? Does anyone know what a kilobyte is????
   public readonly byte[] Oam = new byte[0x100];
   public readonly PpuMapper Mapper;

   // NMI interrupt https://www.nesdev.org/wiki/NMI
   public bool NMI_occurred;
   public bool NMI_output;

   public long TickCount;

   public MnesScreen Screen { get; } = new MnesScreen();
   public int screen_pos;

   public const int SCREEN_WIDTH = 256;
   public const int SCREEN_HEIGHT = 240;

   const int SCANLINE_WIDTH = 341;
   const int SCANLINE_HEIGHT = 261;

   public byte this[ushort i] {
      get => Mapper[i];
      set => Mapper[i] = value;
   }

   int cycle;
   int scanline;

   int skip_cycles;

   byte _current_nt;
   Byte2 _current_color;

   public Ppu(MachineState m) {
      machine = m;
      Registers = new(m);
      Mapper = new(m, this);
      Palette = new();
   }

   public void SetPowerUpState() {
      Registers.SetPowerUpState();
      Array.Clear(Vram);
      Array.Clear(Oam);
   }

   // https://www.nesdev.org/wiki/PPU_rendering
   public void Tick() {
      bool visible_cycle = cycle < SCREEN_WIDTH && scanline < SCREEN_HEIGHT;
      bool prefetch_cycle = cycle >= 321 && cycle <= 336;
      bool fetch_cycle = visible_cycle || prefetch_cycle;

      //if (Registers.PpuStatus.VBlankHasStarted) ClocksSinceVBlank++;

      if (scanline < 240)
      {
         if (visible_cycle)
            ProcessPixel();
      }

      // the PPU performs memory fetches on dots 321-336 and 1-256 of scanlines 0-239 and 261
      // https://www.nesdev.org/w/images/default/4/4f/Ppu.svg
      if ((scanline < 240 && scanline > 0) || scanline == 261)
      {
         var cycle4 = cycle & 0b_1111;

         if (cycle4 == 1) _current_nt = this[(ushort)(0x2000 + Registers.Internal.V % 0x1000)];
         if (cycle4 == 3) _current_color = ReadAttribute();
      }

      if (cycle == 1 && scanline == 241)
      {
         Registers.PpuStatus.VBlankHasStarted = true;
         NMI_output = true;
      }

      IncrementDot();
      TickCount++;
   }

   // https://www.nesdev.org/wiki/PPU_scrolling#Tile_and_attribute_fetching
   Byte2 ReadAttribute() {
      var v = Registers.Internal.V;
      var attribute_address = 0x23C0 | (v & 0x0C00) | ((v >> 4) & 0x38) | ((v >> 2) & 0x07);
      var current_color = (Byte2)(this[(ushort)attribute_address] >> ((Registers.Internal.CoarseX & 2) | ((Registers.Internal.CoarseY & 2) << 1)));
      return current_color;
   }

   void ProcessPixel()
   {

   }

   void IncrementDot() {
      cycle++;
      if (cycle == SCANLINE_WIDTH) { cycle = 0; scanline++; }
      if (scanline == SCANLINE_HEIGHT) { scanline = 0; }
   }
}
