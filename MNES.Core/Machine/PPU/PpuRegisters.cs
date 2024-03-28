namespace Mnes.Core.Machine.PPU;

public sealed class PpuRegisters {
   readonly byte[] registers = new byte[8];

   // VPHB SINN
   // NMI enable (V), PPU master/slave (P), sprite height (H), background tile select (B), sprite tile select (S), increment mode (I), nametable select (NN)
   public byte PpuCtrl => registers[0];
   // BGRs bMmG
   // color emphasis (BGR), sprite enable (s), background enable (b), sprite left column enable (M), background left column enable (m), greyscale (G)
   public byte PpuMask => registers[1];
   // VSO- ----
   // 	vblank (V), sprite 0 hit (S), sprite overflow (O); read resets write pair for $2005/$2006
   public byte PpuStatus => registers[2];
   // aaaa aaaa
   // OAM read/write address
   public byte OamAddr => registers[3];
   // dddd dddd
   // OAM data read/write
   public byte OamData => registers[4];
   // xxxx xxxx
   // fine scroll position (two writes: X scroll, Y scroll)
   public byte PpuScroll => registers[5];
   // aaaa aaaa
   // PPU read/write address (two writes: most significant byte, least significant byte)
   public byte PpuAddr => registers[6];
   // dddd dddd
   // PPU data read/write
   public byte PpuData => registers[7];

   public byte this[int index] {
      get => registers[index];
      set => registers[index] = value;
   }

   public void SetPowerUpState() =>
      Array.Clear(registers);
}
