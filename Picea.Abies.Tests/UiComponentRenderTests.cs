using Picea.Abies.DOM;
using Picea.Abies.UI;
using static Picea.Abies.Html.Events;
using static Picea.Abies.UI.Components;

namespace Picea.Abies.Tests;

[NotInParallel("shared-dom-state")]
public class UiComponentRenderTests
{
    [Test]
    public async Task Button_RendersDisabledState_WhenDisabled()
    {
        var node = disabledButton(new ButtonOptions(Label: "Save"));

        var html = Render.Html(node);

        await Assert.That(html).Contains("abies-ui-button--disabled");
        await Assert.That(html).Contains(" disabled");
        await Assert.That(html).Contains(">Save<");
    }

    [Test]
    public async Task Button_RendersLoadingState_AndBusyAttributes()
    {
        var node = loadingButton(new LoadingButtonOptions(
            Label: "Publish",
            LoadingText: "Publishing",
            Variant: ButtonVariant.Secondary,
            Common: new UiCommonOptions(DataTestId: "publish-button")));

        var html = Render.Html(node);

        await Assert.That(html).Contains("abies-ui-button--secondary");
        await Assert.That(html).Contains("abies-ui-button--loading");
        await Assert.That(html).Contains("aria-busy=\"true\"");
        await Assert.That(html).Contains("aria-disabled=\"true\"");
        await Assert.That(html).Contains("data-testid=\"publish-button\"");
        await Assert.That(html).Contains("abies-ui-button__status");
        await Assert.That(html).Contains(">Publishing<");
    }

    [Test]
    public async Task TextInput_RendersNameValueAndPlaceholder_WhenProvided()
    {
        var node = textInput(new TextInputOptions(
            Name: "email",
            Value: "maurice@example.com",
            Placeholder: "name@example.com",
            Label: "Email address",
            Description: "We will not share your email."));

        var html = Render.Html(node);

        await Assert.That(html).Contains("class=\"abies-ui-text-input\"");
        await Assert.That(html).Contains("class=\"abies-ui-text-input__label\"");
        await Assert.That(html).Contains("for=\"email-input\"");
        await Assert.That(html).Contains(">Email address<");
        await Assert.That(html).Contains("class=\"abies-ui-text-input__control\"");
        await Assert.That(html).Contains("id=\"email-input\"");
        await Assert.That(html).Contains("type=\"text\"");
        await Assert.That(html).Contains("name=\"email\"");
        await Assert.That(html).Contains("value=\"maurice@example.com\"");
        await Assert.That(html).Contains("placeholder=\"name@example.com\"");
        await Assert.That(html).Contains("aria-describedby=\"email-input-description\"");
        await Assert.That(html).Contains("id=\"email-input-description\"");
        await Assert.That(html).Contains(">We will not share your email.<");
    }

    [Test]
    public async Task TextInput_OmitsPlaceholder_WhenBlank()
    {
        var node = textInput(new TextInputOptions(
            Name: "username",
            Value: "abies",
            Placeholder: "   ",
            AriaLabel: "Username"));

        var html = Render.Html(node);

        await Assert.That(html).Contains("id=\"username-input\"");
        await Assert.That(html).Contains("name=\"username\"");
        await Assert.That(html).Contains("value=\"abies\"");
        await Assert.That(html).Contains("aria-label=\"Username\"");
        await Assert.That(html).DoesNotContain("placeholder=");
    }

    [Test]
    public async Task Select_RendersSelectedOption()
    {
        var node = select(new SelectOptions(
            Name: "framework",
            SelectedValue: "abies",
            Items:
            [
                new SelectItem("elm", "Elm"),
                new SelectItem("abies", "Abies")
            ]));

        var html = Render.Html(node);

        await Assert.That(html).Contains("<select");
        await Assert.That(html).Contains("name=\"framework\"");
        await Assert.That(html).Contains("selected");
        await Assert.That(html).Contains(">Abies<");
    }

