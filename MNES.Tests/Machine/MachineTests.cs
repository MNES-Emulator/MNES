using MNES.Core.Machine;
using MNES.Core.Machine.Input;
using MNES.Core.Saves.Configuration;
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
        string rom_file = "Resources/Test Roms/other/nestest.nes";

        [TestMethod]
        public async Task TestRun()
        {
            var input = new InputState();
            var settings = new ConfigSettings();
            settings.System.DebugMode = true;
            var machine = new MachineState(rom_file, settings, input);
            await machine.Run();
        }
    }
}
