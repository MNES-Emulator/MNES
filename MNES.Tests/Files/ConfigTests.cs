using MNES.Core.Saves.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNES.Tests.Files
{
    [TestClass]
    public class ConfigTests
    {
        [TestMethod]
        public void TestInitialization()
        {
            Config.InitializeFromDisk();
        }
    }
}
