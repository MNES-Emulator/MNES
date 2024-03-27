using static Mnes.Core.Machine.NesTimer;

namespace Mnes.Core.Saves.Configuration;

public class SystemConfig
{
    public RegionType Region { get; set; } = RegionType.NTSC;

    public bool DebugMode { get; set; }
}
