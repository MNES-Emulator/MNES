using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNES.Core.Machine.CPU
{
    public class CpuInstruction
    {
        /// <summary> Iterates through each clock cycle of a CPU instruction. </summary>
        /// <returns>true if completed</returns>
        public delegate void ProcessDelegate(MachineState state);

        public string Name { get; init; }
        public byte OpCode { get; init; }
        public int Bytes { get; init; }
        public bool Illegal { get; init; }
        public ProcessDelegate[] Process { get; init; }
        /// <summary> True if fully implemented. </summary>
        public bool Unfinished { get; init; } = false;
    }
}
