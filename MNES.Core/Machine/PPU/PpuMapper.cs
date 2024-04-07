﻿namespace Mnes.Core.Machine.PPU;

// https://www.nesdev.org/wiki/PPU_memory_map
public sealed class PpuMapper {
   readonly Ppu _ppu;
   readonly MachineState _machine;

   public ushort PatternTable0MachineAddress = 0x0000;
   public ushort PatternTable1MachineAddress = 0x1000;

   public PpuMapper(MachineState machine, Ppu ppu) {
       _ppu = ppu;
      _machine = machine;
   }

   public byte this[ushort i] =>
      // Pattern tables
      // CHR maps at 0x0000 .. 0x1FFF (8k) in /PPU/ address space.
      i < 0x1000 ? _machine[(ushort)(PatternTable0MachineAddress + i)] :
      i < 0x2000 ? _machine[(ushort)(PatternTable1MachineAddress + i - 0x1000)] :

      // Nametables
      i >= 0x2000 && i <= 0x23BF ? _ppu.Vram[i - 0x2000] :
      i >= 0x2400 && i <= 0x27FF ? _ppu.Vram[i - 0x2000] :
      i >= 0x2800 && i <= 0x2BFF ? _ppu.Vram[i - 0x2000] :
      i >= 0x2C00 && i <= 0x2FFF ? _ppu.Vram[i - 0x2000] :

      // a mirror of the 2kB region from $2000-2EFF
      i >= 0x3F00 && i <= 0x3FFF ? this[(ushort)(i - 0x1000)] :

      // The palette for the background runs from VRAM $3F00 to $3F0F; the palette for the sprites runs from $3F10 to $3F1F. Each color takes up one byte.

      _ppu.Registers.OpenBus;
}