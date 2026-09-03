using System.IO;

namespace PSProxmoxVE.Core.Tests
{
    public static class TestHelper
    {
        public static string LoadFixture(string filename)
        {
            var path = Path.Combine("Fixtures", filename);
            return File.ReadAllText(path);
        }
    }
}
