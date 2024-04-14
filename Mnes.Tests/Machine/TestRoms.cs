using Mnes.Core.Machine;
using Mnes.Core.Machine.Input;
using Mnes.Core.Saves.Configuration;

namespace Mnes.Tests.Machine;

[TestClass, TestCategory("Unit")]
public sealed class TestRoms {
   const string nestest_path = "../../../../Resources/Test Roms/other/nestest.nes";

   [TestMethod]
   public async Task Nestest() {
      var input = new NesInputState();
      var settings = new MnesConfig();
      settings.System.DebugMode = true;
      var rom_data = await File.ReadAllBytesAsync(nestest_path);
      var machine = new MachineState(rom_data, settings, input);
      await machine.Run();
   }
}
