using Mnes.Core.Saves.Configuration;

namespace Mnes.Tests.Files;

[TestClass]
public class ConfigTests
{
    [TestMethod]
    public void TestInitialization()
    {
        Config.InitializeFromDisk();
    }
}
