using Mnes.Core.Machine.Logging;

namespace Mnes.Core.Machine.CPU;

// https://www.nesdev.org/wiki/CPU_registers
/// <summary> Represents all the registers of the CPU. </summary>
public sealed partial class CpuRegisters {
   /// <summary> The 5 8-bit registers. </summary>
   readonly byte[] registers = new byte[5];
   /// <summary> The program counter. </summary>
   public ushort PC;

   public byte this[RegisterType reg] {
      get => registers[reg];
      set => registers[reg] = value;
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
   public byte P { get => registers[4]; set => registers[4] = value | StatusFlag._1; }

   public CpuRegisters() =>
      P |= StatusFlag._1;

   public byte GetRegister(RegisterType r) =>
      registers[r];

   public void SetRegister(RegisterType r, byte value) {
      if (r.SetsFlags)
         SetRegisterAndFlags(r, value);
      else
         registers[r] = value;
   }

   /// <summary> Set register value and handle status flag updating. </summary>
   void SetRegisterAndFlags(RegisterType r, byte value) {
      registers[r] = value;
      UpdateFlags(value);
   }

   /// <summary> Set relevant flags from value in memory. </summary>
   /// <param name="value"></param>
   public void UpdateFlags(byte value) {
      SetFlag(StatusFlag.Negative, (value & 0b_1000_0000) > 0);
      SetFlag(StatusFlag.Zero, value == 0);
   }

   public bool HasFlag(StatusFlag flag) =>
      flag.IsSet(P);

   public void SetFlag(StatusFlag flag) =>
      P |= flag;

   public void SetFlag(StatusFlag flag, bool value) {
      if (value)
         SetFlag(flag);
      else
         ClearFlag(flag);
   }

   public void ClearFlag(StatusFlag flag) =>
      P &= ~flag;

   public CpuRegisterLog GetLog() =>
      new(this);
}
