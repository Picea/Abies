// =============================================================================
// Theme and Accessibility Tests
// =============================================================================
// Both are encoded as ordinary string attributes, so the vocabulary side is
// testable headlessly; resolving a theme key against a live resource dictionary
// is the backend's job and needs a running app.
// =============================================================================

using Picea.Abies.DOM;
using Picea.Abies.Native.Rendering;
using static Picea.Abies.Native.Elements;
using static Picea.Abies.Native.Properties;

namespace Picea.Abies.Native.Tests;

public class ThemeAndAccessibilityTests
{
    private static (PatchInterpreter<FakeControl> Interpreter, FakeBackend Backend) Create()
    {
        var backend = new FakeBackend();
        return (new PatchInterpreter<FakeControl>(backend), backend);
    }

    // =========================================================================
    // Theme
    // =========================================================================

    [Test]
    public async Task ThemeColor_EncodesAsAResourceLookup()
    {
        var (interpreter, backend) = Create();
        var view = (Element)StackPanel([Id("p"), Background(ThemeColor.SurfaceCard)],
            [TextBlock([Id("t"), Foreground(ThemeColor.TextSecondary)], "hi")]);

        interpreter.Apply(Operations.Diff(null, view));

        await Assert.That(backend.Root!.Props["Background"]).IsEqualTo("theme:CardBackgroundFillColorDefaultBrush");
        await Assert.That(backend.Root!.Children[0].Props["Foreground"]).IsEqualTo("theme:TextFillColorSecondaryBrush");
    }

    /// <summary>Literal colours must stay literal — the two forms have to be distinguishable.</summary>
    [Test]
    public async Task LiteralColour_IsNotTreatedAsATheme()
    {
        var (interpreter, backend) = Create();
        interpreter.Apply(Operations.Diff(null, (Element)StackPanel([Id("p"), Background("#102030")], [])));

        await Assert.That(backend.Root!.Props["Background"]).IsEqualTo("#102030");
        await Assert.That(Theme.TryDecode("#102030")).IsNull();
    }

    [Test]
    public async Task TryDecode_RoundTripsEveryRole()
    {
        foreach (var role in Enum.GetValues<ThemeColor>())
        {
            var encoded = Theme.Encode(role);
            await Assert.That(Theme.TryDecode(encoded)).IsEqualTo(Theme.ResourceKey(role));
        }
    }

    [Test]
    public async Task EveryRole_MapsToAResourceKey()
    {
        foreach (var role in Enum.GetValues<ThemeColor>())
        {
            await Assert.That(string.IsNullOrWhiteSpace(Theme.ResourceKey(role))).IsFalse();
        }
    }

    /// <summary>Switching a literal colour to a theme role is an ordinary attribute update.</summary>
    [Test]
    public async Task SwitchingFromLiteralToTheme_UpdatesInPlace()
    {
        var (interpreter, backend) = Create();
        var before = (Element)StackPanel([Id("p"), Background("#000000")], []);
        var after = (Element)StackPanel([Id("p"), Background(ThemeColor.SurfaceBase)], []);

        interpreter.Apply(Operations.Diff(null, before));
        var created = backend.AllCreated.Count;
        interpreter.Apply(Operations.Diff(before, after));

        await Assert.That(backend.Root!.Props["Background"]).IsEqualTo("theme:SolidBackgroundFillColorBaseBrush");
        await Assert.That(backend.AllCreated.Count).IsEqualTo(created);
    }

    // =========================================================================
    // Accessibility
    // =========================================================================

    [Test]
    public async Task AutomationName_ReachesTheControl()
    {
        var (interpreter, backend) = Create();
        var view = (Element)StackPanel([Id("p")],
            [Button([Id("b"), AutomationName("Delete item"), AutomationHelpText("Removes the row")], "🗑")]);

        interpreter.Apply(Operations.Diff(null, view));

        var button = backend.Root!.Children[0];
        await Assert.That(button.Props["AutomationName"]).IsEqualTo("Delete item");
        await Assert.That(button.Props["AutomationHelpText"]).IsEqualTo("Removes the row");
    }

    [Test]
    public async Task AccessibilityHidden_IsCarried()
    {
        var (interpreter, backend) = Create();
        interpreter.Apply(Operations.Diff(null,
            (Element)StackPanel([Id("p"), AccessibilityHidden(true)], [])));

        await Assert.That(backend.Root!.Props["AccessibilityHidden"]).IsEqualTo("True");
    }

    /// <summary>Removing the attribute must reset it, not leave a stale name announced.</summary>
    [Test]
    public async Task RemovingAutomationName_ResetsIt()
    {
        var (interpreter, backend) = Create();
        var before = (Element)StackPanel([Id("p")], [Button([Id("b"), AutomationName("Old")], "x")]);
        var after = (Element)StackPanel([Id("p")], [Button([Id("b")], "x")]);

        interpreter.Apply(Operations.Diff(null, before));
        interpreter.Apply(Operations.Diff(before, after));

        await Assert.That(backend.AllCreated.Single(c => c.Id == "b").Props.ContainsKey("AutomationName")).IsFalse();
    }
}
