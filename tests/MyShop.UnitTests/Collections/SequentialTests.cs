using Xunit;

namespace MyShop.UnitTests.Collections;

/// <summary>
/// Testes que demonstram quando NÃO é seguro rodar testes em paralelo.
/// 
/// Estes testes NÃO podem rodar em paralelo porque:
/// - Compartilham um recurso (simulado aqui com uma variável estática)
/// - Podem causar condições de corrida (race conditions)
/// - Dependem de estado compartilhado
/// 
/// Exemplos reais de quando usar collections sequenciais:
/// - Testes que usam o mesmo banco de dados em memória
/// - Testes que escrevem no mesmo arquivo temporário
/// - Testes que modificam configurações globais
/// - Testes que dependem de ordem de execução
/// </summary>
[Collection("Sequential Tests")]
public class SequentialTests
{
    // Variável estática compartilhada entre os testes
    // Em um cenário real, isso poderia ser um banco de dados em memória
    private static int _sharedCounter = 0;
    private static readonly object _lock = new object();

    [Fact]
    public void Test1_MustRunSequentially()
    {
        // Este teste modifica o contador compartilhado
        // Se rodasse em paralelo, poderia haver condições de corrida
        lock (_lock)
        {
            _sharedCounter++;
            Assert.True(_sharedCounter > 0);
        }
    }

    [Fact]
    public void Test2_MustRunSequentially()
    {
        // Este teste também modifica o contador compartilhado
        // Por isso, deve rodar sequencialmente após Test1
        lock (_lock)
        {
            _sharedCounter++;
            Assert.True(_sharedCounter > 0);
        }
    }

    [Fact]
    public void Test3_MustRunSequentially()
    {
        // Este teste depende do estado deixado pelos testes anteriores
        // Por isso, a ordem de execução importa
        lock (_lock)
        {
            // Em um cenário real, aqui poderia verificar dados no banco
            // que foram inseridos pelos testes anteriores
            Assert.True(_sharedCounter >= 0);
        }
    }
}