    [Test]
    public async Task Select_RendersDescriptionError_AndReadonlyContract()
    {
        var node = readOnlySelect(new SelectOptions(
            Name: "environment",
            SelectedValue: "prod",
            Items:
            [
                new SelectItem("dev", "Development"),
                new SelectItem("prod", "Production")
            ],
            Label: "Environment",
            Description: "Used for publish routing.",
            ErrorText: "Production requires approval.",
            Common: new UiCommonOptions(Id: "environment-field")));

        var html = Render.Html(node);

        await Assert.That(html).Contains("id=\"environment-field-select\"");
        await Assert.That(html).Contains("for=\"environment-field-select\"");
        await Assert.That(html).Contains("aria-describedby=\"environment-field-select-description environment-field-select-error\"");
        await Assert.That(html).Contains("aria-invalid=\"true\"");
        await Assert.That(html).Contains("aria-readonly=\"true\"");
        await Assert.That(html).Contains("data-readonly=\"true\"");
        await Assert.That(html).Contains("abies-ui-select__description");
        await Assert.That(html).Contains("abies-ui-select__error");
    }

    [Test]
    public async Task Modal_RendersNothing_WhenClosed()
    {
        var node = modal(new ModalOptions(false, "Dialog title", new Text("body", "Hidden")));

        var html = Render.Html(node);

        await Assert.That(html).IsEmpty();
    }

    [Test]
    public async Task Spinner_RendersStatusRoleAndAccessibleLabel()
    {
        var node = spinner(new SpinnerOptions(
            Label: "Saving article",
            ScreenReaderLabelOverride: "Saving in progress"));

        var html = Render.Html(node);

        await Assert.That(html).Contains("abies-ui-spinner");
        await Assert.That(html).Contains("role=\"status\"");
        await Assert.That(html).Contains("aria-live=\"polite\"");
        await Assert.That(html).Contains("aria-busy=\"true\"");
        await Assert.That(html).Contains("aria-label=\"Saving in progress\"");
        await Assert.That(html).Contains("abies-ui-spinner__label");
        await Assert.That(html).Contains(">Saving article<");
    }

    [Test]
    public async Task Toast_RendersStatusRoleAndPoliteLiveRegion()
    {
        var node = toast(new ToastOptions("Article saved"));

        var html = Render.Html(node);

        await Assert.That(html).Contains("abies-ui-toast");
        await Assert.That(html).Contains("role=\"status\"");
        await Assert.That(html).Contains("aria-live=\"polite\"");
        await Assert.That(html).Contains("aria-atomic=\"true\"");
        await Assert.That(html).Contains("abies-ui-toast__message");
        await Assert.That(html).Contains(">Article saved<");
    }

    [Test]
    public async Task Toast_UsesAlertRole_ForAssertiveVariant()
    {
        var node = toast(new ToastOptions(
            Message: "Build failed",
            Variant: ToastVariant.Error,
            Title: "Deploy failed",
            DismissButton: new ButtonOptions("Dismiss", Variant: ButtonVariant.Ghost)));

        var html = Render.Html(node);

        await Assert.That(html).Contains("abies-ui-toast--error");
        await Assert.That(html).Contains("role=\"alert\"");
        await Assert.That(html).Contains("aria-live=\"assertive\"");
        await Assert.That(html).Contains("abies-ui-toast__title");
        await Assert.That(html).Contains("abies-ui-toast__actions");
        await Assert.That(html).Contains(">Dismiss<");
        await Assert.That(html).Contains(">Deploy failed<");
    }

    [Test]
    public async Task Modal_RendersDialogContract_WhenOpen()
    {
        var node = modal(new ModalOptions(
            IsOpen: true,
            Title: "Dialog title",
            Content: new Text("body", "Visible content"),
            Description: "Review the keyboard guidance before continuing.",
            CloseButton: new ButtonOptions("Close", Variant: ButtonVariant.Ghost),
            AutoFocusCloseButton: true,
            CloseOnEscape: true,
            OnEscape: new RenderTestMessage(),
            OnKeyDownFallback: new RenderTestMessage(),
            OnCloseButton: new RenderTestMessage(),
            Footer:
            [
                button(new ButtonOptions("Confirm", Variant: ButtonVariant.Secondary))
            ]));

        var html = Render.Html(node);

        await Assert.That(html).Contains("abies-ui-modal abies-ui-modal--open");
        await Assert.That(html).Contains("id=\"abies-ui-modal\"");
        await Assert.That(html).Contains("role=\"dialog\"");
        await Assert.That(html).Contains("aria-modal=\"true\"");
        await Assert.That(html).Contains("aria-labelledby=\"abies-ui-modal-title\"");
        await Assert.That(html).Contains("aria-describedby=\"abies-ui-modal-description\"");
        await Assert.That(html).Contains("tabindex=\"-1\"");
        await Assert.That(html).Contains("data-event-keydown=");
        await Assert.That(html).Contains("abies-ui-modal__surface");
        await Assert.That(html).Contains("abies-ui-modal__header");
        await Assert.That(html).Contains("abies-ui-modal__header-action");
        await Assert.That(html).Contains("abies-ui-modal__title");
        await Assert.That(html).Contains(" autofocus");
        await Assert.That(html).Contains(">Dialog title<");
        await Assert.That(html).Contains("abies-ui-modal__description");
        await Assert.That(html).Contains(">Review the keyboard guidance before continuing.<");
        await Assert.That(html).Contains("abies-ui-modal__body");
        await Assert.That(html).Contains("abies-ui-modal__footer");
        await Assert.That(html).Contains("data-event-click=");
        await Assert.That(html).Contains(">Close<");
        await Assert.That(html).Contains(">Confirm<");
        await Assert.That(html).Contains(">Visible content<");
    }

