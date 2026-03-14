// =============================================================================
// ServerTemplateFixture — E2E Fixture for the "abies-server" Template
// =============================================================================
// Scaffolds a project from the abies-server template, builds it, runs it,
// and provides Playwright access for browser-based verification.
//
// The server template produces a Kestrel-hosted app with InteractiveServer
// rendering — the MVU loop runs on the server, DOM patches flow over WebSocket.
// =============================================================================

using Picea.Abies.Templates.Testing.E2E.Infrastructure;

namespace Picea.Abies.Templates.Testing.E2E.Fixtures;

/// <summary>
/// Fixture for the <c>abies-server</c> template.
/// </summary>
public sealed class ServerTemplateFixture : TemplateFixture
{
    /// <inheritdoc />
    protected override string TemplateName => "abies-server";

    /// <inheritdoc />
    protected override string ProjectName => "TestServerApp";
}
