using Mnes.Core.Machine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNES.Core.Machine.Logging
{
    public class MachineLogger
    {
        MachineState machine;

        public List<InstructionLog> CpuLog { get; init; } = new();

        public MachineLogger(MachineState machine)
        {
            this.machine = machine;
        }

        public void Log(InstructionLog log)
        {
            CpuLog.Add(log);
            Debug.WriteLine(CpuLog.Count.ToString().PadLeft(4) + " " + log);
        }
    }
}
