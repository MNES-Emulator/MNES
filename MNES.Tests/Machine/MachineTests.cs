using MNES.Core.Machine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNES.Tests.Machine
{
    [TestClass]
    public class MachineTests
    {
        string rom_file = "Resources/Test Roms/instr_test-v3/official_only.nes";

        [TestMethod]
        public void TestCreation()
        {
            var machine = new MachineState(rom_file);
        }
    }
}
