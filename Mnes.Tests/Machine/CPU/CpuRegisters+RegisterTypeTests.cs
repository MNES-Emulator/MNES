using static Mnes.Core.Machine.CPU.CpuRegisters;

namespace Mnes.Tests.Machine.CPU;

[TestClass, TestCategory("Unit")]
public sealed class CpuRegisters_RegisterTypeTests {
   // not a lot to test atm

   [TestMethod]
   public void Test_Equals_True() {
      // ReSharper disable once EqualExpressionComparison
#pragma warning disable CS1718 // Comparison made to same variable
      var result = RegisterType.Y == RegisterType.Y;
#pragma warning restore CS1718 // Comparison made to same variable

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
#pragma warning disable CS1718 // Comparison made to same variable
      var result = RegisterType.S != RegisterType.S;
#pragma warning restore CS1718 // Comparison made to same variable

      Assert.IsFalse(result);
   }

   [TestMethod]
   public void Test_ImplicitOperatorInt() {
      int result = RegisterType.Y;

      Assert.AreEqual(2, result);
   }
}
