using PSProxmoxVE.Core.Utilities;
using Xunit;

namespace PSProxmoxVE.Core.Tests.Utilities
{
    public class CorosyncLinksTests
    {
        [Fact]
        public void Parse_NullInput_ReturnsNullDictionaryAndNoMalformedEntries()
        {
            var (links, malformed) = CorosyncLinks.Parse(null);

            Assert.Null(links);
            Assert.Empty(malformed);
        }

        [Fact]
        public void Parse_WellFormedEntries_ReturnsTrimmedKeysAndValues()
        {
            var (links, malformed) = CorosyncLinks.Parse(new[] { "link0= 10.0.0.1 ", "link1=10.0.0.2" });

            Assert.NotNull(links);
            Assert.Equal("10.0.0.1", links!["link0"]);
            Assert.Equal("10.0.0.2", links["link1"]);
            Assert.Empty(malformed);
        }

        [Theory]
        [InlineData("link0")]
        [InlineData("link0=")]
        [InlineData("=10.0.0.1")]
        [InlineData("")]
        public void Parse_MalformedEntry_IsReportedAndOmitted(string entry)
        {
            var (links, malformed) = CorosyncLinks.Parse(new[] { entry });

            Assert.Null(links);
            Assert.Equal(new[] { entry }, malformed);
        }

        [Fact]
        public void Parse_NullEntry_IsReportedAsMalformedRatherThanThrowing()
        {
            var (links, malformed) = CorosyncLinks.Parse(new[] { "link0=10.0.0.1", null! });

            Assert.NotNull(links);
            Assert.Single(links!);
            Assert.Equal(new[] { "" }, malformed);
        }

        [Fact]
        public void Parse_ValueContainingEquals_SplitsOnlyOnTheFirstOne()
        {
            var (links, malformed) = CorosyncLinks.Parse(new[] { "link0=10.0.0.1=extra" });

            Assert.NotNull(links);
            Assert.Equal("10.0.0.1=extra", links!["link0"]);
            Assert.Empty(malformed);
        }

        [Fact]
        public void Parse_AllEntriesMalformed_ReturnsNullDictionaryWithEveryEntryReported()
        {
            var (links, malformed) = CorosyncLinks.Parse(new[] { "garbage", "link0=" });

            Assert.Null(links);
            Assert.Equal(new[] { "garbage", "link0=" }, malformed);
        }
    }
}
