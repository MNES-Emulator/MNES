using MNES.Core.Machine.Mappers;

namespace MNES.Core.Machine
{
    public class MachineState
    {
        INesHeader header;
        Mapper mapper;

        // 2KB onboard RAM
        public byte[] Ram = new byte[2000];

        public byte[] Rom;

        // CPU register values
        public CpuRegisters CpuRegisters = new();

        public PPU Ppu = new();

        Apu Apu = new();

        public MachineState(string rom_path)
        {
            var nes_bytes = File.ReadAllBytes(rom_path);
            header = new INesHeader(nes_bytes);
            Rom = new byte[nes_bytes.Length - 16];
            nes_bytes[16..].CopyTo(Rom, 0);
            mapper = Mapper.GetMapper(header, this);
            if (mapper == null) throw new NotImplementedException($"Mapper {header.MapperNumber} is not implemented.");
        }

        // obviously very slow but whatever
        public byte this[ushort index] { 
            get =>
                index < 0x2000 ? Ram[index % 0x2000] :
                index < 0x4000 ? Ppu.Registers[index % 8] :
                index < 0x4020 ? Apu.Registers[index - 0x4000] :
                mapper[index];
            set {
                if (index < 0x2000) Ram[index % 0x2000] = value;
                else if (index < 0x4000) Ppu.Registers[index % 8] = value;
                else if (index < 0x4020) Apu.Registers[index - 0x4000] = value;
                else mapper[index] = value;
            }
        }
    }
}
