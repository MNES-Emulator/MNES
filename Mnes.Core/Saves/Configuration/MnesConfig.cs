using Emu.Core;

namespace Mnes.Core.Saves.Configuration;

public sealed class MnesConfig : EmulatorConfig {
   public SystemConfig System { get; set; } = new();
}
