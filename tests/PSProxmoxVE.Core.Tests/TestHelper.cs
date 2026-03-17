using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;

namespace PSProxmoxVE.Core.Tests
{
    public static class TestHelper
    {
        public static string LoadFixture(string filename)
        {
            var path = Path.Combine("Fixtures", filename);
            return File.ReadAllText(path);
        }

        public static Mock<HttpMessageHandler> CreateMockHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(responseBody)
                });
            return mock;
        }

        public static Mock<HttpMessageHandler> CreateMockHandlerSequence(params (string body, HttpStatusCode status)[] responses)
        {
            var mock = new Mock<HttpMessageHandler>();
            var setup = mock.Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>());
            foreach (var (body, status) in responses)
            {
                setup = setup.ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = status,
                    Content = new StringContent(body)
                });
            }
            return mock;
        }
    }
}
