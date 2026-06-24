using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using OrganizationService.Clients;
using Xunit;

namespace OrganizationService.Tests.Clients
{
    public class CaptchaVerifierClientTests
    {
        private static IConfiguration ConfigWithSecret(string secret = "test-secret") =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Captcha:SecretKey"] = secret })
                .Build();

        private static IHttpClientFactory FactoryFor(HttpMessageHandler handler)
        {
            var client = new HttpClient(handler) { BaseAddress = new Uri("https://challenges.cloudflare.com/") };
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient("Turnstile")).Returns(client);
            return factory.Object;
        }

        // Always throws to simulate a network failure.
        private sealed class ThrowingHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => throw new HttpRequestException("network down");
        }

        private sealed class StaticResponseHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            private readonly string _body;
            public int Calls { get; private set; }
            public StaticResponseHandler(HttpStatusCode status, string body)
            {
                _status = status;
                _body = body;
            }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Calls++;
                return Task.FromResult(new HttpResponseMessage(_status) { Content = new StringContent(_body) });
            }
        }

        [Fact]
        public async Task VerifyAsync_ReturnsTrue_WhenSuccessTrue()
        {
            var client = new CaptchaVerifierClient(
                FactoryFor(new StaticResponseHandler(HttpStatusCode.OK, "{\"success\":true}")), ConfigWithSecret());

            Assert.True(await client.VerifyAsync("a-token", CancellationToken.None));
        }

        [Fact]
        public async Task VerifyAsync_ReturnsFalse_WhenSuccessFalse()
        {
            var client = new CaptchaVerifierClient(
                FactoryFor(new StaticResponseHandler(HttpStatusCode.OK, "{\"success\":false}")), ConfigWithSecret());

            Assert.False(await client.VerifyAsync("a-token", CancellationToken.None));
        }

        [Fact]
        public async Task VerifyAsync_FailsClosed_OnNonSuccessStatus()
        {
            var client = new CaptchaVerifierClient(
                FactoryFor(new StaticResponseHandler(HttpStatusCode.InternalServerError, "")), ConfigWithSecret());

            Assert.False(await client.VerifyAsync("a-token", CancellationToken.None));
        }

        [Fact]
        public async Task VerifyAsync_FailsClosed_WhenHttpThrows()
        {
            var client = new CaptchaVerifierClient(FactoryFor(new ThrowingHandler()), ConfigWithSecret());

            Assert.False(await client.VerifyAsync("a-token", CancellationToken.None));
        }

        [Fact]
        public async Task VerifyAsync_ReturnsFalse_AndSkipsNetwork_WhenTokenEmpty()
        {
            var handler = new StaticResponseHandler(HttpStatusCode.OK, "{\"success\":true}");
            var client = new CaptchaVerifierClient(FactoryFor(handler), ConfigWithSecret());

            Assert.False(await client.VerifyAsync("", CancellationToken.None));
            Assert.Equal(0, handler.Calls);
        }
    }
}
