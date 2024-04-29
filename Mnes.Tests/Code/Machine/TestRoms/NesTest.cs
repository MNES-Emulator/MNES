using Jamie.Core.Testing;
using Mnes.Core.Machine;
using Mnes.Core.Machine.CPU;
using Mnes.Core.Machine.Input;
using Mnes.Core.Machine.Logging;
using Mnes.Core.Saves.Configuration;

namespace Mnes.Tests.Code.Machine.TestRoms;

[TestClass, TestCategory("Integration")]
public sealed class NesTest {
   const string NESTEST_PATH = "../../../../Resources/Test Roms/other/nestest.nes";
   const string NESTEST_LOG_PATH = "../../../../Resources/Test Roms/other/nestest_log.txt";

   [TestMethod]
   public void Test_RomPathExists() =>
      JamieAssert.FileExists(NESTEST_PATH);

   [TestMethod]
   public void Test_LogPathExists() =>
   JamieAssert.FileExists(NESTEST_LOG_PATH);

   // TODO: misbehaved test? out of memory exception in StringBuilder.ToString()?
   //
   // Answer: This test currently has no way of passing and will never end. The memory leak is
   // unrelated. Idk if that calls for an Ignore attribute or a test that is correctly
   // identified as "failed". Leaving it in because it takes forever.
   [TestMethod/*, Ignore*/]
   public async Task RunNestest() {
      var input = new NesInputState();
      var settings = new MnesConfig();
      settings.System.DebugMode = true;
      // nestest has a custom entry point that will go through all the CPU instructions.
      // the log starts there, so we will too.
      settings.System.StartExecutionAtAddress = 0xC000;
      var rom_data = File.ReadAllBytes(NESTEST_PATH);
      var valid_logs = File.ReadAllLines(NESTEST_LOG_PATH)
         .Select(x => InstructionLog.FromString(x))
         .ToArray();
      var machine = new MachineState(rom_data, settings, input);

      int i = 0;
      void cpu_callback(CpuInstruction instruction) {
         var valid_log = valid_logs[i++];
         var current_log = machine.Logger.GetLast();

         Assert.AreEqual(
            valid_log.Instruction.OpCode, 
            current_log.Instruction.OpCode, 
            $"Opcode mismatch at instruction {i}");
      }

      machine.Callbacks.OnNesInstructionExecute += cpu_callback;
      await machine.Run();
   }
}
