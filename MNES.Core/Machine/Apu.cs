namespace Mnes.Core.Machine;

public sealed class Apu {
   /// <summary> Also contains some IO registers. </summary>
   public byte[] Registers = new byte[32];

   public void SetPowerUpState() =>
      Array.Clear(Registers);
}
