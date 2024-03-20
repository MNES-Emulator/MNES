using MNES.Core.Machine.Log;
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
            Carry =             0b_0000_0001,
            Zero =              0b_0000_0010,
            InerruptDisable =   0b_0000_0100,
            Decimal =           0b_0000_1000,
            BFlag =             0b_0001_0000,
            _1 =                0b_0010_0000,
            Overflow =          0b_0100_0000,
            Negative =          0b_1000_0000,
        }

        public enum StatusFlagClearType : byte
        {
            Carry =             0b_1111_1110,
            Zero =              0b_1111_1101,
            InerruptDisable =   0b_1111_1011,
            Decimal =           0b_1111_0111,
            BFlag =             0b_1110_1111,
            _1 =                0b_1101_1111,
            Overflow =          0b_1011_1111,
            Negative =          0b_0111_1111,
        }

        public enum RegisterType
        {
            A = 0,
            X = 1,
            Y = 2,
            S = 3,
            P = 4,
        }

        byte[] registers = new byte[5];

        /// <summary> The accumulator. </summary>
        public byte A { get => registers[0]; set => SetRegisterAndFlags(RegisterType.A, value); }

        /// <summary> The X register. </summary>
        public byte X { get => registers[1]; set => SetRegisterAndFlags(RegisterType.X, value); }

        /// <summary> The Y register. </summary>
        public byte Y { get => registers[2]; set => SetRegisterAndFlags(RegisterType.Y, value); }

        /// <summary> The program counter. </summary>
        public ushort PC;

        /// <summary> The stack pointer. </summary>
        public byte S { get => registers[3]; set => registers[3] = value; }

        /// <summary> The status register. </summary>
        public byte P { get => registers[4]; set => registers[4] = value; }

        public byte GetRegister(RegisterType r) => registers[(int)r];

        public void SetRegister(RegisterType r, byte value)
        {
            if ((int)r < (int)RegisterType.S) SetRegisterAndFlags(r, value);
            else registers[(int)r] = value;
        }

        /// <summary> Set register value and handle status flag updating. </summary>
        void SetRegisterAndFlags(RegisterType r, byte value)
        {
            registers[(int)r] = value;
            if ((value & 0b_1000_000) > 0) P |= (byte)StatusFlagType.Negative;
            if (value == 0) P |= (byte)StatusFlagType.Zero;
        }

        public bool HasFlag(StatusFlagType flag) => (P & (byte)flag) > 0;
        public void SetFlag(StatusFlagType flag) => P |= (byte)flag;
        public void ClearFlag(StatusFlagClearType flag) => P &= (byte)flag;

        public CpuRegisterLog GetLog() => new(this);
    }
}
