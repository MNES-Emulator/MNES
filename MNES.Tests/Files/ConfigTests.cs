using Mnes.Core.Saves.Configuration;

namespace Mnes.Tests.Files;

[TestClass, TestCategory("Integration")]
public sealed class ConfigTests {
   [TestMethod]
   public void TestInitialization() =>
      Config.InitializeFromDisk();
}
