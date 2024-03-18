using MNES.Core;
using MNES.Core.Machine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNES.Tests.Files
{
    [TestClass]
    public class INesTests
    {
        [TestMethod]
        public void TestHeader()
        {
            var rom_file = "Resources/Test Roms/other/nestest.nes";

            var nes_bytes = File.ReadAllBytes(rom_file);
            INesHeader header = new(nes_bytes);
        }
    }
}
