using Microsoft.VisualStudio.TestPlatform.Utilities;
using MyShop.ApiTests.Paralelismo.Fixtures;
using System.Text.Json;
using Xunit.Abstractions;

namespace MyShop.ApiTests.Paralelismo
{
    public class CommentsApiParallelTests : IClassFixture<HttpClientFixture>
    {
        private readonly ITestOutputHelper _output;
        private readonly HttpClientFixture _fixture;

        public CommentsApiParallelTests(HttpClientFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [Trait("Categoria", "Paralelo")]
        public async Task GetCommentsByPost_DeveRetornarListaDeComentarios(int postId)
        {
            LogInicio();

            var response = await _fixture.Client.GetAsync($"posts/{postId}/comments");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json).RootElement;

            Assert.Equal(JsonValueKind.Array, doc.ValueKind);
            Assert.NotEmpty(doc.EnumerateArray());

            LogFim();
        }

        [Theory]
        [InlineData("posts/1/comments")]
        [InlineData("posts/2/comments")]
        [Trait("Categoria", "Paralelo")]
        public async Task Endpoint_DeveResponderEmAte_1s(string endpoint)
        {
            LogInicio();

            var cts = new System.Threading.CancellationTokenSource(millisecondsDelay: 1000);

            var response = await _fixture.Client.GetAsync(endpoint, cts.Token);

            response.EnsureSuccessStatusCode();

            LogFim();
        }

        private void LogInicio()
        {
            Task.Delay(2000).Wait(); // só pra ficar fácil ver a sequência

            _output.WriteLine(
                $"[{DateTime.Now:HH:mm:ss.fff}] INÍCIO");
        }

        private void LogFim()
        {
            _output.WriteLine(
                $"[{DateTime.Now:HH:mm:ss.fff}] FIM");
        }
    }
}
