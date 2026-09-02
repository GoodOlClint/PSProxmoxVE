using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Utilities;
using Xunit;

namespace PSProxmoxVE.Core.Tests.Utilities
{
    public class ApiValueHelperTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(1L)]
        [InlineData(1)]
        [InlineData("1")]
        public void IsExited_TrueValues_ReturnsTrue(object value)
        {
            Assert.True(ApiValueHelper.IsExited(value));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(0L)]
        [InlineData(0)]
        [InlineData("0")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("true")]
        [InlineData(2L)]
        [InlineData(42)]
        public void IsExited_FalseValues_ReturnsFalse(object? value)
        {
            Assert.False(ApiValueHelper.IsExited(value));
        }

        [Theory]
        [InlineData("{\"data\":{\"exited\":true}}", true)]
        [InlineData("{\"data\":{\"exited\":false}}", false)]
        [InlineData("{\"data\":{\"exited\":1}}", true)]
        [InlineData("{\"data\":{\"exited\":0}}", false)]
        [InlineData("{\"data\":{\"exited\":\"1\"}}", true)]
        [InlineData("{\"data\":{\"exited\":\"0\"}}", false)]
        [InlineData("{\"data\":{\"exited\":null}}", false)]
        [InlineData("{\"data\":{}}", false)]
        public void IsExited_ApiJsonValues_ReturnExpectedCompletionState(string json, bool expected)
        {
            var data = (JObject)JObject.Parse(json)["data"]!;
            var status = JsonHelper.ToDictionary(data);
            var completed = status.TryGetValue("exited", out var exited) && ApiValueHelper.IsExited(exited);

            Assert.Equal(expected, completed);
        }
    }
}
