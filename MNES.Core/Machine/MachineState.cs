using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNES.Core.Machine
{
    public class MachineState
    {
        INesHeader header;

        // 2KB onboard RAM
        public ushort[] Memory = new ushort[8000];

        public ushort[] Rom;

        // CPU register values
        public CpuRegisters CpuRegisters = new();

        public MachineState(string rom_path)
        {
            var nes_bytes = File.ReadAllBytes(rom_path);
            header = new INesHeader(nes_bytes);
            Rom = new ushort[(nes_bytes.Length - 8) / 2];
            Buffer.BlockCopy(
                nes_bytes, 
                16,
                Rom, 
                0, 
                nes_bytes.Length - 16);
        }
    }
}
