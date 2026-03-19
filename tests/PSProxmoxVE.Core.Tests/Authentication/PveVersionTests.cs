using System;
using Xunit;
using PSProxmoxVE.Core.Authentication;

namespace PSProxmoxVE.Core.Tests.Authentication
{
    public class PveVersionTests
    {
        [Fact]
        public void Parse_ValidVersion_9_1()
        {
            var version = PveVersion.Parse("9.1-1");
            Assert.Equal(9, version.Major);
            Assert.Equal(1, version.Minor);
        }

        [Fact]
        public void Parse_ValidVersion_8_3()
        {
            var version = PveVersion.Parse("8.3-2");
            Assert.Equal(8, version.Major);
            Assert.Equal(3, version.Minor);
        }

        [Fact]
        public void Parse_ValidVersion_8_0()
        {
            var version = PveVersion.Parse("8.0-1");
            Assert.Equal(8, version.Major);
            Assert.Equal(0, version.Minor);
        }

        [Fact]
        public void Parse_NoPatchLevel()
        {
            var version = PveVersion.Parse("9.1");
            Assert.Equal(9, version.Major);
            Assert.Equal(1, version.Minor);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Parse_NullOrEmpty_Throws(string? input)
        {
            Assert.Throws<ArgumentException>(() => PveVersion.Parse(input!));
        }

        [Fact]
        public void Parse_InvalidFormat_Throws()
        {
            Assert.Throws<FormatException>(() => PveVersion.Parse("invalid"));
        }

        [Fact]
        public void IsAtLeast_True_SameMajorLowerMinor()
        {
            var version = PveVersion.Parse("9.1-1");
            Assert.True(version.IsAtLeast(9, 0));
        }

        [Fact]
        public void IsAtLeast_True_LowerMajor()
        {
            var version = PveVersion.Parse("9.1-1");
            Assert.True(version.IsAtLeast(8, 0));
        }

        [Fact]
        public void IsAtLeast_False()
        {
            var version = PveVersion.Parse("8.3-2");
            Assert.False(version.IsAtLeast(9, 0));
        }

        [Fact]
        public void IsAtLeast_ExactMatch()
        {
            var version = PveVersion.Parse("8.0-1");
            Assert.True(version.IsAtLeast(8, 0));
        }

        [Fact]
        public void ToString_Format()
        {
            var version = PveVersion.Parse("9.1-1");
            Assert.Equal("9.1", version.ToString());
        }
    }
}
