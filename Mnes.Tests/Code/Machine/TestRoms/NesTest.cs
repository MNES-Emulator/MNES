﻿using Jamie.Core.Testing;
using Mnes.Core.Machine;
using Mnes.Core.Machine.CPU;
using Mnes.Core.Machine.Input;
using Mnes.Core.Machine.Logging;
using Mnes.Core.Saves.Configuration;
using System.Diagnostics;

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

   // nestest writes to certain areas of memory the results of how accurately it was run,
   // so evaluating those addresses at the end, as well as making sure our log is identical
   // to the Nintendulator one will determine whether this test passes or not.
   //
   // The test "ends" after reaching a specific address. After that it will start checking
   // to see if illegal opcodes are working, and none of those are implemented atm.
   [TestMethod]
   public async Task RunNestest() {
      var rom_data = File.ReadAllBytes(NESTEST_PATH);
      var valid_logs = File.ReadAllLines(NESTEST_LOG_PATH)
         .Select(x => InstructionLog.FromString(x))
         .ToArray();

      var input = new NesInputState();
      var settings = new MnesConfig();
      settings.System.DebugMode = true;
      settings.System.DebugShowStatusFlags = true;
      settings.System.StartExecutionAtAddress = 0xC000; // test starts here
      var machine = new MachineState(rom_data, settings, input);

      int i = 0;
      void cpu_callback() {
         var valid_log = valid_logs[i++];
         var current_log = machine.Logger.GetLast();
         Debug.WriteLine($"     {valid_log.GetDebugString(true)} (control)");
         valid_log.AssertAreEqual(current_log, i);
         Debug.WriteLine("");
         if (machine.Cpu.Registers.PC == 0xC6BC) // test ends here
            _ = machine.Stop();
      }

      machine.Callbacks.OnCpuExecute += cpu_callback;
      await machine.Run();
      // Todo: Check addresses that nestest writes results to.
   }
}