    [Test]
    public async Task Modal_OmitsKeydownHandler_WhenOnKeyDownIsNotProvided()
    {
        var node = modal(new ModalOptions(
            IsOpen: true,
            Title: "Dialog title",
            Content: new Text("body", "Visible content")));

        var html = Render.Html(node);

        await Assert.That(html).DoesNotContain("data-event-keydown=");
    }

    [Test]
    public async Task Modal_DoesNotWireCloseButtonClick_WhenCloseRequestIsMissing()
    {
        var node = modal(new ModalOptions(
            IsOpen: true,
            Title: "Dialog title",
            Content: new Text("body", "Visible content"),
            CloseButton: new ButtonOptions("Close", Variant: ButtonVariant.Ghost)));

        var html = Render.Html(node);

        await Assert.That(html).DoesNotContain("data-event-keydown=");
        await Assert.That(html).DoesNotContain("data-event-click=");
    }

    [Test]
    public async Task Modal_UsesCustomKeydownHandler_WhenCloseOnEscapeIsDisabled()
    {
        var node = modal(new ModalOptions(
            IsOpen: true,
            Title: "Dialog title",
            Content: new Text("body", "Visible content")));

        var html = Render.Html(node);

        await Assert.That(html).DoesNotContain("data-event-keydown=");
    }

    [Test]
    public async Task Modal_StampsFocusReturnTarget_WhenFocusReturnTargetIdIsProvided()
    {
        var node = modal(new ModalOptions(
            IsOpen: true,
            Title: "Dialog title",
            Content: new Text("body", "Visible content"),
            FocusReturnTargetId: "modal-trigger-button"));

        var html = Render.Html(node);

        await Assert.That(html).Contains("data-focus-return=\"modal-trigger-button\"");
    }

    [Test]
    public async Task Modal_OmitsFocusReturnTarget_WhenFocusReturnTargetIdIsAbsent()
    {
        var node = modal(new ModalOptions(
            IsOpen: true,
            Title: "Dialog title",
            Content: new Text("body", "Visible content")));

        var html = Render.Html(node);

        await Assert.That(html).DoesNotContain("data-focus-return=");
    }

    [Test]
    public async Task Table_RendersHeaderAndBodyCells()
    {
        var node = table(new TableSimpleOptions(
            Columns: ["Component", "Status"],
            Rows:
            [
                ["button", "ready"],
                ["modal", "skeleton"]
            ]));

        var html = Render.Html(node);

        await Assert.That(html).Contains("abies-ui-table");
        await Assert.That(html).Contains(">Component<");
        await Assert.That(html).Contains(">ready<");
        await Assert.That(html).Contains(">skeleton<");
    }

    [Test]
    public async Task Table_RendersEmptyLoadingAndErrorMessages()
    {
        var emptyNode = table(new TableSimpleOptions(
            Columns: ["Component", "Status"],
            Rows: [],
            EmptyStateText: "Nothing to show yet"));

        var loadingNode = table(new TableSimpleOptions(
            Columns: ["Component", "Status"],
            Rows: [],
            IsBusy: true,
            LoadingText: "Loading component data"));

        var errorNode = table(new TableSimpleOptions(
            Columns: ["Component", "Status"],
            Rows: [],
            ErrorText: "Could not load component data"));

        var emptyHtml = Render.Html(emptyNode);
        var loadingHtml = Render.Html(loadingNode);
        var errorHtml = Render.Html(errorNode);

        await Assert.That(emptyHtml).Contains(">Nothing to show yet<");
        await Assert.That(emptyHtml).Contains("abies-ui-table--empty");
        await Assert.That(loadingHtml).Contains("aria-busy=\"true\"");
        await Assert.That(loadingHtml).Contains("abies-ui-table--busy");
        await Assert.That(loadingHtml).Contains(">Loading component data<");
        await Assert.That(errorHtml).Contains("abies-ui-table--error");
        await Assert.That(errorHtml).Contains(">Could not load component data<");
    }

