using Jamie.Core.Testing;
using Mnes.Core.Machine;

namespace Mnes.Tests.Files;

[TestClass, TestCategory("Unit")]
public sealed class InesTests {
   const string rom_file = "../../../../Resources/Test Roms/other/nestest.nes";

   [TestMethod]
   public void Test_NesFileExists() =>
      JamieAssert.FileExists(rom_file);

   [TestMethod]
   public void Test_CanReadHeaderOfRealRomFile() {
      var nes_bytes = File.ReadAllBytes(rom_file);
      _ = new InesHeader(nes_bytes);
   }
}
