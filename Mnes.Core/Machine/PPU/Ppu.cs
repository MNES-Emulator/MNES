using Mnes.Core.Utility;

namespace Mnes.Core.Machine.PPU;

// Credit to https://github.com/jeb495/C-Sharp-NES-Emulator/blob/master/dotNES/PPU.Core.cs for partial use as a reference here.
// https://www.nesdev.org/wiki/PPU_registers
public sealed class Ppu {
   readonly MachineState _machine;
   public PpuRegisters Registers { get; }
   public PpuPalette Palette { get; }
   public byte[] Vram { get; } = new byte[0x1000]; // Is this supposed to be 0x1000? 0x800? Does anyone know what a kilobyte is????
   public byte[] Oam { get; } = new byte[0x100];
   public PpuMapper Mapper { get; }

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

   public int Cycle { get; private set; }
   public int Scanline { get; private set; }

   //int skip_cycles;

   long _tile_shift_register;
   byte _current_nt;
   Byte2 _current_color;
   byte _current_bg_tile_high;
   byte _current_bg_tile_low;

   int _ticks_since_vblank;

   public Ppu(MachineState m) {
      _machine = m;
      Registers = new(m);
      Mapper = new(m, this);
      Palette = new();
   }

   public void SetPowerUpState() {
      Registers.SetPowerUpState();
      Cycle = 21; // probably a hack
      Array.Clear(Vram);
      Array.Clear(Oam);
   }

   // https://www.nesdev.org/wiki/PPU_rendering
   public void Tick() {
      var visible_cycle = Cycle < SCREEN_WIDTH && Scanline < SCREEN_HEIGHT;
      var prefetch_cycle = Cycle >= 321 && Cycle <= 336;
      var fetch_cycle = visible_cycle || prefetch_cycle;

      if (Registers.PpuStatus.VBlankHasStarted) _ticks_since_vblank++;
      if (Cycle == 0 && Scanline == 0) screen_pos = 0;

      // the PPU performs memory fetches on dots 321-336 and 1-256 of scanlines 0-239 and 261
      // https://www.nesdev.org/w/images/default/4/4f/Ppu.svg
      if (Scanline < 240 && Scanline > 0 || Scanline == 261) {
         if (visible_cycle)
            ProcessPixel(Cycle - 1, Scanline);

         // During pixels 280 through 304 of this scanline, the vertical scroll bits are reloaded
         if (Scanline == -1 && 280 <= Cycle && Cycle <= 304)
            Registers.Internal.ReloadScrollY();

         if (fetch_cycle)
         {
            _tile_shift_register <<= 4;
            var cycle4 = Cycle & 0b_1111;

            if (cycle4 == 0)
            {
               if (Cycle == 256) Registers.Internal.IncrementScrollY();
               else Registers.Internal.IncrementScrollX();
               ShiftTileRegister();
            }
            if (cycle4 == 1) _current_nt = this[(ushort)(0x2000 + Registers.Internal.V % 0x1000)];
            if (cycle4 == 3) _current_color = ReadAttribute();
            if (cycle4 == 5) _current_bg_tile_low = ReadTileByte(false);
            if (cycle4 == 7) _current_bg_tile_high = ReadTileByte(true);
         }
      }


      if (Cycle == 1) {
         if (Scanline == 241) {
            Registers.PpuStatus.VBlankHasStarted = true;
            if (Registers.PPUCTRL.NMIEnabled) NMI_output = true;
         }

         if (Scanline == -1) {
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

   byte ReadTileByte(bool high) {
      var address = Registers.PPUCTRL.BackgroundTableAddress + _current_nt * 16 + Registers.Internal.FineY;
      return high ? this[(ushort)(address + 8)] : this[(ushort)address];
   }

   public void ProcessPixel(int x, int y) {
      //if (Registers.PpuMask.ShowBg)
      ProcessBackgroundForPixel(x, y);
      //if (Registers.PpuMask.ShowSprites)
      //   ProcessSpritesForPixel(x, y);

      if (y != -1) screen_pos++;
   }

   void ProcessBackgroundForPixel(int cycle, int scanline) {
      var paletteEntry = (uint)(_tile_shift_register >> 32 >> (7 - Registers.Internal.X) * 4) & 0x0F;
      if (paletteEntry % 4 == 0) paletteEntry = 0;

      if (scanline != -1)
         Screen.WriteRgb(screen_pos, Palette.SpritePaletteIndexes[this[(ushort)(0x3F00u + paletteEntry)] & 0x3F]);
   }

   void ShiftTileRegister() {
      for (var x = 0; x < 8; x++) {
         var palette = ((_current_bg_tile_high & 0x80) >> 6) | ((_current_bg_tile_low & 0x80) >> 7);
         _tile_shift_register |= (uint)((palette + _current_color * 4) << ((7 - x) * 4));
         _current_bg_tile_low <<= 1;
         _current_bg_tile_high <<= 1;
      }
   }

   void IncrementDot() {
      if (++Cycle != SCANLINE_WIDTH)
         return;

      Cycle = 0;
      if (++Scanline == SCANLINE_HEIGHT)
         Scanline = -1;
   }
}