    [Test]
    public async Task Table_RendersSortableHeaders_AndSelectedRows()
    {
        var node = table(new TableRichOptions(
            ColumnStates:
            [
                new TableColumnState("component", "Component", IsSortable: true, SortDirection: TableSortDirection.Ascending),
                new TableColumnState("status", "Status")
            ],
            RowStates:
            [
                new TableRowState(["button", "ready"]),
                new TableRowState(["modal", "scaffolded"], IsSelected: true)
            ]));

        var html = Render.Html(node);

        await Assert.That(html).Contains("aria-sort=\"ascending\"");
        await Assert.That(html).Contains("abies-ui-table__sort-button");
        await Assert.That(html).Contains("aria-label=\"Sort by Component. Currently sorted ascending.\"");
        await Assert.That(html).Contains("abies-ui-table__row--selected");
        await Assert.That(html).Contains("aria-selected=\"true\"");
        await Assert.That(html).Contains("tabindex=\"0\"");
    }

    [Test]
    public async Task Table_UsesExplicitSortButtonLabel_AndFocusableRows_WhenConfigured()
    {
        var node = table(new TableRichOptions(
            ColumnStates:
            [
                new TableColumnState("component", "Component", IsSortable: true, SortButtonAriaLabel: "Sort by component name"),
                new TableColumnState("status", "Status")
            ],
            RowStates:
            [
                new TableRowState(["button", "ready"], IsFocusable: true)
            ]));

        var html = Render.Html(node);

        await Assert.That(html).Contains("aria-label=\"Sort by component name\"");
        await Assert.That(html).Contains("tabindex=\"0\"");
    }

    [Test]
    public async Task Table_WiresRowInteractionHandlers_WhenOnRowRequestIsProvided()
    {
        var node = table(new TableRichOptions(
            ColumnStates:
            [
                new TableColumnState("component", "Component"),
                new TableColumnState("status", "Status")
            ],
            RowStates:
            [
                new TableRowState(["button", "ready"], SourceIndex: 7)
            ],
            OnRowClick: new RenderTestMessage(),
            OnRowKeyDown: new RenderTestMessage()));

        var html = Render.Html(node);

        await Assert.That(html).Contains("data-event-click=");
        await Assert.That(html).Contains("data-event-keydown=");
        await Assert.That(html).Contains("tabindex=\"0\"");
    }

    [Test]
    public async Task Table_DoesNotDuplicateExistingRowHandlers_WhenRowCommonAlreadyDefinesThem()
    {
        var rowCommon = new UiCommonOptions(
            Attributes:
            [
                onclick(new RenderTestMessage()),
                onkeydown(_ => new RenderTestMessage())
            ]);

        var node = table(new TableRichOptions(
            ColumnStates:
            [
                new TableColumnState("component", "Component"),
                new TableColumnState("status", "Status")
            ],
            RowStates:
            [
                new TableRowState(["button", "ready"], Common: rowCommon)
            ],
            OnRowClick: new RenderTestMessage(),
            OnRowKeyDown: new RenderTestMessage()));

        var html = Render.Html(node);

        await Assert.That(CountOccurrences(html, "data-event-click=")).IsEqualTo(1);
        await Assert.That(CountOccurrences(html, "data-event-keydown=")).IsEqualTo(1);
    }

    private static int CountOccurrences(string value, string needle)
    {
        var count = 0;
        var currentIndex = 0;

        while (currentIndex < value.Length)
        {
            var matchIndex = value.IndexOf(needle, currentIndex, StringComparison.Ordinal);

            if (matchIndex < 0)
            {
                break;
            }

            count++;
            currentIndex = matchIndex + needle.Length;
        }

        return count;
    }
}

file sealed record RenderTestMessage : Message;