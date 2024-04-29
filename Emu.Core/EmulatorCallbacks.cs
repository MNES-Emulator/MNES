namespace Emu.Core;

public abstract class EmulatorCallbacks {
   public Action? OnCpuExecute { get; set; }
}