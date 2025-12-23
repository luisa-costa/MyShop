using MyShop.ApiTests.Paralelismo.Fixtures;

namespace MyShop.ApiTests.Paralelismo.Collections;

[CollectionDefinition("NoParallelCollection", DisableParallelization = true)]
public class NoParallelCollection : ICollectionFixture<HttpClientFixture>
{
    // não precisa ter código: serve só para metadados
}
