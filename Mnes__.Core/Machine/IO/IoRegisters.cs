namespace Mnes.Core.Machine.IO;

public sealed class IoRegisters {
   readonly MachineState _machine;
   readonly byte[] _registers = new byte[0x20];

   public byte SQ1_VOL { get => _registers[0]; set => _registers[0] = value; }
   public byte SQ1_SWEEP { get => _registers[1]; set => _registers[1] = value; }
   public byte SQ1_LO { get => _registers[2]; set => _registers[2] = value; }
   public byte SQ1_HI { get => _registers[3]; set => _registers[3] = value; }
   public byte SQ2_VOL { get => _registers[4]; set => _registers[4] = value; }
   public byte SQ2_SWEEP { get => _registers[5]; set => _registers[5] = value; }
   public byte SQ2_LO { get => _registers[6]; set => _registers[6] = value; }
   public byte SQ2_HI { get => _registers[7]; set => _registers[7] = value; }
   public byte TRI_LINEAR { get => _registers[8]; set => _registers[8] = value; }
   byte unused { get => _registers[9]; set => _registers[9] = value; }
   public byte TRI_LO { get => _registers[10]; set => _registers[10] = value; }
   public byte TRI_HI { get => _registers[11]; set => _registers[11] = value; }
   public byte NOISE_VOL { get => _registers[12]; set => _registers[12] = value; }
   byte unused2 { get => _registers[13]; set => _registers[13] = value; }
   public byte NOISE_LO { get => _registers[14]; set => _registers[14] = value; }
   public byte NOISE_HI { get => _registers[15]; set => _registers[15] = value; }
   public byte DMC_FREQ { get => _registers[16]; set => _registers[16] = value; }
   public byte DMC_RAW { get => _registers[17]; set => _registers[17] = value; }
   public byte DMC_START { get => _registers[18]; set => _registers[18] = value; }
   public byte DMC_LEN { get => _registers[19]; set => _registers[19] = value; }
   public byte OAMDMA { get => _registers[20]; set => _registers[20] = value; }
   public byte SND_CHN { get => _registers[21]; set => _registers[21] = value; }
   public byte JOY1 { get => _registers[22]; set => _registers[22] = value; }
   public byte JOY2 { get => _registers[23]; set => _registers[23] = value; }

   // $4000-$4014 are write only
   public byte? this[int index] {
      get => index < 0x15 ? null : _registers[index];
      set => _registers[index] = value.Value; // TODO: throws on null
   }

   public IoRegisters(MachineState m) =>
      _machine = m;
}
