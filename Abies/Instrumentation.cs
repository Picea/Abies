// =============================================================================
// Instrumentation
// =============================================================================
// OpenTelemetry instrumentation for the Abies framework.
//
// Architecture Decision Records:
// - ADR-013: OpenTelemetry Instrumentation (docs/adr/ADR-013-opentelemetry.md)
// =============================================================================

namespace Abies;

using System.Diagnostics;

/// <summary>
/// Provides the ActivitySource for OpenTelemetry tracing.
/// </summary>
/// <remarks>
/// All framework instrumentation uses this ActivitySource.
/// Applications can use the same source or create their own.
/// 
/// See ADR-013: OpenTelemetry Instrumentation
/// </remarks>
public static class Instrumentation
{
    /// <summary>
    /// The ActivitySource for all Abies tracing spans.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new("Abies");
}
