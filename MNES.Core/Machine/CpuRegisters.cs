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
        public enum StatusFlagType : byte
        {
            Carry = 1,
            Zero = 2,
            InerruptDisable = 4,
            Decimal = 8,
            BFlag = 16,
            _1 = 32,
            Overflow = 64,
            Negative = 128,
        }

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
