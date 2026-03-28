// This file exists solely to make the Program class accessible to WebApplicationFactory<Program>.
// The actual Program class is in TendexAI.API and uses top-level statements.
// We need to ensure it's visible by adding InternalsVisibleTo or by referencing it.
// Since .NET 6+ top-level statements generate an internal Program class,
// we need the API project to expose it.

namespace TendexAI.IntegrationTests.Infrastructure;

/// <summary>
/// Marker file. The actual Program class is exposed via InternalsVisibleTo
/// in the TendexAI.API project.
/// </summary>
internal static class ProgramAccessor
{
    // Intentionally empty. See TendexAI.API.csproj for InternalsVisibleTo.
}
