namespace TendexAI.IntegrationTests.Infrastructure;

/// <summary>
/// Defines a shared test fixture that uses a single <see cref="TendexWebApplicationFactory"/>
/// across all test classes in the collection. This ensures containers are started once
/// and shared, reducing total test execution time.
/// </summary>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
[CollectionDefinition("Integration")]
public sealed class IntegrationTestCollection : ICollectionFixture<TendexWebApplicationFactory>
{
    // This class has no code; it is used solely to define the collection.
}
#pragma warning restore CA1711
