using Mnes.Core.Machine;
using Mnes.Core.Machine.Input;
using Mnes.Core.Saves.Configuration;

namespace Mnes.Tests.Machine;

[TestClass]
public sealed class MachineTests {
   const string rom_file = "Resources/Test Roms/other/nestest.nes";

   [TestMethod]
   public async Task TestRun() {
      var input = new InputState();
      var settings = new ConfigSettings();
      settings.System.DebugMode = true;
      var machine = new MachineState(rom_file, settings, input);
      await machine.Run();
   }
}
