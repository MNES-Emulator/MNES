using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mnes.Core.Machine.Input;

public class InputState
{
    public ControllerState C1 { get; } = new();
    public ControllerState C2 { get; } = new();
    public HotkeyState Hotkeys { get; } = new();
}
