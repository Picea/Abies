// =============================================================================
// BrowserEmptyTemplateFixture — E2E Fixture for the "abies-browser-empty" Template
// =============================================================================
// Scaffolds a project from the abies-browser-empty template (minimal starter),
// builds it, runs it via the WebAssembly SDK dev server, and provides
// Playwright access for browser-based verification.
//
// The empty template renders a static welcome page — no counter, no messages.
// It verifies that the minimal template produces a working app.
// =============================================================================

using Picea.Abies.Templates.Testing.E2E.Infrastructure;

namespace Picea.Abies.Templates.Testing.E2E.Fixtures;

/// <summary>
/// Fixture for the <c>abies-browser-empty</c> template.
/// </summary>
public sealed class BrowserEmptyTemplateFixture : TemplateFixture
{
    /// <inheritdoc />
    protected override string TemplateName => "abies-browser-empty";

    /// <inheritdoc />
    protected override string ProjectName => "TestBrowserEmptyApp";
}
