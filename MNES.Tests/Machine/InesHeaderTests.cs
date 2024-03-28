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
      Assert.IsFalse(header.NameTableArrangment);
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
      var bytes = MakeBlankValidHeader();

      bytes[4] = 3;

      var header = new InesHeader(bytes);

      Assert.AreEqual(49_152, header.PrgRomSize);
   }
}
