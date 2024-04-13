namespace Mnes.Core.Machine;

/// <summary> General use register that will trigger additional behavior defined by the inheriting class. </summary>
public abstract class Register {
   protected readonly MachineState Machine;
   protected byte Value; // may not be used

   public Register(MachineState m) =>
      Machine = m;

   public virtual byte CpuRead() => Value;
   public virtual void CpuWrite(byte value) => Value = value;
   public virtual void SetPowerUpState() => Value = 0;
}
