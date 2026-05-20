using System;
using PSProxmoxVE.Core.Utilities;
using Xunit;

namespace PSProxmoxVE.Core.Tests.Utilities
{
    public class SizeParserTests
    {
        [Theory]
        [InlineData("32", "32")]
        [InlineData("32G", "32")]
        [InlineData("32g", "32")]
        [InlineData("32GB", "32")]
        [InlineData("32gb", "32")]
        [InlineData("32GiB", "32")]
        [InlineData("1T", "1024")]
        [InlineData("1t", "1024")]
        [InlineData("1TB", "1024")]
        [InlineData("1TiB", "1024")]
        [InlineData("2T", "2048")]
        [InlineData("  60G  ", "60")]
        [InlineData("60 G", "60")]
        public void NormalizeToGibibytes_AcceptedInputs_ReturnsBareGibibyteString(string input, string expected)
        {
            var result = SizeParser.NormalizeToGibibytes(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("512M")]
        [InlineData("512MB")]
        [InlineData("1024K")]
        [InlineData("1024KB")]
        [InlineData("100B")]
        [InlineData("1P")]
        [InlineData("1PB")]
        public void NormalizeToGibibytes_UnsupportedUnit_Throws(string input)
        {
            var ex = Assert.Throws<ArgumentException>(() => SizeParser.NormalizeToGibibytes(input));
            Assert.Contains("unsupported unit", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void NormalizeToGibibytes_EmptyOrWhitespace_Throws(string? input)
        {
            Assert.Throws<ArgumentException>(() => SizeParser.NormalizeToGibibytes(input!));
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("G")]
        [InlineData("-32")]
        [InlineData("32.5G")]
        [InlineData("32 G B")]
        public void NormalizeToGibibytes_Malformed_Throws(string input)
        {
            Assert.Throws<ArgumentException>(() => SizeParser.NormalizeToGibibytes(input));
        }

        [Theory]
        [InlineData("0")]
        [InlineData("0G")]
        public void NormalizeToGibibytes_Zero_Throws(string input)
        {
            var ex = Assert.Throws<ArgumentException>(() => SizeParser.NormalizeToGibibytes(input));
            Assert.Contains("positive", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void NormalizeToGibibytes_UsesProvidedParameterNameInError()
        {
            var ex = Assert.Throws<ArgumentException>(() => SizeParser.NormalizeToGibibytes("512M", "DiskSize"));
            Assert.Equal("DiskSize", ex.ParamName);
            Assert.Contains("DiskSize", ex.Message);
        }
    }
}
