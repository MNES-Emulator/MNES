using Mnes.Core.Machine.Logging;

namespace Mnes.Core.Machine.CPU;

// https://www.nesdev.org/wiki/CPU_registers
/// <summary> Represents all the registers of the CPU. </summary>
public sealed partial class CpuRegisters {
   /// <summary> The 5 8-bit registers. </summary>
   readonly byte[] _registers = new byte[RegisterType.Values.Count];
   /// <summary> The program counter. Points to the address that the CPU pulls the next instruction from. </summary>
   public ushort PC;

   public byte this[RegisterType reg] {
      get => _registers[reg];
      set => SetRegister(reg, value);
   }

   /// <summary> The accumulator. Used for arithmetic operations. </summary>
   public byte A { get => _registers[RegisterType.A]; set => SetRegister(RegisterType.A, value); }

   /// <summary> The X register. </summary>
   public byte X { get => _registers[RegisterType.X]; set => SetRegister(RegisterType.X, value); }

   /// <summary> The Y register. </summary>
   public byte Y { get => _registers[RegisterType.Y]; set => SetRegister(RegisterType.Y, value); }

   /// <summary> The stack pointer. </summary>
   public byte S { get => _registers[RegisterType.S]; set => SetRegister(RegisterType.S, value); }

   /// <summary> The status register. </summary>
   public byte P { get => _registers[RegisterType.P]; set => SetRegister(RegisterType.P, value); }

   public CpuRegisters() {
      foreach (var reg in RegisterType.Values)
         SetRegister(reg, 0);
   }

   public byte GetRegister(RegisterType r) =>
      _registers[r];

   public void SetRegister(RegisterType r, byte value) {
      _registers[r] = value;

      if (r == RegisterType.P)
         _registers[r] |= StatusFlag._1;

      if (r.SetsFlags)
         UpdateFlags(value);
   }

   /// <summary> Set relevant flags from value in memory. </summary>
   /// <param name="value"></param>
   public void UpdateFlags(byte value) {
      SetFlag(StatusFlag.Negative, StatusFlag.Negative.IsSet(value));
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
