using System;
using System.Net;
using System.Threading.Tasks;
using PSProxmoxVE.Core.Exceptions;
using PSProxmoxVE.Core.Utilities;
using Xunit;

namespace PSProxmoxVE.Core.Tests.Utilities
{
    public class GuestLockRetryTests
    {
        private const string VmLockError =
            "can't lock file '/var/lock/qemu-server/lock-100.conf' - got timeout";

        private const string LxcLockError =
            "can't lock file '/run/lock/lxc/pve-config-100.lock' - got timeout";

        private static PveApiException ApiError(string message) =>
            new PveApiException(HttpStatusCode.InternalServerError, message, "nodes/pve9a/qemu/100/config", "PUT");

        private static PveTaskFailedException TaskError(string exitStatus) =>
            new PveTaskFailedException("UPID:pve9a:00000001:qmreset:100:root@pam:", exitStatus);

        [Fact]
        public void IsLockTimeout_MatchesTheVmFlockErrorFromBothSurfaces()
        {
            Assert.True(GuestLockRetry.IsLockTimeout(ApiError(VmLockError)));
            Assert.True(GuestLockRetry.IsLockTimeout(TaskError(VmLockError)));
        }

        [Fact]
        public void IsLockTimeout_MatchesTheContainerFlockError()
        {
            Assert.True(GuestLockRetry.IsLockTimeout(TaskError(LxcLockError)));
        }

        [Theory]
        [InlineData("VM 100 not running")]
        [InlineData("can't lock file '/var/lock/qemu-server/lock-100.conf'")]
        [InlineData("got timeout")]
        // PVE::Tools::lock_file uses this same wording for locks that carry no
        // reissue-safety guarantee. Only the two guest config paths may retry.
        [InlineData("can't lock file '/run/lock/pve-manager/pve-storage-local' - got timeout")]
        [InlineData("can't lock file '/var/lock/pve-manager/pve-backup' - got timeout")]
        [InlineData("can't lock file '/run/lock/lvm/V_pve' - got timeout")]
        // A message PVE prefixed with its own context means the worker had already
        // started; reissuing it could repeat work that landed.
        [InlineData("clone failed: can't lock file '/var/lock/qemu-server/lock-100.conf' - got timeout")]
        [InlineData("unable to resize: can't lock file '/var/lock/qemu-server/lock-100.conf' - got timeout")]
        public void IsLockTimeout_RejectsEveryOtherFailure(string message)
        {
            Assert.False(GuestLockRetry.IsLockTimeout(ApiError(message)));
            Assert.False(GuestLockRetry.IsLockTimeout(TaskError(message)));
        }

        [Fact]
        public void IsLockTimeout_ReadsWhatPveSaidRatherThanTheComposedMessage()
        {
            // Both exception types prefix Message with their own context, which would
            // defeat the anchor if the predicate matched on Message.
            var api = ApiError(VmLockError);
            var task = TaskError(VmLockError);

            Assert.StartsWith("PVE API error", api.Message);
            Assert.StartsWith("Task UPID:", task.Message);
            Assert.True(GuestLockRetry.IsLockTimeout(api));
            Assert.True(GuestLockRetry.IsLockTimeout(task));
        }

        [Fact]
        public void IsLockTimeout_RejectsExceptionTypesThatAreNotApiFailures()
        {
            Assert.False(GuestLockRetry.IsLockTimeout(new InvalidOperationException(VmLockError)));
        }

        [Fact]
        public void Execute_ReturnsWithoutRetryingWhenTheOperationSucceeds()
        {
            var attempts = 0;

            var result = GuestLockRetry.Execute(() => { attempts++; return 42; });

            Assert.Equal(42, result);
            Assert.Equal(1, attempts);
        }

        [Fact]
        public void Execute_ReissuesUntilTheLockClears()
        {
            var attempts = 0;

            var result = GuestLockRetry.Execute(() =>
            {
                attempts++;
                if (attempts < 2) throw TaskError(VmLockError);
                return "cloned";
            });

            Assert.Equal("cloned", result);
            Assert.Equal(2, attempts);
        }

        [Fact]
        public void Execute_DoesNotRetryOtherFailures()
        {
            var attempts = 0;

            Assert.Throws<PveApiException>(() => GuestLockRetry.Execute<int>(() =>
            {
                attempts++;
                throw ApiError("VM 100 not running");
            }));

            Assert.Equal(1, attempts);
        }

        [Fact]
        public async Task ExecuteAsync_ReissuesUntilTheLockClears()
        {
            var attempts = 0;

            var result = await GuestLockRetry.ExecuteAsync(() =>
            {
                attempts++;
                if (attempts < 2) throw ApiError(VmLockError);
                return Task.FromResult("written");
            });

            Assert.Equal("written", result);
            Assert.Equal(2, attempts);
        }

        [Fact]
        public async Task ExecuteAsync_DoesNotRetryOtherFailures()
        {
            var attempts = 0;

            await Assert.ThrowsAsync<PveApiException>(() => GuestLockRetry.ExecuteAsync<int>(() =>
            {
                attempts++;
                throw ApiError("can't lock file '/run/lock/lvm/V_pve' - got timeout");
            }));

            Assert.Equal(1, attempts);
        }

        [Fact]
        public void Execute_GivesUpAndRethrowsOnceTheWindowElapses()
        {
            var attempts = 0;

            var ex = Assert.Throws<PveTaskFailedException>(() => GuestLockRetry.Execute<int>(
                () => { attempts++; throw TaskError(VmLockError); },
                TimeSpan.Zero));

            Assert.Contains("got timeout", ex.Message);
            Assert.Equal(1, attempts);
        }
    }
}
