using Picea.Abies.DOM;
using Picea.Abies.UI;
using static Picea.Abies.UI.Components;

namespace Picea.Abies.Tests;

[NotInParallel("shared-dom-state")]
public class UiAccessibilityContractTests
{
    [Test]
    public async Task Button_DoesNotEmitRedundantAriaLabel_WhenVisibleLabelMatches()
    {
        var node = button(new ButtonOptions(
            Label: "Save",
            AriaLabel: "Save"));

        var html = Render.Html(node);

        await Assert.That(html).DoesNotContain("aria-label=");
        await Assert.That(html).Contains(">Save<");
    }

    [Test]
    public async Task Button_EmitsAriaLabel_WhenAccessibleNameDiffersFromVisibleLabel()
    {
        var node = button(new ButtonOptions(
            Label: "X",
            AriaLabel: "Close dialog"));

        var html = Render.Html(node);

        await Assert.That(html).Contains("aria-label=\"Close dialog\"");
    }

    [Test]
    public async Task TextInput_EmitsRequiredAndInvalidAttributes_WhenConfigured()
    {
        var node = textInput(new TextInputOptions(
            Name: "email",
            Value: "",
            Label: "Email",
            ErrorText: "Email is required.",
            IsRequired: true));

        var html = Render.Html(node);

        await Assert.That(html).Contains(" required");
        await Assert.That(html).Contains("aria-invalid=\"true\"");
        await Assert.That(html).Contains("aria-describedby=\"email-input-error\"");
    }

    [Test]
    public async Task Select_UsesAriaLabelFallback_WhenVisibleLabelIsMissing()
    {
        var node = select(new SelectOptions(
            Name: "theme",
            SelectedValue: "light",
            Items:
            [
                new SelectItem("light", "Light"),
                new SelectItem("dark", "Dark")
            ],
            AriaLabel: "Theme"));

        var html = Render.Html(node);

        await Assert.That(html).DoesNotContain("abies-ui-select__label");
        await Assert.That(html).Contains("aria-label=\"Theme\"");
    }

    [Test]
    public async Task Modal_UsesExplicitAriaLabel_WhenProvided()
    {
        var node = modal(new ModalOptions(
            IsOpen: true,
            Title: "Ignored title for naming",
            Content: new Text("body", "Visible content"),
            AriaLabel: "Publish confirmation"));

        var html = Render.Html(node);

        await Assert.That(html).Contains("aria-label=\"Publish confirmation\"");
        await Assert.That(html).DoesNotContain("aria-labelledby=");
    }

    [Test]
    public async Task Modal_CanAutofocusTheCloseButton_WhenRequested()
    {
        var node = modal(new ModalOptions(
            IsOpen: true,
            Title: "Keyboard check",
            Content: new Text("body", "Visible content"),
            CloseButton: new ButtonOptions("Close", Variant: ButtonVariant.Ghost),
            AutoFocusCloseButton: true));

        var html = Render.Html(node);

        await Assert.That(html).Contains(" autofocus");
    }

    [Test]
    public async Task Table_UsesCaption_AsAccessibleName_WhenPresent()
    {
        var node = table(new TableSimpleOptions(
            Columns: ["Component", "Status"],
            Rows: [["button", "ready"]],
            Caption: "Component readiness"));

        var html = Render.Html(node);

        await Assert.That(html).Contains("abies-ui-table__caption");
        await Assert.That(html).Contains(">Component readiness<");
        await Assert.That(html).DoesNotContain("aria-label=");
    }

    [Test]
    public async Task Table_UsesAriaLabel_WhenCaptionIsMissing()
    {
        var node = table(new TableSimpleOptions(
            Columns: ["Component", "Status"],
            Rows: [["button", "ready"]],
            AriaLabel: "Component status table"));

        var html = Render.Html(node);

        await Assert.That(html).Contains("aria-label=\"Component status table\"");
    }

    [Test]
    public async Task Table_SortButtons_ExposeActionOrientedAccessibleNames()
    {
        var node = table(new TableRichOptions(
            ColumnStates:
            [
                new TableColumnState("component", "Component", IsSortable: true, SortDirection: TableSortDirection.Descending),
                new TableColumnState("status", "Status")
            ],
            RowStates:
            [
                new TableRowState(["button", "ready"])
            ]));

        var html = Render.Html(node);

        await Assert.That(html).Contains("aria-label=\"Sort by Component. Currently sorted descending.\"");
    }
}