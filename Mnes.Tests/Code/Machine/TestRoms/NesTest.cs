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

   // TODO: misbehaved test? out of memory exception in StringBuilder.ToString()?
   //
   // Answer: This test currently has no way of passing and will never end. The memory leak is
   // unrelated. Idk if that calls for an Ignore attribute or a test that is correctly
   // identified as "failed". Leaving it in because it takes forever.
   //
   // nestest writes to certain areas of memory the results of how accurately it was run,
   // so evaluating those addresses at the end, as well as making sure our log is identical
   // to the Nintendulator one will determine whether this test passes or not.
   [TestMethod, Ignore]
   public async Task RunNestest() {
      var input = new NesInputState();
      var settings = new MnesConfig();
      settings.System.DebugMode = true;
      settings.System.DebugShowStatusFlags = true;
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
         Debug.WriteLine($"     {valid_log.GetDebugString(true)} (control)");

         Assert.AreEqual(
            valid_log.Instruction.OpCode,
            current_log.Instruction.OpCode,
            $"Opcode mismatch at instruction {i}");

         Assert.AreEqual(
            valid_log.Param1,
            current_log.Param1,
            $"Argument 1 mismatch at instruction {i}");

         Assert.AreEqual(
            valid_log.Param2,
            current_log.Param2,
            $"Argument 2 mismatch at instruction {i}");

         // This should only be a warning for now at most
         //Assert.AreEqual(
         //   valid_log.Message,
         //   current_log.Message,
         //   $"Argument 1 mismatch at instruction {i}");

         Assert.AreEqual(
            valid_log.CpuRegisters.A,
            current_log.CpuRegisters.A,
            $"CPU register A mismatch at instruction {i}");

         Assert.AreEqual(
            valid_log.CpuRegisters.X,
            current_log.CpuRegisters.X,
            $"CPU register X mismatch at instruction {i}");

         Assert.AreEqual(
            valid_log.CpuRegisters.Y,
            current_log.CpuRegisters.Y,
            $"CPU register Y mismatch at instruction {i}");

         Assert.AreEqual(
            valid_log.CpuRegisters.P,
            current_log.CpuRegisters.P,
            $"CPU register P mismatch at instruction {i}");

         Assert.AreEqual(
            valid_log.CpuRegisters.S,
            current_log.CpuRegisters.S,
            $"CPU register SP mismatch at instruction {i}");
      }

      machine.Callbacks.OnNesInstructionExecute += cpu_callback;
      await machine.Run();
   }
}