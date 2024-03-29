using AutoFixture;
using Mnes.Core.Machine.CPU;
using Mnes.Tests.Testing;
using static Mnes.Core.Machine.CPU.CpuRegisters;

namespace Mnes.Tests.Machine.CPU;

[TestClass, TestCategory("Unit")]
public sealed class CpuRegistersTests {
   readonly IFixture F = new Fixture().MnesFixes();

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

   [TestMethod]
   public void Test_HasFlag_Defaults() {
      var cregs = new CpuRegisters();

      foreach (var flag in StatusFlag.Values) {
         if (flag == StatusFlag._1)
            Assert.IsTrue(cregs.HasFlag(flag));
         else
            Assert.IsFalse(cregs.HasFlag(flag));
      }
   }

   [TestMethod]
   public void Test_SetFlag_HasFlag() {
      var cregs = new CpuRegisters();
      var flag = F.Create<StatusFlag>();

      cregs.SetFlag(flag);

      Assert.IsTrue(cregs.HasFlag(flag));
   }

   [TestMethod]
   public void Test_SetFlag_Bool_False() {
      var cregs = new CpuRegisters();
      var flag = F.Create<StatusFlag>();

      cregs.SetFlag(flag, false);

      if (flag == StatusFlag._1)
         Assert.IsTrue(cregs.HasFlag(flag));
      else
         Assert.IsFalse(cregs.HasFlag(flag));
   }

   [TestMethod]
   public void Test_SetFlag_Bool_True() {
      var cregs = new CpuRegisters();
      var flag = F.Create<StatusFlag>();

      cregs.SetFlag(flag, true);

      Assert.IsTrue(cregs.HasFlag(flag));
   }

   [TestMethod]
   public void Test_ClearFlag_HasFlag() {
      var cregs = new CpuRegisters();
      var flag = F.Create<StatusFlag>();
      cregs.SetFlag(flag);

      cregs.ClearFlag(flag);

      // 1 flag cannot be unset
      if (flag != StatusFlag._1)
         Assert.IsFalse(cregs.HasFlag(flag));
      else
         Assert.IsTrue(cregs.HasFlag(flag));
   }

   [TestMethod]
   public void Test_SetRegister_SetsFlagNegative_WhenSetsFlagsAndNegative() {
      var cregs = new CpuRegisters();
      var reg = RegisterType.Y;
      Assert.IsTrue(reg.SetsFlags);

      cregs.SetRegister(reg, 0b1000_1010);

      Assert.IsTrue(cregs.HasFlag(StatusFlag.Negative));
   }

   [TestMethod]
   public void Test_SetRegister_DoesntSetFlagNegative_WhenNotNegative() {
      var cregs = new CpuRegisters();
      var reg = RegisterType.Y;
      Assert.IsTrue(reg.SetsFlags);

      cregs.SetRegister(reg, 0b0000_1010);

      Assert.IsFalse(cregs.HasFlag(StatusFlag.Negative));
   }

   [TestMethod]
   public void Test_SetRegister_DoesntSetFlagNegative_WhenDoesntSetFlags() {
      var cregs = new CpuRegisters();
      var reg = RegisterType.S;
      Assert.IsFalse(reg.SetsFlags);

      cregs.SetRegister(reg, 0b1010_0101);

      Assert.IsFalse(cregs.HasFlag(StatusFlag.Negative));
   }
}
