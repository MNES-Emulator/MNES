using Mnes.Core.Machine.Logging;

namespace Mnes.Core.Machine;

// https://www.nesdev.org/wiki/CPU_registers
/// <summary> Represents all the registers of the CPU. </summary>
public class CpuRegisters
{
    public enum StatusFlagType
    {
        /// <summary> The C flag. </summary>
        Carry =             0b_0000_0001,
        /// <summary> The Z flag. </summary>
        Zero =              0b_0000_0010,
        /// <summary> The I flag. </summary>
        InerruptDisable =   0b_0000_0100,
        /// <summary> The D flag. </summary>
        Decimal =           0b_0000_1000,
        /// <summary> The B flag. </summary>
        BFlag =             0b_0001_0000,
        /// <summary> The 1 flag. </summary>
        _1 =                0b_0010_0000,
        /// <summary> The V flag. </summary>
        Overflow =          0b_0100_0000,
        /// <summary> The N flag. </summary>
        Negative =          0b_1000_0000,
    }

    public enum RegisterType
    {
        A = 0,
        X = 1,
        Y = 2,
        S = 3,
        P = 4,
    }

    /// <summary> The 5 8-bit registers. </summary>
    readonly byte[] registers = new byte[5];
    /// <summary> The program counter. </summary>
    public ushort PC;


    public byte this[RegisterType index]
    {
        get => registers[(int)index];
        set => registers[(int)index] = value;
    }

    /// <summary> The accumulator. </summary>
    public byte A { get => registers[0]; set => SetRegisterAndFlags(RegisterType.A, value); }

    /// <summary> The X register. </summary>
    public byte X { get => registers[1]; set => SetRegisterAndFlags(RegisterType.X, value); }

    /// <summary> The Y register. </summary>
    public byte Y { get => registers[2]; set => SetRegisterAndFlags(RegisterType.Y, value); }

    /// <summary> The stack pointer. </summary>
    public byte S { get => registers[3]; set => registers[3] = value; }

    /// <summary> The status register. </summary>
    public byte P { get => registers[4]; set => registers[4] = (byte)(value | (byte)StatusFlagType._1); }

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
        SetFlag(StatusFlagType.Negative, (value & 0b_1000_0000) > 0);
        SetFlag(StatusFlagType.Zero, value == 0);
    }

    /// <summary> Set relevant flags from value in memory. </summary>
    /// <param name="value"></param>
    public void UpdateFlags(byte value)
    {
        SetFlag(StatusFlagType.Negative, (value & 0b_1000_0000) > 0);
        SetFlag(StatusFlagType.Zero, value == 0);
    }

    public bool HasFlag(StatusFlagType flag) => (P & (byte)flag) > 0;
    public void SetFlag(StatusFlagType flag) => P |= (byte)flag;
    public void SetFlag(StatusFlagType flag, bool value)
    {
        if (value) P |= (byte)flag;
        else P &= (byte)~(byte)flag;
    }
    public void ClearFlag(StatusFlagType flag) => P &= (byte)~flag;

    public CpuRegisterLog GetLog() => new(this);
}
