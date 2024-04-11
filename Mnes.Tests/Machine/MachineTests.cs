using Mnes.Core.Machine;
using Mnes.Core.Machine.Input;
using Mnes.Core.Saves.Configuration;

namespace Mnes.Tests.Machine;

[TestClass, TestCategory("Unit")]
public sealed class MachineTests {
   const string rom_filename = "Resources/Test Roms/other/nestest.nes";

   [TestMethod]
   public async Task TestRun() {
      var input = new NesInputState();
      var settings = new MnesConfig();
      settings.System.DebugMode = true;
      var rom_data = await File.ReadAllBytesAsync(rom_filename);
      var machine = new MachineState(rom_data, settings, input);
      await machine.Run();
   }
}
