using Jamie.Core.Testing;
using Mnes.Core.Machine;
using Mnes.Core.Machine.Input;
using Mnes.Core.Saves.Configuration;

namespace Mnes.Tests.Machine;

[TestClass, TestCategory("Integration")]
public sealed class TestRoms {
   const string nestest_path = "../../../../Resources/Test Roms/other/nestest.nes";

   [TestMethod]
   public void Test_RomPathExists() =>
      JamieAssert.FileExists(nestest_path);

   // TODO: misbehaved test? out of memory exception in StringBuilder.ToString()?
   [TestMethod, Ignore]
   public async Task Nestest() {
      var input = new NesInputState();
      var settings = new MnesConfig();
      settings.System.DebugMode = true;
      var rom_data = await File.ReadAllBytesAsync(nestest_path);
      var machine = new MachineState(rom_data, settings, input);
      await machine.Run();
   }
}
