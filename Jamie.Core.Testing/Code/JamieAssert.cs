using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jamie.Core.Testing;

public static class JamieAssert {
   public static void FileExists(
      string path
   ) {
      var exists = File.Exists(path);

      Assert.IsTrue(exists, $"Path '{path}' does not exist; absolute path: '{Path.GetFullPath(path)}'.");
   }
}
