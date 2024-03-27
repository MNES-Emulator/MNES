using static Mnes.Core.Machine.NesTimer;

namespace Mnes.Core.Saves.Configuration;

public sealed class SystemConfig {
    public RegionType Region { get; set; } = RegionType.NTSC;

    public bool DebugMode { get; set; }
}
