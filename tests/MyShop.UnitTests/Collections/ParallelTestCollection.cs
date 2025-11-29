namespace MyShop.UnitTests.Collections;

/// <summary>
/// Definição de uma collection de testes que pode rodar em paralelo.
/// 
/// Collections são usadas para agrupar testes que compartilham algum estado ou recurso.
/// Esta collection é marcada como permitindo paralelismo porque os testes dentro dela
/// não compartilham estado mutável.
/// </summary>
[CollectionDefinition("Parallel Tests")]
public class ParallelTestCollection : ICollectionFixture<ParallelTestCollection>
{
    // Esta collection não precisa de setup/teardown especial
    // Os testes podem rodar em paralelo sem problemas
}

