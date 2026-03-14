// =============================================================================
// BrowserTemplateFixture — E2E Fixture for the "abies-browser" Template
// =============================================================================
// Scaffolds a project from the abies-browser template, builds it, runs it
// via the WebAssembly SDK dev server, and provides Playwright access.
//
// The browser template produces a standalone WebAssembly app — the MVU loop
// runs entirely in the browser. The SDK's built-in dev server hosts the
// compiled WASM output and static files during development.
// =============================================================================

using Picea.Abies.Templates.Testing.E2E.Infrastructure;

namespace Picea.Abies.Templates.Testing.E2E.Fixtures;

/// <summary>
/// Fixture for the <c>abies-browser</c> template.
/// </summary>
public sealed class BrowserTemplateFixture : TemplateFixture
{
    /// <inheritdoc />
    protected override string TemplateName => "abies-browser";

    /// <inheritdoc />
    protected override string ProjectName => "TestBrowserApp";
}
