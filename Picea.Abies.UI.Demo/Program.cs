using System.Runtime.Versioning;
using Picea;
using Picea.Abies;
using Picea.Abies.Browser;
using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using Picea.Abies.UI;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Events;
using static Picea.Abies.Subscriptions.SubscriptionModule;
using static Picea.Abies.UI.Components;

[assembly: SupportedOSPlatform("browser")]

await Runtime.Run<UiDemo, DemoModel, DemoArguments>(new DemoArguments());

internal sealed record DemoArguments;

internal sealed record DemoModel(
    bool IsModalOpen,
    bool IsLoadingButtonActive,
    bool IsStatusSortAscending,
    int? SelectedTableRowIndex,
    double ProgressBarValue = 45.0,
    bool IsAlertVisible = true);

internal interface DemoMessage : Message
{
    sealed record ToggleModal : DemoMessage;

    sealed record ToggleLoadingButton : DemoMessage;

    sealed record ToggleStatusSort : DemoMessage;

    sealed record SelectTableRow(int Index) : DemoMessage;

    sealed record CloseModal : DemoMessage;

    sealed record Noop : DemoMessage;

    sealed record IncrementProgress : DemoMessage;

    sealed record ToggleAlert : DemoMessage;
}

internal sealed class UiDemo : Program<DemoModel, DemoArguments>
{
    public static (DemoModel, Command) Initialize(DemoArguments argument)
        => (
            new DemoModel(
                IsModalOpen: false,
                IsLoadingButtonActive: false,
                IsStatusSortAscending: true,
                SelectedTableRowIndex: 1,
                ProgressBarValue: 45.0,
                IsAlertVisible: true),
            Commands.None);

    public static Subscription Subscriptions(DemoModel model)
        => None;

    public static (DemoModel, Command) Transition(DemoModel model, Message message)
        => message switch
        {
            DemoMessage.ToggleModal => (model with { IsModalOpen = !model.IsModalOpen }, Commands.None),
            DemoMessage.ToggleLoadingButton => (model with { IsLoadingButtonActive = !model.IsLoadingButtonActive }, Commands.None),
            DemoMessage.ToggleStatusSort => (model with { IsStatusSortAscending = !model.IsStatusSortAscending }, Commands.None),
            DemoMessage.SelectTableRow selectedRow => (model with { SelectedTableRowIndex = selectedRow.Index }, Commands.None),
            DemoMessage.CloseModal => (model with { IsModalOpen = false }, Commands.None),
            DemoMessage.Noop => (model, Commands.None),
            DemoMessage.IncrementProgress => (model with { ProgressBarValue = Math.Min(100.0, model.ProgressBarValue + 10.0) }, Commands.None),
            DemoMessage.ToggleAlert => (model with { IsAlertVisible = !model.IsAlertVisible }, Commands.None),
            _ => (model, Commands.None)
        };

    public static Result<Message[], Message> Decide(DemoModel _, Message command) =>
        Result<Message[], Message>.Ok([command]);

    public static bool IsTerminal(DemoModel _) => false;

