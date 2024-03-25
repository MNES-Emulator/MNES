using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mnes.Core.Machine
{
    // https://www.nesdev.org/wiki/PPU_registers
    public class Ppu
    {
        public byte[] Registers = new byte[8];
        public byte OAMDMA;

        // VPHB SINN
        // NMI enable (V), PPU master/slave (P), sprite height (H), background tile select (B), sprite tile select (S), increment mode (I), nametable select (NN)
        byte PpuCtrl => Registers[0];
        // BGRs bMmG
        // color emphasis (BGR), sprite enable (s), background enable (b), sprite left column enable (M), background left column enable (m), greyscale (G)
        byte PpuMask => Registers[1];
        // VSO- ----
        // 	vblank (V), sprite 0 hit (S), sprite overflow (O); read resets write pair for $2005/$2006
        byte PpuStatus => Registers[2];
        // aaaa aaaa
        // OAM read/write address
        byte OamAddr => Registers[3];
        // dddd dddd
        // OAM data read/write
        byte OamData => Registers[4];
        // xxxx xxxx
        // fine scroll position (two writes: X scroll, Y scroll)
        byte PpuScroll => Registers[5];
        // aaaa aaaa
        // PPU read/write address (two writes: most significant byte, least significant byte)
        byte PpuAddr => Registers[6];
        // dddd dddd
        // PPU data read/write
        byte PpuData => Registers[7];

        public void SetPowerUpState()
        {
            Array.Clear(Registers);
            OAMDMA = 0;
        }
    }
}
