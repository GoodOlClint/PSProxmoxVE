using System;
using System.Net;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Errors;
using PSProxmoxVE.Core.Exceptions;
using Xunit;

namespace PSProxmoxVE.Core.Tests.Errors
{
    public class PveErrorMapperTests
    {
        private static PveApiException Api(HttpStatusCode status, string resource = "nodes/pve1/qemu/100")
            => new PveApiException(status, "denied", resource, "GET");

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, PveErrorKind.InvalidArgument)]
        [InlineData(HttpStatusCode.Unauthorized, PveErrorKind.AuthenticationError)]
        [InlineData(HttpStatusCode.Forbidden, PveErrorKind.PermissionDenied)]
        [InlineData(HttpStatusCode.NotFound, PveErrorKind.ObjectNotFound)]
        [InlineData(HttpStatusCode.RequestTimeout, PveErrorKind.OperationTimeout)]
        [InlineData(HttpStatusCode.GatewayTimeout, PveErrorKind.OperationTimeout)]
        [InlineData(HttpStatusCode.ServiceUnavailable, PveErrorKind.ConnectionError)]
        [InlineData(HttpStatusCode.InternalServerError, PveErrorKind.ResourceUnavailable)]
        [InlineData(HttpStatusCode.BadGateway, PveErrorKind.ResourceUnavailable)]
        [InlineData(HttpStatusCode.Conflict, PveErrorKind.InvalidOperation)]
        [InlineData(HttpStatusCode.MethodNotAllowed, PveErrorKind.InvalidOperation)]
        [InlineData((HttpStatusCode)599, PveErrorKind.ResourceUnavailable)]
        [InlineData((HttpStatusCode)418, PveErrorKind.InvalidOperation)]
        [InlineData((HttpStatusCode)600, PveErrorKind.InvalidOperation)]
        public void Describe_MapsApiStatusToKind(HttpStatusCode status, PveErrorKind expected)
        {
            Assert.Equal(expected, PveErrorMapper.Describe(Api(status)).Kind);
        }

        [Fact]
        public void Describe_ApiErrorIdCarriesStatusAndResource()
        {
            var descriptor = PveErrorMapper.Describe(Api(HttpStatusCode.NotFound));

            Assert.Equal("PveApi.404.nodes/pve1/qemu/100", descriptor.ErrorId);
            Assert.Equal("nodes/pve1/qemu/100", descriptor.Target);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Describe_ApiErrorIdOmitsBlankResource(string resource)
        {
            var descriptor = PveErrorMapper.Describe(Api(HttpStatusCode.Forbidden, resource));

            Assert.Equal("PveApi.403", descriptor.ErrorId);
            Assert.Null(descriptor.Target);
        }

        [Fact]
        public void Describe_MapsNotConnectedToConnectionError()
        {
            var descriptor = PveErrorMapper.Describe(new PveNotConnectedException());

            Assert.Equal(PveErrorKind.ConnectionError, descriptor.Kind);
            Assert.Equal("PveNotConnected", descriptor.ErrorId);
            Assert.Null(descriptor.Target);
        }

        [Fact]
        public void Describe_MapsSessionExpiredToAuthenticationError()
        {
            var descriptor = PveErrorMapper.Describe(new PveSessionExpiredException());

            Assert.Equal(PveErrorKind.AuthenticationError, descriptor.Kind);
            Assert.Equal("PveSessionExpired", descriptor.ErrorId);
            Assert.Null(descriptor.Target);
        }

        [Fact]
        public void Describe_MapsTaskFailedToOperationStoppedWithUpidTarget()
        {
            var descriptor = PveErrorMapper.Describe(new PveTaskFailedException("UPID:pve1:1", "boom"));

            Assert.Equal(PveErrorKind.OperationStopped, descriptor.Kind);
            Assert.Equal("PveTaskFailed", descriptor.ErrorId);
            Assert.Equal("UPID:pve1:1", descriptor.Target);
        }

        [Fact]
        public void Describe_MapsTaskTimeoutToOperationTimeoutWithUpidTarget()
        {
            var descriptor = PveErrorMapper.Describe(
                new PveTaskTimeoutException("UPID:pve1:2", TimeSpan.FromSeconds(5)));

            Assert.Equal(PveErrorKind.OperationTimeout, descriptor.Kind);
            Assert.Equal("PveTaskTimeout", descriptor.ErrorId);
            Assert.Equal("UPID:pve1:2", descriptor.Target);
        }

        [Fact]
        public void Describe_MapsVersionExceptionToInvalidOperation()
        {
            var descriptor = PveErrorMapper.Describe(
                new PveVersionException(9, 0, new PveVersion(8, 4)));

            Assert.Equal(PveErrorKind.InvalidOperation, descriptor.Kind);
            Assert.Equal("PveVersionTooOld", descriptor.ErrorId);
            Assert.Null(descriptor.Target);
        }

        [Fact]
        public void Describe_LeavesUnrecognisedExceptionUnclassified()
        {
            var descriptor = PveErrorMapper.Describe(new InvalidOperationException("x"));

            Assert.Equal(PveErrorKind.NotSpecified, descriptor.Kind);
            Assert.Equal("InvalidOperationException", descriptor.ErrorId);
            Assert.Null(descriptor.Target);
        }

        [Fact]
        public void IsRecognized_AcceptsEveryModuleException()
        {
            Assert.True(PveErrorMapper.IsRecognized(Api(HttpStatusCode.NotFound)));
            Assert.True(PveErrorMapper.IsRecognized(new PveNotConnectedException()));
            Assert.True(PveErrorMapper.IsRecognized(new PveSessionExpiredException()));
            Assert.True(PveErrorMapper.IsRecognized(new PveTaskFailedException("u", "boom")));
            Assert.True(PveErrorMapper.IsRecognized(new PveTaskTimeoutException("u", TimeSpan.Zero)));
            Assert.True(PveErrorMapper.IsRecognized(new PveVersionException(9, 0, new PveVersion(8, 4))));
        }

        [Fact]
        public void IsRecognized_RejectsExceptionsTheModuleDoesNotClassify()
        {
            Assert.False(PveErrorMapper.IsRecognized(new ArgumentException("x")));
            Assert.False(PveErrorMapper.IsRecognized(new TimeoutException("x")));
            Assert.False(PveErrorMapper.IsRecognized(new InvalidOperationException("x")));
        }

        [Fact]
        public void Describe_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => PveErrorMapper.Describe(null!));
        }
    }
}
