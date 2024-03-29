using static Mnes.Core.Machine.CPU.CpuRegisters;

namespace Mnes.Tests.Machine.CPU;

[TestClass, TestCategory("Unit")]
public sealed class CpuRegisters_RegisterTypeTests {
   // not a lot to test atm

   [TestMethod]
   public void Test_DefaultInstance() {
      var result = new RegisterType();

      Assert.AreEqual(RegisterType.A, result);
   }

   [TestMethod]
   public void Test_Equals_True() {
      // ReSharper disable once EqualExpressionComparison
      var result = RegisterType.Y == RegisterType.Y;

      Assert.IsTrue(result);
   }

   [TestMethod]
   public void Test_Equals_False() {
      var result = RegisterType.Y == RegisterType.P;

      Assert.IsFalse(result);
   }

   [TestMethod]
   public void Test_NotEquals_True() {
      var result = RegisterType.P != RegisterType.A;

      Assert.IsTrue(result);
   }

   [TestMethod]
   public void Test_NotEquals_False() {
      // ReSharper disable once EqualExpressionComparison
      var result = RegisterType.S != RegisterType.S;

      Assert.IsFalse(result);
   }

   [TestMethod]
   public void Test_ImplicitOperatorInt() {
      int result = RegisterType.Y;

      Assert.AreEqual(2, result);
   }
}
