using Emu.Core;
using Mnes.Core.Machine.CPU;

namespace Mnes.Core;

public sealed class MnesCallbacks : EmulatorCallbacks {
   // Todo: give everything below a base abstract class so EmulatorCallbacks can have it's own generic CpuInstruction
   /// <summary> Triggered immediately after the CPU finishes an instruction. Read the latest log to see what was executed. </summary>
   public Action<CpuInstruction>? OnNesInstructionExecute { get; set; }
}
