using MNES.Core.Machine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNES.Core
{
    public class State
    {
        MachineState Machine;
        NesTimer Timer;

        public State(string rom_path)
        {
            Machine = new MachineState(rom_path);
        }

        public void Run()
        {

        }
    }
}
