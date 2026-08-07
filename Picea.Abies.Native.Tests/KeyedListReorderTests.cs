// =============================================================================
// Keyed List Reorder Tests
// =============================================================================
// Mirrors the TaskTimer sample's list: rows keyed by task id, re-sorted as
// elapsed time changes. Asserts that a re-sort moves existing controls rather
// than rebuilding the list — the property the sample demonstrates in a real
// window, pinned here so a regression fails fast and headlessly.
// =============================================================================

using Picea.Abies.DOM;
using Picea.Abies.Native.Rendering;
using static Picea.Abies.Native.Elements;
using static Picea.Abies.Native.Properties;

namespace Picea.Abies.Native.Tests;

public class KeyedListReorderTests
{
    private static (PatchInterpreter<FakeControl> Interpreter, FakeBackend Backend) Create()
    {
        var backend = new FakeBackend();
        return (new PatchInterpreter<FakeControl>(backend), backend);
    }

    /// <summary>One row, shaped like the sample's: keyed border wrapping labels.</summary>
    private static Node Row(string id, string name, int elapsed) =>
        Border([Id($"row-{id}"), Key(id), Padding(8)],
            StackPanel([Id($"inner-{id}"), Orientation(StackOrientation.Horizontal)],
            [
                TextBlock([Id($"name-{id}")], name),
                TextBlock([Id($"elapsed-{id}")], $"{elapsed}s"),
            ]));

    private static Element List(params (string Id, string Name, int Elapsed)[] rows) =>
        (Element)StackPanel([Id("list")], [.. rows.Select(r => Row(r.Id, r.Name, r.Elapsed))]);

    [Test]
    public async Task ResortingByElapsed_MovesRowsInsteadOfRecreatingThem()
    {
        var (interpreter, backend) = Create();

        // "b" is running and overtakes the others.
        var before = List(("a", "Alpha", 5), ("b", "Beta", 3), ("c", "Gamma", 1));
        var after = List(("b", "Beta", 9), ("a", "Alpha", 5), ("c", "Gamma", 1));

        interpreter.Apply(Operations.Diff(null, before));
        var createdAfterFirstRender = backend.AllCreated.Count;
        interpreter.Apply(Operations.Diff(before, after));

        var order = backend.Root!.Children.Select(c => c.Id).ToArray();
        await Assert.That(order).IsEquivalentTo(new[] { "row-b", "row-a", "row-c" });

        // The whole point: reordering must not rebuild the rows.
        await Assert.That(backend.AllCreated.Count).IsEqualTo(createdAfterFirstRender);
        await Assert.That(backend.Log.Any(l => l.StartsWith("move"))).IsTrue();
    }

    [Test]
    public async Task TickUpdatesElapsedTextInPlace()
    {
        var (interpreter, backend) = Create();
        var before = List(("a", "Alpha", 0));
        var after = List(("a", "Alpha", 1));

        interpreter.Apply(Operations.Diff(null, before));
        var createdAfterFirstRender = backend.AllCreated.Count;
        interpreter.Apply(Operations.Diff(before, after));

        var elapsed = backend.AllCreated.Single(c => c.Id == "elapsed-a");
        await Assert.That(elapsed.Props["Text"]).IsEqualTo("1s");
        await Assert.That(backend.AllCreated.Count).IsEqualTo(createdAfterFirstRender);
    }

    [Test]
    public async Task RemovingARow_LeavesTheOthersIntact()
    {
        var (interpreter, backend) = Create();
        var before = List(("a", "Alpha", 2), ("b", "Beta", 1), ("c", "Gamma", 0));
        var after = List(("a", "Alpha", 2), ("c", "Gamma", 0));

        interpreter.Apply(Operations.Diff(null, before));
        var createdAfterFirstRender = backend.AllCreated.Count;
        interpreter.Apply(Operations.Diff(before, after));

        var order = backend.Root!.Children.Select(c => c.Id).ToArray();
        await Assert.That(order).IsEquivalentTo(new[] { "row-a", "row-c" });
        await Assert.That(backend.AllCreated.Count).IsEqualTo(createdAfterFirstRender);
    }

    /// <summary>Growing from empty is the SetChildrenHtml fast path.</summary>
    [Test]
    public async Task AddingTheFirstRow_Renders()
    {
        var (interpreter, backend) = Create();
        var before = (Element)StackPanel([Id("list")], []);
        var after = List(("a", "Alpha", 0));

        interpreter.Apply(Operations.Diff(null, before));
        interpreter.Apply(Operations.Diff(before, after));

        await Assert.That(backend.Root!.Children).Count().IsEqualTo(1);
        await Assert.That(backend.AllCreated.Single(c => c.Id == "name-a").Props["Text"]).IsEqualTo("Alpha");
    }
}
