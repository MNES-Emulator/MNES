using Emu.Core;

namespace Mnes.Core.Machine.Input;

public sealed class NesInputState : InputState {
   public NesControllerState C1 { get; } = new();
   public NesControllerState C2 { get; } = new();
}
