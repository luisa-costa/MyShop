using Microsoft.VisualStudio.TestPlatform.Utilities;
using MyShop.ApiTests.Paralelismo.Fixtures;
using System.Net;
using System.Text.Json;
using Xunit.Abstractions;

namespace MyShop.ApiTests.Paralelismo
{
    [Collection("NoParallelCollection")] 
    public class PostsApiSerialTests : IClassFixture<HttpClientFixture>
    {
        private readonly ITestOutputHelper _output;
        private readonly HttpClientFixture _fixture;

        public PostsApiSerialTests(HttpClientFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        [Fact]
        public async Task GetPostById_1_DeveRetornarStatus200()
        {
            LogInicio();

            var response = await _fixture.Client.GetAsync("posts/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            LogFim();
        }

        [Fact]
        public async Task GetPostById_1_DeveTerUserIdValido()
        {
            LogInicio();

            _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Iniciando teste");

            var response = await _fixture.Client.GetAsync("posts/1");

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("userId", out var userIdProp));
            Assert.True(userIdProp.GetInt32() > 0);

            LogFim();
        }

        [Fact]
        public async Task GetPostById_1_DeveTerTituloNaoVazio()
        {
            LogInicio();

            var response = await _fixture.Client.GetAsync("posts/1");

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("title", out var titleProp));
            Assert.False(string.IsNullOrWhiteSpace(titleProp.GetString()));

            LogFim();
        }

        [Fact]
        public async Task SerialTest_1()
        {
            LogInicio();

            await Task.Delay(500); // só pra ficar fácil ver a sequência
            var response = await _fixture.Client.GetAsync("posts/1");
            response.EnsureSuccessStatusCode();

            LogFim();
        }

        [Fact]
        public async Task SerialTest_2()
        {
            LogInicio();

            await Task.Delay(500);
            var response = await _fixture.Client.GetAsync("posts/2");
            response.EnsureSuccessStatusCode();

            LogFim();
        }

        [Fact]
        public async Task SerialTest_3()
        {
            LogInicio();

            await Task.Delay(500);
            var response = await _fixture.Client.GetAsync("posts/3");
            response.EnsureSuccessStatusCode();

            LogFim();
        }

        private void LogInicio()
        {
            //Task.Delay(5000).Wait(); // só pra ficar fácil ver a sequência
            
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
