using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jamie.Core.Tests;

[TestClass, TestCategory("Demonstrative")]
public sealed class TestLab {
   [TestMethod]
   public void Test_DoesFileExistsWorkWithNullAndEmptyString_Yes() {
      Assert.IsFalse(File.Exists(null));
      Assert.IsFalse(File.Exists(""));
      Assert.IsFalse(File.Exists("    \r\n    "));
   }
}
