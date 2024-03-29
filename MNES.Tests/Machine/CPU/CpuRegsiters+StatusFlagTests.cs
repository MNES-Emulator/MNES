using static Mnes.Core.Machine.CPU.CpuRegisters;

namespace Mnes.Tests.Machine.CPU;

[TestClass, TestCategory("Unit")]
public sealed class CpuRegsiters_StatusFlagTypeTests {
   [TestMethod]
   public void Test_BitwiseOr_ByteFlag() {
      var result = 0b1010_0000 | StatusFlag.InterruptDisable;

      Assert.AreEqual(0b1010_0100, result);
   }

   [TestMethod]
   public void Test_BitwiseOr_FlagByte() {
      var result = StatusFlag.Overflow | 0b1000_0101;

      Assert.AreEqual(0b1100_0101, result);
   }

   [TestMethod]
   public void Test_BitwiseOr_FlagFlag() {
      var result = StatusFlag.Zero | StatusFlag.BFlag;

      Assert.AreEqual(0b0001_0010, result);
   }

   [TestMethod]
   public void Test_BitwiseAnd_ByteFlag() {
      var result = 0b1010_1111 & StatusFlag.InterruptDisable;

      Assert.AreEqual(StatusFlag.InterruptDisable.Bits, result);
   }

   [TestMethod]
   public void Test_BitwiseAnd_FlagByte() {
      var result = StatusFlag.Overflow & 0b1111_0101;

      Assert.AreEqual(StatusFlag.Overflow.Bits, result);
   }

   [TestMethod]
   public void Test_BitwiseNot() {
      var result = ~StatusFlag.BFlag;

      Assert.AreEqual(0b1110_1111, result);
   }

   [TestMethod]
   public void Test_IsSet_True() {
      const byte value = 0b1010_1010;

      var result = StatusFlag.Decimal.IsSet(value);

      Assert.IsTrue(result);
   }

   [TestMethod]
   public void Test_IsSet_False() {
      const byte value = 0b0101_0101;

      var result = StatusFlag.Decimal.IsSet(value);

      Assert.IsFalse(result);
   }
}
