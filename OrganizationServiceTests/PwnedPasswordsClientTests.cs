using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using OrganizationService.Clients;
using Xunit;

namespace OrganizationService.Tests.Clients
{
    public class PwnedPasswordsClientTests
    {
        private static IHttpClientFactory FactoryFor(HttpMessageHandler handler)
        {
            var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.pwnedpasswords.com/") };
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient("PwnedPasswords")).Returns(client);
            return factory.Object;
        }

        // Simple handler that always throws to simulate a network failure.
        private sealed class ThrowingHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => throw new HttpRequestException("network down");
        }

        private sealed class StaticResponseHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            private readonly string _body;
            public StaticResponseHandler(HttpStatusCode status, string body)
            {
                _status = status;
                _body = body;
            }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(new HttpResponseMessage(_status) { Content = new StringContent(_body) });
        }

        [Fact]
        public async Task IsBreachedAsync_FailsOpen_WhenHttpThrows()
        {
            var client = new PwnedPasswordsClient(FactoryFor(new ThrowingHandler()));

            var result = await client.IsBreachedAsync("Zx9$mQ2!vK7w", CancellationToken.None);

            Assert.False(result);
        }

        [Fact]
        public async Task IsBreachedAsync_FailsOpen_OnNonSuccessStatus()
        {
            var client = new PwnedPasswordsClient(FactoryFor(new StaticResponseHandler(HttpStatusCode.InternalServerError, "")));

            var result = await client.IsBreachedAsync("Zx9$mQ2!vK7w", CancellationToken.None);

            Assert.False(result);
        }

        [Fact]
        public async Task IsBreachedAsync_ReturnsTrue_WhenSuffixPresent()
        {
            // SHA-1 of "password" = 5BAA61E4C9B93F3F0682250B6CF8331B7EE68FD8
            // prefix = 5BAA6, suffix = 1E4C9B93F3F0682250B6CF8331B7EE68FD8
            const string body = "0018A45C4D1DEF81644B54AB7F969B88D65:1\r\n1E4C9B93F3F0682250B6CF8331B7EE68FD8:99\r\n";
            var client = new PwnedPasswordsClient(FactoryFor(new StaticResponseHandler(HttpStatusCode.OK, body)));

            var result = await client.IsBreachedAsync("password", CancellationToken.None);

            Assert.True(result);
        }

        [Fact]
        public async Task IsBreachedAsync_ReturnsFalse_WhenSuffixAbsent()
        {
            const string body = "0018A45C4D1DEF81644B54AB7F969B88D65:1\r\nAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA:2\r\n";
            var client = new PwnedPasswordsClient(FactoryFor(new StaticResponseHandler(HttpStatusCode.OK, body)));

            var result = await client.IsBreachedAsync("password", CancellationToken.None);

            Assert.False(result);
        }
    }
}
