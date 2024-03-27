namespace Mnes.Core.Machine;

// https://www.nesdev.org/wiki/INES
public sealed class INesHeader {
    static readonly byte[] ines_text = new byte[] { 0x4E, 0x45, 0x53, 0x1A };

    public readonly int PrgRomSize;
    public readonly int ChrRomSize;
    /// <summary>
    /// Nametable arrangement: 0: vertical arrangement ("horizontal mirrored") (CIRAM A10 = PPU A11) 1: horizontal arrangement ("vertically mirrored") (CIRAM A10 = PPU A10)
    /// </summary>
    public readonly bool NameTableArrangment;
    /// <summary>
    /// Cartridge contains battery-backed PRG RAM ($6000-7FFF) or other persistent memory
    /// </summary>
    public readonly bool HasBatteryBackedPrgRam;
    /// <summary>
    /// 512-byte trainer at $7000-$71FF (stored before PRG data)
    /// </summary>
    public readonly bool HasTrainer;
    public readonly bool AlternativeNametableLayout;
    public readonly byte MapperNumber;
    public readonly bool VsUnisystem;
    public readonly bool PlayChoice10;
    public readonly bool Nes2_0;

    public readonly int PrgRamSize;

    public readonly NesTimer.RegionType TvSystem;
    public readonly bool PrgRam_6000_7FFF_Present;
    public readonly bool HasBusConflicts;

    public INesHeader(byte[] nes_file) {
        // 0-3	Constant $4E $45 $53 $1A (ASCII "NES" followed by MS-DOS end-of-file)
        if (!nes_file[..4].SequenceEqual(ines_text)) throw new Exception("ROM invalid, INES header not found.");
        // 4	Size of PRG ROM in 16 KB units
        PrgRomSize = nes_file[4] * 16000;
        // 5	Size of CHR ROM in 8 KB units (value 0 means the board uses CHR RAM)
        ChrRomSize = nes_file[5] * 8000;
        // 6	Flags 6 – Mapper, mirroring, battery, trainer
        NameTableArrangment = (nes_file[6] & 0b0000_0001) > 1;
        HasBatteryBackedPrgRam = (nes_file[6] & 0b0000_0010) > 1;
        HasTrainer = (nes_file[6] & 0b0000_0100) > 1;
        MapperNumber = (byte)(nes_file[6] >> 4);
        // 7	Flags 7 – Mapper, VS/Playchoice, NES 2.0
        VsUnisystem = (nes_file[7] & 0b0000_0001) > 1;
        PlayChoice10 = (nes_file[7] & 0b0000_0010) > 1;
        Nes2_0 = ((nes_file[7] & 0b0000_0100) == 0) && ((nes_file[7] & 0b0000_1000) > 1);
        // 8	Flags 8 – PRG-RAM size (rarely used extension)
        PrgRamSize = nes_file[6] * 8000;
        // 9	Flags 9 – TV system (rarely used extension)
        // 10	Flags 10 – TV system, PRG-RAM presence (unofficial, rarely used extension)
        // 11-15	Unused padding (should be filled with zero, but some rippers put their name across bytes 7-15)
    }
}
