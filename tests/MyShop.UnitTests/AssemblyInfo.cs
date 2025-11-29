using Xunit;

// Configuração de paralelismo para os testes
// DisableParallelization = false significa que os testes podem rodar em paralelo
// Por padrão, o xUnit permite paralelismo entre diferentes classes de teste
[assembly: CollectionBehavior(DisableTestParallelization = false, MaxParallelThreads = -1)]