    public static Document View(DemoModel model)
        => new(
            "Abies UI Demo",
            main(
                [class_("demo-page")],
                [
                    header(
                        [class_("demo-hero")],
                        [
                            p([class_("demo-eyebrow")], [text("Picea.Abies.UI.Demo")]),
                            h1([], [text("Phase 2 component kit")]),
                            p(
                                [class_("demo-intro")],
                                [text("Extending the Phase 1 baseline with layout and feedback components.")])
                        ]),
                    div(
                        [class_("demo-grid")],
                        [
                            ShowcaseSection(
                                "button",
                                "Primary, secondary, ghost, disabled, and loading states.",
                                div(
                                    [class_("demo-stack")],
                                    [
                                        div(
                                            [class_("demo-row")],
                                            [
                                                button(new ButtonOptions("Primary action", Variant: ButtonVariant.Primary)),
                                                button(new ButtonOptions("Secondary action", Variant: ButtonVariant.Secondary)),
                                                button(new ButtonOptions("Ghost action", Variant: ButtonVariant.Ghost))
                                            ]),
                                        div(
                                            [class_("demo-row")],
                                            [
                                                disabledButton(new ButtonOptions("Disabled")),
                                                model.IsLoadingButtonActive
                                                        ? loadingButton(new LoadingButtonOptions("Processing", LoadingText: "Working", Common: Clickable(new DemoMessage.ToggleLoadingButton())))
                                                    : button(new ButtonOptions("Toggle loading", Common: Clickable(new DemoMessage.ToggleLoadingButton())))
                                            ])
                                    ])),
                            ShowcaseSection(
                                "textInput",
                                "Labeled default and validation states.",
                                div(
                                    [class_("demo-stack")],
                                    [
                                        textInput(
                                            new TextInputOptions(
                                                Name: "display-name",
                                                Value: "Abies UI",
                                                Label: "Display name",
                                                Description: "Static example of the v1 field contract.")),
                                        textInput(
                                            new TextInputOptions(
                                                Name: "api-key",
                                                Value: "",
                                                Label: "API key",
                                                Placeholder: "Enter a token",
                                                ErrorText: "A token is required before saving.",
                                                IsRequired: true))
                                    ])),
                            ShowcaseSection(
                                "select",
                                "Single-select baseline with help and validation messaging.",
                                div(
                                    [class_("demo-stack")],
                                    [
                                        select(
                                            new SelectOptions(
                                                Name: "variant",
                                                SelectedValue: "secondary",
                                                Items:
                                                [
                                                    new SelectItem("primary", "Primary"),
                                                    new SelectItem("secondary", "Secondary"),
                                                    new SelectItem("ghost", "Ghost")
                                                ],
                                                Label: "Default button variant",
                                                Description: "Uses the shared immutable item contract.")),
                                        select(
                                            new SelectOptions(
                                                Name: "environment",
                                                SelectedValue: "",
                                                Items:
                                                [
                                                    new SelectItem("", "Choose an environment"),
                                                    new SelectItem("dev", "Development"),
                                                    new SelectItem("prod", "Production")
                                                ],
                                                Label: "Deployment target",
                                                ErrorText: "Select a target before publishing.",
                                                IsRequired: true))
                                    ])),
                            ShowcaseSection(
                                "spinner",
                                "Inline and block loading indicators with busy semantics.",
                                div(
                                    [class_("demo-stack")],
                                    [
                                        spinner(new SpinnerOptions(Label: "Loading showcase data", Size: SpinnerSize.Large)),
                                        spinner(new SpinnerOptions(Label: "Saving draft", Size: SpinnerSize.Small, IsInline: true))
                                    ])),
                            ShowcaseSection(
                                "toast",
                                "Polite and assertive live-region variants.",
                                div(
                                    [class_("demo-stack")],
                                    [
                                        toast(new ToastOptions(
                                            Title: "Saved",
                                            Message: "Token changes were persisted.",
                                            Variant: ToastVariant.Success,
                                            DismissButton: new ButtonOptions("Dismiss", Variant: ButtonVariant.Ghost))),
                                        toast(new ToastOptions(
                                            Title: "Needs attention",
                                            Message: "Accessibility verification is still pending.",
                                            Variant: ToastVariant.Warning,
                                            DismissButton: new ButtonOptions("Review", Variant: ButtonVariant.Ghost)))
                                    ])),
                            ShowcaseSection(
                                "table",
                                "Static data table plus empty/loading/error contract references.",
                                div(
                                    [class_("demo-stack")],
                                    [
                                        table(
                                            BuildComponentTable(model)),
                                        p([class_("demo-note")], [text("Rows are selectable with click, Enter, or Space. Status sort toggles with the header action. Full grid navigation remains deferred.")])
                                    ])),
                            ShowcaseSection(
                                "modal",
                                "Basic open/close shell using external MVU state.",
                                div(
                                    [class_("demo-stack")],
                                    [
                                        button(
                                            ModalToggleButton(model.IsModalOpen)),
                                        modal(
                                            new ModalOptions(
                                                IsOpen: model.IsModalOpen,
                                                Title: "Demo modal",
                                                Content: div(
                                                    [class_("demo-stack")],
                                                    [
                                                        p([], [text("Press Escape to close this modal. Focus returns to the trigger button.")])
                                                    ]),
                                                Description: "The demo owns open state so the component stays pure.",
                                                CloseButton: new ButtonOptions("Close", Variant: ButtonVariant.Ghost),
                                                AutoFocusCloseButton: true,
                                                CloseOnEscape: true,
                                                OnEscape: new DemoMessage.CloseModal(),
                                                OnKeyDownFallback: new DemoMessage.Noop(),
                                                OnCloseButton: new DemoMessage.CloseModal(),
                                                FocusReturnTargetId: "modal-trigger-button",
                                                Footer:
                                                [
                                                    button(new ButtonOptions("Done", Variant: ButtonVariant.Secondary, Common: Clickable(new DemoMessage.CloseModal())))
                                                ]))
                                    ])),
                            ShowcaseSection(
                                "stack (vertical)",
                                "Arranges children in a vertical flex column with configurable gap.",
                                stack(new StackOptions(
                                    Children:
                                    [
                                        p([], [text("Item 1")]),
                                        p([], [text("Item 2")]),
                                        p([], [text("Item 3")])
                                    ],
                                    Gap: StackGap.Gap2))),
                            ShowcaseSection(
                                "stack (horizontal)",
                                "Arranges children in a horizontal flex row.",
                                stack(new StackOptions(
                                    Children:
                                    [
                                        p([], [text("Item A")]),
                                        p([], [text("Item B")]),
                                        p([], [text("Item C")])
                                    ],
                                    Direction: StackDirection.Horizontal,
                                    Gap: StackGap.Gap3))),
                            ShowcaseSection(
                                "card",
                                "Surface container with configurable elevation and padding.",
                                card(new CardOptions(
                                    Children:
                                    [
                                        p([], [text("This is card content")])
                                    ]))),
                            ShowcaseSection(
                                "divider (plain)",
                                "Horizontal rule between content sections.",
                                divider(new DividerOptions())),
                            ShowcaseSection(
                                "divider (labeled)",
                                "Horizontal rule with an inline section label.",
                                divider(new DividerOptions(Label: "Section break"))),
                            ShowcaseSection(
                                "progressBar (determinate)",
                                "Shows task completion with a fill bar — increment with the button.",
                                div(
                                    [class_("demo-stack")],
                                    [
                                        progressBar(new ProgressBarOptions(
                                            Label: "Upload progress",
                                            Value: model.ProgressBarValue,
                                            ShowValue: true)),
                                        button(new ButtonOptions(
                                            "Add 10 %",
                                            Variant: ButtonVariant.Secondary,
                                            Common: Clickable(new DemoMessage.IncrementProgress())))
                                    ])),
                            ShowcaseSection(
                                "progressBar (indeterminate)",
                                "Indeterminate bar for operations with an unknown duration.",
                                progressBar(new ProgressBarOptions(
                                    Label: "Loading data",
                                    Value: null))),
                            ShowcaseSection(
                                "alert (info)",
                                "Informational in-page alert with optional title.",
                                alert(new AlertOptions(
                                    Message: "Your changes have been saved successfully.",
                                    Title: "Saved",
                                    Variant: AlertVariant.Info))),
                            ShowcaseSection(
                                "alert (danger, live)",
                                "Danger live-region alert with toggle control.",
                                div(
                                    [class_("demo-stack")],
                                    [
                                        button(new ButtonOptions(
                                            model.IsAlertVisible ? "Dismiss alert" : "Show alert",
                                            Variant: ButtonVariant.Secondary,
                                            Common: Clickable(new DemoMessage.ToggleAlert()))),
                                        model.IsAlertVisible
                                            ? alert(new AlertOptions(
                                                Message: "Token is invalid. Please check your credentials.",
                                                Variant: AlertVariant.Danger,
                                                IsLive: true))
                                            : new Empty()
                                    ])),
                            ShowcaseSection(
                                "skeleton (text lines)",
                                "Multi-line text placeholder for article or list content.",
                                skeleton(new SkeletonOptions(
                                    Shape: SkeletonShape.Text,
                                    Lines: 3,
                                    Label: "Loading article"))),
                            ShowcaseSection(
                                "skeleton (avatar)",
                                "Circular avatar placeholder for user profile loading states.",
                                skeleton(new SkeletonOptions(
                                    Shape: SkeletonShape.Avatar,
                                    Label: "Loading avatar")))
                        ])
                ]));

