namespace MyShop.ApiTests.Paralelismo.Fixtures;

public class HttpClientFixture : IDisposable
{
    public HttpClient Client { get; }

    public HttpClientFixture()
    {
        Client = new HttpClient
        {
            BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
        };
    }

    public void Dispose()
    {
        Client.Dispose();
    }
}
