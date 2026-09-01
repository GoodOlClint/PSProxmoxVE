using PSProxmoxVE.Core.Utilities;
using Xunit;

namespace PSProxmoxVE.Core.Tests.Utilities
{
    public class GuestStatusSnapshotTests
    {
        [Fact]
        public void Evaluate_RunningAndUnlocked_MatchesAndIsNotLocked()
        {
            var json = @"{""data"": {""status"": ""running"", ""qmpstatus"": ""running""}}";

            var result = GuestStatusSnapshot.Evaluate(json, "running");

            Assert.True(result.StatusMatched);
            Assert.False(result.Locked);
        }

        [Fact]
        public void Evaluate_RunningButStillLocked_MatchesAndIsLocked()
        {
            var json = @"{""data"": {""status"": ""running"", ""qmpstatus"": ""running"", ""lock"": ""clone""}}";

            var result = GuestStatusSnapshot.Evaluate(json, "running");

            Assert.True(result.StatusMatched);
            Assert.True(result.Locked);
        }

        [Fact]
        public void Evaluate_PrefersQmpStatusOverStatus()
        {
            // PVE reports status=running with qmpstatus=paused for a suspended VM.
            var json = @"{""data"": {""status"": ""running"", ""qmpstatus"": ""paused""}}";

            Assert.False(GuestStatusSnapshot.Evaluate(json, "running").StatusMatched);
            Assert.True(GuestStatusSnapshot.Evaluate(json, "paused").StatusMatched);
        }

        [Fact]
        public void Evaluate_ContainerResponseWithoutQmpStatus_FallsBackToStatus()
        {
            var json = @"{""data"": {""status"": ""stopped""}}";

            var result = GuestStatusSnapshot.Evaluate(json, "stopped");

            Assert.True(result.StatusMatched);
            Assert.False(result.Locked);
        }

        [Fact]
        public void Evaluate_EmptyLockValue_IsNotLocked()
        {
            var json = @"{""data"": {""status"": ""stopped"", ""lock"": """"}}";

            Assert.False(GuestStatusSnapshot.Evaluate(json, "stopped").Locked);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Evaluate_EmptyBody_DoesNotMatch(string? json)
        {
            var result = GuestStatusSnapshot.Evaluate(json!, "running");

            Assert.False(result.StatusMatched);
            Assert.False(result.Locked);
        }
    }
}
