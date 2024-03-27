using Mnes.Core.Machine;
using System.Diagnostics;

namespace MNES.Core.Machine.Logging;

public sealed class MachineLogger {
    MachineState machine;

    public List<InstructionLog> CpuLog { get; init; } = new();

    public MachineLogger(MachineState machine) =>
        this.machine = machine;

    public void Log(InstructionLog log) {
        CpuLog.Add(log);
        Debug.WriteLine(CpuLog.Count.ToString().PadLeft(4) + " " + log);
    }
}
