using Xunit;

namespace MyShop.UnitTests.Collections;

/// <summary>
/// Testes que demonstram quando é seguro rodar testes em paralelo.
/// 
/// Estes testes podem rodar em paralelo porque:
/// - Não compartilham estado mutável
/// - Não dependem de recursos externos compartilhados
/// - Cada teste é independente
/// - Usam mocks isolados
/// 
/// Rodar testes em paralelo acelera a execução, especialmente em projetos grandes.
/// </summary>
[Collection("Parallel Tests")]
public class ParallelTests
{
    [Fact]
    public void Test1_CanRunInParallel()
    {
        // Este teste pode rodar em paralelo com outros
        // porque não compartilha estado
        var result = 1 + 1;
        Assert.Equal(2, result);
    }

    [Fact]
    public void Test2_CanRunInParallel()
    {
        // Este teste também pode rodar em paralelo
        // mesmo que execute ao mesmo tempo que Test1
        var result = 2 * 2;
        Assert.Equal(4, result);
    }

    [Fact]
    public void Test3_CanRunInParallel()
    {
        // Testes que usam apenas variáveis locais
        // são seguros para execução paralela
        var list = new List<int> { 1, 2, 3 };
        Assert.Equal(3, list.Count);
    }
}

