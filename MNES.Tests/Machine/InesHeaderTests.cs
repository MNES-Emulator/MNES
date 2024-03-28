using Mnes.Core.Machine;

namespace Mnes.Tests.Machine;

[TestClass]
public sealed class InesHeaderTests {
   [TestMethod]
   public void Test_EmptyByteArrayThrows() =>
      Assert.ThrowsException<Exception>(() =>
         new InesHeader(Array.Empty<byte>())
      );

   [TestMethod]
   public void Test_CorrectSizeArrayWithoutMagicValueThrows() =>
      Assert.ThrowsException<Exception>(() =>
         new InesHeader(new byte[InesHeader.header_length])
      );

   static byte[] MakeBlankValidHeader() {
      var bytes = new byte[InesHeader.header_length];
      InesHeader.ines_text.CopyTo(bytes, 0);
      return bytes;
   }

   static byte[] MakeBlankValidHeader(
      int index,
      byte value
   ) {
      var bytes = MakeBlankValidHeader();
      bytes[index] = value;
      return bytes;
   }

   [TestMethod]
   public void Test_CorrectSizeArrayWithMagicValueDoesntThrow() {
      var bytes = MakeBlankValidHeader();

      _ = new InesHeader(bytes);
   }

   [TestMethod]
   public void Test_ZeroArrayResultingValues() {
      var bytes = MakeBlankValidHeader();

      var header = new InesHeader(bytes);

      Assert.AreEqual(0, header.PrgRomSize);
      Assert.AreEqual(0, header.ChrRomSize);
      Assert.IsFalse(header.NameTableArrangement);
      Assert.IsFalse(header.HasBatteryBackedPrgRam);
      Assert.IsFalse(header.HasTrainer);
      Assert.AreEqual(0, header.MapperNumber);
      Assert.IsFalse(header.VsUnisystem);
      Assert.IsFalse(header.PlayChoice10);
      Assert.IsFalse(header.Nes2_0);
      Assert.AreEqual(0, header.PrgRamSize);
   }

   [TestMethod]
   public void Test_CorrectPrgRomSize() {
      var bytes = MakeBlankValidHeader(4, 3);

      var header = new InesHeader(bytes);

      Assert.AreEqual(49_152, header.PrgRomSize);
   }

   [TestMethod]
   public void Test_CorrectChrRomSize() {
      var bytes = MakeBlankValidHeader(5, 4);

      var header = new InesHeader(bytes);

      Assert.AreEqual(32_768, header.ChrRomSize);
   }

   [TestMethod]
   public void Test_NameTableArrangement_False() {
      var bytes = MakeBlankValidHeader(6, 0b1111_1110);

      var header = new InesHeader(bytes);

      Assert.IsFalse(header.NameTableArrangement);
   }

   [TestMethod]
   public void Test_NameTableArrangement_True() {
      var bytes = MakeBlankValidHeader(6, 0b0000_0001);

      var header = new InesHeader(bytes);

      Assert.IsTrue(header.NameTableArrangement);
   }

   [TestMethod]
   public void Test_HasBatteryBackedPrgRam_False() {
      var bytes = MakeBlankValidHeader(6, 0b1111_1101);

      var header = new InesHeader(bytes);

      Assert.IsFalse(header.HasBatteryBackedPrgRam);
   }

   [TestMethod]
   public void Test_HasBatteryBackedPrgRam_True() {
      var bytes = MakeBlankValidHeader(6, 0b0000_0010);

      var header = new InesHeader(bytes);

      Assert.IsTrue(header.HasBatteryBackedPrgRam);
   }

   [TestMethod]
   public void Test_HasTrainer_False() {
      var bytes = MakeBlankValidHeader(6, 0b1111_1011);

      var header = new InesHeader(bytes);

      Assert.IsFalse(header.HasTrainer);
   }

   [TestMethod]
   public void Test_HasTrainer_True() {
      var bytes = MakeBlankValidHeader(6, 0b0000_0100);

      var header = new InesHeader(bytes);

      Assert.IsTrue(header.HasTrainer);
   }

   [TestMethod]
   public void Test_MapperNumber() {
      var bytes = MakeBlankValidHeader(6, 0b1001_0110);

      var header = new InesHeader(bytes);

      Assert.AreEqual(0b0000_1001, header.MapperNumber);
   }
}
