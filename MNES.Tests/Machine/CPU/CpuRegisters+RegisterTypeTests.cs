using static Mnes.Core.Machine.CPU.CpuRegisters;

namespace Mnes.Tests.Machine.CPU;

[TestClass, TestCategory("Unit")]
public sealed class CpuRegisters_RegisterTypeTests {
   // not a lot to test atm

   [TestMethod]
   public void Test_ImplicitOperatorInt() {
      int result = RegisterType.Y;

      Assert.AreEqual(2, result);
   }
}
