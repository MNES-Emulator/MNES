using Mnes.Core.Saves.Configuration;
using Mnes.Ui.Shared;

namespace Mnes.Tests.Files;

[TestClass, TestCategory("Integration")]
public sealed class ConfigTests {
   [TestMethod]
   public void TestInitialization() =>
      Config.InitializeFromDisk();
}
