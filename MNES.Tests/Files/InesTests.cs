using Mnes.Core.Machine;

namespace Mnes.Tests.Files;

[TestClass, TestCategory("Unit")]
public sealed class InesTests {
   const string rom_file = "Resources/Test Roms/other/nestest.nes";

   [TestMethod]
   public void TestHeader() {
      var nes_bytes = File.ReadAllBytes(rom_file);
      InesHeader _ = new(nes_bytes);
   }
}
