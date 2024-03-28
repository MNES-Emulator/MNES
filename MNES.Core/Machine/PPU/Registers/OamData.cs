﻿namespace Mnes.Core.Machine.PPU.Registers;

public sealed class OamData : Register {
   public OamData(MachineState m) : base(m) { }

   public override void CpuWrite(byte value) {
      Machine.Ppu.Registers.OpenBus = value;
   }

   public override byte CpuRead() {
      Machine.Ppu.Registers.OpenBus = Value;
      return Value;
   }
}