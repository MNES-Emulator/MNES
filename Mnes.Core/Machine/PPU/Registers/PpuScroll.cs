﻿namespace Mnes.Core.Machine.PPU.Registers;

public sealed class PpuScroll : Register {
   public PpuScroll(MachineState m) : base(m) { }

   public bool XHighBit { get; set; }
   public bool YHighBit { get; set; }

   public byte ScrollX { get; set; }
   public byte ScrollY { get; set; }

   public override void CpuWrite(byte value) {
      Machine.Ppu.Registers.OpenBus = value;
      if (Machine.Ppu.Registers.Internal.W) ScrollY = value;
      else ScrollX = value;
      Machine.Ppu.Registers.Internal.W = !Machine.Ppu.Registers.Internal.W;
   }

   public override byte CpuRead() =>
      Machine.Ppu.Registers.OpenBus;
}
