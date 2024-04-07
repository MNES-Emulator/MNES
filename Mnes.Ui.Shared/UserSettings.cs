using Mnes.Core.Saves.Configuration;

namespace Mnes.Ui.Shared;

public sealed class UserSettings {
   public MnesConfig Mnes { get; set; } = new();
}
