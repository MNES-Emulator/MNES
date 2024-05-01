namespace Emu.Core;

public abstract class EmulatorCallbacks {
   /// <summary> Triggered immediately after the CPU finishes an instruction. Read the latest log to see what was executed. </summary>
   public Action? OnCpuExecute { get; set; }
}