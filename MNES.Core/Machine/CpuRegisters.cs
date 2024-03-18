using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNES.Core.Machine
{
    // https://www.nesdev.org/wiki/CPU_registers
    /// <summary> Represents all the registers of the CPU. </summary>
    public class CpuRegisters
    {
        /// <summary> The accumulator. </summary>
        public byte A;

        /// <summary> The X register. </summary>
        public byte X;

        /// <summary> The Y register. </summary>
        public byte Y;

        /// <summary> The program counter. </summary>
        public ushort PC;

        /// <summary> The stack pointer. </summary>
        public byte S;

        /// <summary> The status register. </summary>
        public byte P;

        public bool GetFlag(byte flag) => (P & flag) > 0;
    }
}
