using AutoFixture;
using Mnes.Core.Machine.CPU;
using RegisterType = Mnes.Core.Machine.CPU.CpuRegisters.RegisterType;

namespace Mnes.Tests.Machine.CPU;

[TestClass, TestCategory("Unit")]
public sealed class CpuRegistersTests {
   readonly IFixture F = new Fixture();

   public CpuRegistersTests() {
      // TODO: push somewhere reusable (make values arbitrary; default is in order)
      var values = Enum.GetValues<RegisterType>();
      F.Register(() => values[F.Create<int>() % values.Length]);
   }

   [TestMethod]
   public void Test_Indexer() {
      var reg = F.Create<RegisterType>();
      var value = F.Create<byte>();

      var cregs = new CpuRegisters();
      cregs[reg] = value;

      Assert.AreEqual(value, cregs[reg]);
   }

   [TestMethod]
   public void Test_A_Value() {
      var value = F.Create<byte>();

      var cregs = new CpuRegisters();
      cregs.A = value;

      Assert.AreEqual(value, cregs.A);
   }

   [TestMethod]
   public void Test_X_Value() {
      var value = F.Create<byte>();

      var cregs = new CpuRegisters();
      cregs.X = value;

      Assert.AreEqual(value, cregs.X);
   }

   [TestMethod]
   public void Test_Y_Value() {
      var value = F.Create<byte>();

      var cregs = new CpuRegisters();
      cregs.Y = value;

      Assert.AreEqual(value, cregs.Y);
   }


   [TestMethod]
   public void Test_S_Value() {
      var value = F.Create<byte>();

      var cregs = new CpuRegisters();
      cregs.S = value;

      Assert.AreEqual(value, cregs.S);
   }


   [TestMethod]
   public void Test_P_Value() {
      var value = F.Create<byte>();

      var cregs = new CpuRegisters();
      cregs.P = value;

      Assert.AreEqual(value | 0b0010_0000, cregs.P);
   }

   [TestMethod]
   public void Test_GetSetRegister_Value() {
      var reg = F.Create<RegisterType>();
      var value = F.Create<byte>();

      var cregs = new CpuRegisters();
      cregs.SetRegister(reg, value);
      var result = cregs.GetRegister(reg);

      var expected = reg == RegisterType.P
         ? value | 0b0010_0000
         : value;

      Assert.AreEqual(expected, result);
   }
}