    private static TableRichOptions BuildComponentTable(DemoModel model)
    {
        var orderedRows = model.IsStatusSortAscending
            ? new (string[] Cells, int SourceIndex)[]
            {
                (new[] { "button", "ready", "Variants and loading state" }, 0),
                (new[] { "modal", "scaffolded", "Focus trap deferred" }, 1),
                (new[] { "toast", "scaffolded", "Dismiss wiring deferred" }, 2)
            }
            : new (string[] Cells, int SourceIndex)[]
            {
                (new[] { "modal", "scaffolded", "Focus trap deferred" }, 1),
                (new[] { "toast", "scaffolded", "Dismiss wiring deferred" }, 2),
                (new[] { "button", "ready", "Variants and loading state" }, 0)
            };

        var rowStates = orderedRows
            .Select(row => new TableRowState(
                Cells: row.Cells,
                IsSelected: model.SelectedTableRowIndex == row.SourceIndex,
                SourceIndex: row.SourceIndex,
                Common: ClickableRow(new DemoMessage.SelectTableRow(row.SourceIndex))))
            .ToArray();

        return new TableRichOptions(
            ColumnStates:
            [
                new TableColumnState("component", "Component", IsSortable: true, SortDirection: TableSortDirection.Ascending),
                new TableColumnState(
                    "status",
                    "Status",
                    IsSortable: true,
                    SortDirection: model.IsStatusSortAscending ? TableSortDirection.Ascending : TableSortDirection.Descending,
                    SortButtonAriaLabel: "Toggle sort for implementation status",
                    SortButtonCommon: Clickable(new DemoMessage.ToggleStatusSort())),
                new TableColumnState("notes", "Notes")
            ],
            RowStates: rowStates,
            Caption: "Phase 1 component status",
            SelectedRowIndex: null,
            OnRowClick: new DemoMessage.Noop(),
            OnRowKeyDown: new DemoMessage.Noop());
    }

    private static Node ShowcaseSection(string title, string summary, Node content)
        => section(
            [class_("demo-card")],
            [
                div(
                    [class_("demo-card__header")],
                    [
                        p([class_("demo-card__eyebrow")], [text(title)]),
                        p([class_("demo-card__summary")], [text(summary)])
                    ]),
                content
            ]);

    private static ButtonOptions ModalToggleButton(bool isModalOpen)
        => new(
            isModalOpen ? "Hide modal" : "Show modal",
            Variant: ButtonVariant.Secondary,
            Common: new UiCommonOptions(
                Id: "modal-trigger-button",
                Attributes:
                [
                    onclick(new DemoMessage.ToggleModal()),
                    .. (isModalOpen ? [] : new[] { autofocus() })
                ]));

    private static UiCommonOptions Clickable(Message message)
        => new(Attributes: [onclick(message)]);

    private static UiCommonOptions ClickableRow(Message message)
        => new(Attributes: [onclick(message), onkeydown(message)]);
}
