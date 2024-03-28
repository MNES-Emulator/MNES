namespace Mnes.Core.Machine.IO;

public class IoRegisters
{
   readonly MachineState machine;
   readonly byte[] registers = new byte[0x20];

   public byte SQ1_VOL { get => registers[0]; set => registers[0] = value; }
   public byte SQ1_SWEEP { get => registers[1]; set => registers[1] = value; }
   public byte SQ1_LO { get => registers[2]; set => registers[2] = value; }
   public byte SQ1_HI { get => registers[3]; set => registers[3] = value; }
   public byte SQ2_VOL { get => registers[4]; set => registers[4] = value; }
   public byte SQ2_SWEEP { get => registers[5]; set => registers[5] = value; }
   public byte SQ2_LO { get => registers[6]; set => registers[6] = value; }
   public byte SQ2_HI { get => registers[7]; set => registers[7] = value; }
   public byte TRI_LINEAR { get => registers[8]; set => registers[8] = value; }
   byte unused { get => registers[9]; set => registers[9] = value; }
   public byte TRI_LO { get => registers[10]; set => registers[10] = value; }
   public byte TRI_HI { get => registers[11]; set => registers[11] = value; }
   public byte NOISE_VOL { get => registers[12]; set => registers[12] = value; }
   byte unused2 { get => registers[13]; set => registers[13] = value; }
   public byte NOISE_LO { get => registers[14]; set => registers[14] = value; }
   public byte NOISE_HI { get => registers[15]; set => registers[15] = value; }
   public byte DMC_FREQ { get => registers[16]; set => registers[16] = value; }
   public byte DMC_RAW { get => registers[17]; set => registers[17] = value; }
   public byte DMC_START { get => registers[18]; set => registers[18] = value; }
   public byte DMC_LEN { get => registers[19]; set => registers[19] = value; }
   public byte OAMDMA { get => registers[20]; set => registers[20] = value; }
   public byte SND_CHN { get => registers[21]; set => registers[21] = value; }
   public byte JOY1 { get => registers[22]; set => registers[22] = value; }
   public byte JOY2 { get => registers[23]; set => registers[23] = value; }

   // $4000-$4014 are write only
   public byte? this[int index] { 
      get => registers[index]; 
      set => registers[index] = value.Value;
   }

   public IoRegisters(MachineState m) =>
       machine = m;
}
