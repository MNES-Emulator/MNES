namespace Emu.Core;

public abstract class InputState {
   public HotkeyState Hotkeys{ get; } = new();
}
