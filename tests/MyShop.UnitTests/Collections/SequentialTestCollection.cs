namespace MyShop.UnitTests.Collections;

/// <summary>
/// Definição de uma collection de testes que NÃO deve rodar em paralelo.
/// 
/// Esta collection é usada para testes que compartilham recursos,
/// como um banco de dados em memória ou arquivos temporários.
/// 
/// IMPORTANTE: Testes nesta collection rodam sequencialmente para evitar
/// condições de corrida e conflitos de recursos compartilhados.
/// </summary>
[CollectionDefinition("Sequential Tests", DisableParallelization = true)]
public class SequentialTestCollection : ICollectionFixture<SequentialTestCollection>
{
    // Esta collection desabilita paralelismo explicitamente
    // porque os testes compartilham recursos
}

