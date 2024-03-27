using Mnes.Core.Machine;

namespace Mnes.Tests.Files;

[TestClass]
public sealed class INesTests
{
   [TestMethod]
   public void TestHeader() {
      var rom_file = "Resources/Test Roms/other/nestest.nes";

      var nes_bytes = File.ReadAllBytes(rom_file);
      INesHeader header = new(nes_bytes);
   }
}
