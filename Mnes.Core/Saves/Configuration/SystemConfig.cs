using static Mnes.Core.Machine.NesTimer;

namespace Mnes.Core.Saves.Configuration;

public sealed class SystemConfig {
   public RegionType Region { get; set; } = RegionType.NTSC;

   public bool DebugMode { get; set; }

   /// <summary>
   /// Only used if <see cref="DebugMode"/> is enabled. 
   /// Outputs the status register (P) as a binary string 
   /// as well as the hex value.
   /// </summary>
   public bool DebugShowStatusFlags { get; set; }

   /// <summary>
   /// Overrides the default behavior to set the PC flag
   /// to the specified address and start execution from
   /// there.
   /// </summary>
   public ushort? StartExecutionAtAddress { get; set; }
}
