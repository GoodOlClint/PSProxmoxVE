using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace PSProxmoxVE.Core.Tests
{
    public class PackageVersionCentralizationTests
    {
        private static readonly string[] ProjectPaths =
        {
            "src/PSProxmoxVE.Core/PSProxmoxVE.Core.csproj",
            "src/PSProxmoxVE/PSProxmoxVE.csproj",
            "tests/PSProxmoxVE.Core.Tests/PSProxmoxVE.Core.Tests.csproj",
        };

        [Fact]
        public void PackageReferences_DoNotPinVersionsPerProject()
        {
            var repoRoot = FindRepoRoot();

            foreach (var relativePath in ProjectPaths)
            {
                var doc = XDocument.Load(Path.Combine(repoRoot, relativePath));
                var pinned = doc.Descendants("PackageReference")
                    .Where(e => e.Attribute("Version") != null)
                    .Select(e => e.Attribute("Include")?.Value)
                    .ToList();

                Assert.True(pinned.Count == 0,
                    $"{relativePath} pins a version directly instead of going through Directory.Packages.props: {string.Join(", ", pinned)}");
            }
        }

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "PSProxmoxVE.sln")))
            {
                dir = dir.Parent;
            }

            if (dir == null)
            {
                throw new InvalidOperationException(
                    "Could not locate repository root (PSProxmoxVE.sln) from " + AppContext.BaseDirectory);
            }

            return dir.FullName;
        }
    }
}
