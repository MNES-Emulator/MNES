using Mnes.Core.Utility;

namespace Mnes.Core.Machine.PPU;

// Credit to https://github.com/jeb495/C-Sharp-NES-Emulator/blob/master/dotNES/PPU.Core.cs for partial use as a reference here.
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

   public MnesScreen Screen { get; } = new();
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

   //int skip_cycles;

   long _tile_shift_register;
   byte _current_nt;
   Byte2 _current_color;
   byte _current_bg_tile_high;
   byte _current_bg_tile_low;

   int _ticks_since_vblank;

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
      var visible_cycle = cycle < SCREEN_WIDTH && scanline < SCREEN_HEIGHT;
      //var prefetch_cycle = cycle >= 321 && cycle <= 336;
      //var fetch_cycle = visible_cycle || prefetch_cycle;

      if (Registers.PpuStatus.VBlankHasStarted) _ticks_since_vblank++;
      if (cycle == 0 && scanline == 0) screen_pos = 0;

      // the PPU performs memory fetches on dots 321-336 and 1-256 of scanlines 0-239 and 261
      // https://www.nesdev.org/w/images/default/4/4f/Ppu.svg
      if ((scanline < 240 && scanline > 0) || scanline == 261)
      {
         if (visible_cycle)
            ProcessPixel(cycle - 1, scanline);

         // During pixels 280 through 304 of this scanline, the vertical scroll bits are reloaded
         if (scanline == -1 && 280 <= cycle && cycle <= 304)
            Registers.Internal.ReloadScrollY();

         if (fetch_cycle)
         {
            _tile_shift_register <<= 4;
            var cycle4 = cycle & 0b_1111;

            if (cycle4 == 0)
            {
               if (cycle == 256) Registers.Internal.IncrementScrollY();
               else Registers.Internal.IncrementScrollX();
               ShiftTileRegister();
            }
            if (cycle4 == 1) _current_nt = this[(ushort)(0x2000 + Registers.Internal.V % 0x1000)];
            if (cycle4 == 3) _current_color = ReadAttribute();
            if (cycle4 == 5) _current_bg_tile_low = ReadTileByte(false);
            if (cycle4 == 7) _current_bg_tile_high = ReadTileByte(true);
         }
      }
      

      if (cycle == 1) {
         if (scanline == 241) {
            Registers.PpuStatus.VBlankHasStarted = true;
            if (Registers.PPUCTRL.NMIEnabled) NMI_output = true;
         }

         if (scanline == -1) {
            _ticks_since_vblank = 0;
            Registers.PpuStatus.VBlankHasStarted = true;
            Registers.PpuStatus.Sprite0Hit = false;
            Registers.PpuStatus.SpriteOverflow = false;
         }
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

   private byte ReadTileByte(bool high) {
      var address = Registers.PPUCTRL.BackgroundTableAddress + _current_nt * 16 + Registers.Internal.FineY;
      return high ? this[(ushort)(address + 8)] : this[(ushort)address];
   }

   public void ProcessPixel(int x, int y)
   {
      //if (Registers.PpuMask.ShowBg)
      ProcessBackgroundForPixel(x, y);
      //if (Registers.PpuMask.ShowSprites)
      //   ProcessSpritesForPixel(x, y);

      if (y != -1) screen_pos++;
   }

   private void ProcessBackgroundForPixel(int cycle, int scanline)
   {
      uint paletteEntry = (uint)(_tile_shift_register >> 32 >> (int)((7 - Registers.Internal.X) * 4)) & 0x0F;
      if (paletteEntry % 4 == 0) paletteEntry = 0;

      if (scanline != -1)
      {
         Screen.WriteRgb(screen_pos, Palette.SpritePaletteIndexes[this[(ushort)(0x3F00u + paletteEntry)] & 0x3F]);
      }
   }

   private void ShiftTileRegister() {
      for (int x = 0; x < 8; x++) {
         var palette = ((_current_bg_tile_high & 0x80) >> 6) | ((_current_bg_tile_low & 0x80) >> 7);
         _tile_shift_register |= (uint)((palette + _current_color * 4) << ((7 - x) * 4));
         _current_bg_tile_low <<= 1;
         _current_bg_tile_high <<= 1;
      }
   }

   void IncrementDot() {
      if (++cycle == SCANLINE_WIDTH) { 
         cycle = 0;
         if (++scanline == SCANLINE_HEIGHT) { scanline = -1; }
      }
   }
}
