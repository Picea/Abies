// Regression test: verifies the debugger JSON bridge works under WASM AOT/trimming.
// Root cause (2025): JsonSerializer.Serialize threw JsonSerializerIsReflectionDisabled
// because reflection-based serialization is disabled in .NET WASM.
// Fix: source-generated DebuggerAdapterJsonContext in DebuggerAdapterProtocol.cs.

using Microsoft.Playwright;
using Picea.Abies.Counter.Testing.E2E.Fixtures;
using Picea.Abies.Counter.Testing.E2E.Helpers;

namespace Picea.Abies.Counter.Testing.E2E;

[Category("E2E")]
[ClassDataSource<CounterWasmFixture>(Shared = SharedType.Keyed, Key = "CounterWasm")]
[NotInParallel("CounterWasm")]
public sealed class DebuggerBridgeRegressionTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly CounterWasmFixture _fixture;
    private IPage _page = null!;

    public DebuggerBridgeRegressionTests(CounterWasmFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _page.Context.DisposeAsync();
    }

    [Test]
    public async Task DebuggerBridge_ShouldNotThrowJsonSerializerIsReflectionDisabled()
    {
        var bridgeErrors = new List<string>();
        _page.Console += (_, msg) =>
        {
            if (msg.Type == "warning" && msg.Text.Contains("Bridge error"))
                bridgeErrors.Add(msg.Text);
        };

        await _page.GotoAsync("/");
        await InteractivityHelpers.WaitForWasmInteractivity(_page);

        // Click + twice to generate timeline events
        var inc = _page.GetByRole(AriaRole.Button, new() { Name = "+" });
        await inc.ClickAsync();
        await Task.Delay(300);
        await inc.ClickAsync();
        await Task.Delay(300);

        // Open debugger panel
        var shell = _page.GetByRole(AriaRole.Button, new() { Name = "Abies Debugger" });
        await shell.ClickAsync();
        await Task.Delay(300);

        // Verify no bridge errors (the original bug produced JsonSerializerIsReflectionDisabled)
        await Assert.That(bridgeErrors).IsEmpty();

        // Verify the timeline populated (proves serialization worked end-to-end)
        var eventCount = await _page.EvaluateAsync<int>(@"(() => {
            const mp = document.getElementById('abies-debugger-timeline');
            if (!mp) return 0;
            const eventList = mp.querySelector('[role=listbox]');
            return eventList?.children.length ?? 0;
        })()");

        await Assert.That(eventCount).IsGreaterThanOrEqualTo(2);

        // Verify Back button is enabled (proves bridge responses are parsed correctly)
        var backDisabled = await _page.EvaluateAsync<bool>(@"(() => {
            const mp = document.getElementById('abies-debugger-timeline');
            const btn = mp?.querySelector('button[data-intent=step-back]');
            return btn?.disabled ?? true;
        })()");

        await Assert.That(backDisabled).IsFalse();
    }
}
