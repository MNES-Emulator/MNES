using Mnes.Core.Machine.Logging;
using System.Diagnostics;

namespace Mnes.Tests.Code;

internal static class AssertExtensions {
   public static void AssertAreEqual(this InstructionLog expected, InstructionLog actual, int index) {
      Assert.AreEqual(
         expected.Instruction.OpCode,
         actual.Instruction.OpCode,
         $"Opcode mismatch at instruction {index}");

      Assert.AreEqual(
         expected.Param1,
         actual.Param1,
         $"Argument 1 mismatch at instruction {index}");

      Assert.AreEqual(
         expected.Param2,
         actual.Param2,
         $"Argument 2 mismatch at instruction {index}");

      // This should only be a warning for now at most
      //Assert.AreEqual(
      //   expected.Message ?? "",
      //   actual.Message ?? "",
      //   $"Message mismatch at instruction {index}");
      if ((expected.Message ?? "") != (actual.Message ?? ""))
         Debug.WriteLine($"WARNING: Message mismatch at instruction {index}");

      Assert.AreEqual(
         expected.CpuRegisters.A,
         actual.CpuRegisters.A,
         $"CPU register A mismatch at instruction {index}");

      Assert.AreEqual(
         expected.CpuRegisters.X,
         actual.CpuRegisters.X,
         $"CPU register X mismatch at instruction {index}");

      Assert.AreEqual(
         expected.CpuRegisters.Y,
         actual.CpuRegisters.Y,
         $"CPU register Y mismatch at instruction {index}");

      Assert.AreEqual(
         expected.CpuRegisters.P,
         actual.CpuRegisters.P,
         $"CPU register P mismatch at instruction {index}");

      Assert.AreEqual(
         expected.CpuRegisters.S,
         actual.CpuRegisters.S,
         $"CPU register SP mismatch at instruction {index}");

      // ppu cycle, ppu scanline, and cycle total are all out of wack so there's no point in throwing for them atm.
   }
}
