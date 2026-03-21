using Picea.Abies.DOM;
using Picea.Abies.Html;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Events;
using Attribute = Picea.Abies.DOM.Attribute;

namespace Picea.Abies.UI;

/// <summary>
/// Provides baseline Abies UI components for issue #152 Phase 1 kickoff.
/// </summary>
public static class Components
{
    #pragma warning disable IDE1006

    /// <summary>
    /// Creates a button component.
    /// </summary>
    /// <param name="options">Immutable button options.</param>
    /// <returns>A button node.</returns>
    public static Node button(ButtonOptions options)
        => RenderButton(options.Label, options.Type, options.Variant, options.AriaLabel, options.Common, isDisabled: false, isLoading: false, loadingText: null);

    public static Node disabledButton(ButtonOptions options)
        => RenderButton(options.Label, options.Type, options.Variant, options.AriaLabel, options.Common, isDisabled: true, isLoading: false, loadingText: null);

    public static Node loadingButton(LoadingButtonOptions options)
        => RenderButton(options.Label, options.Type, options.Variant, options.AriaLabel, options.Common, isDisabled: true, isLoading: true, loadingText: options.LoadingText);

    /// <summary>
    /// Creates a text input component.
    /// </summary>
    /// <param name="options">Immutable text input options.</param>
    /// <returns>An input node.</returns>
    public static Node textInput(TextInputOptions options)
        => RenderTextInput(options, isDisabled: false, isReadOnly: false);

    public static Node disabledTextInput(TextInputOptions options)
        => RenderTextInput(options, isDisabled: true, isReadOnly: false);

    public static Node readOnlyTextInput(TextInputOptions options)
        => RenderTextInput(options, isDisabled: false, isReadOnly: true);

    /// <summary>
    /// Creates a select component.
    /// </summary>
    /// <param name="options">Immutable select options.</param>
    /// <returns>A select node.</returns>
    public static Node select(SelectOptions options)
        => RenderSelect(options, isDisabled: false, isReadOnly: false);

    public static Node disabledSelect(SelectOptions options)
        => RenderSelect(options, isDisabled: true, isReadOnly: false);

    public static Node readOnlySelect(SelectOptions options)
        => RenderSelect(options, isDisabled: false, isReadOnly: true);

    /// <summary>
    /// Creates a spinner component.
    /// </summary>
    /// <param name="options">Immutable spinner options.</param>
    /// <returns>A spinner node.</returns>
    public static Node spinner(SpinnerOptions options)
    {
        var accessibleLabel = HasText(options.ScreenReaderLabelOverride)
            ? options.ScreenReaderLabelOverride!
            : options.Label;

        return element(
            "div",
            BuildElementAttributes(
                BuildClassName(
                    "abies-ui-spinner",
                    $"abies-ui-spinner--{ToSpinnerSizeCss(options.Size)}",
                    options.IsInline ? "abies-ui-spinner--inline" : "abies-ui-spinner--block"),
                options.Common,
                role("status"),
                ariaLive("polite"),
                attribute("aria-busy", "true"),
                ariaLabel(accessibleLabel)),
            [span([class_("abies-ui-spinner__label")], [text(options.Label)])]);
    }

    /// <summary>
    /// Creates a toast skeleton component for Phase 1.
    /// </summary>
    /// <param name="options">Immutable toast options.</param>
    /// <returns>A toast node.</returns>
    public static Node toast(ToastOptions options)
    {
        var liveMode = options.LiveMode ?? (RequiresAssertiveLiveRegion(options.Variant) ? ToastLiveMode.Assertive : ToastLiveMode.Polite);
        var roleValue = liveMode is ToastLiveMode.Assertive ? "alert" : "status";
        var dismissButton = options.DismissButton is null ? null : ApplyToastDismissVariant(options.DismissButton);

        return div(
            BuildElementAttributes(
                BuildClassName("abies-ui-toast", $"abies-ui-toast--{ToToastVariantCss(options.Variant)}"),
                options.Common,
                role(roleValue),
                ariaLive(ToToastLiveModeValue(liveMode)),
                attribute("aria-atomic", "true")),
            [
                .. BuildOptionalNode(HasText(options.Title), () => div([class_("abies-ui-toast__title")], [text(options.Title!)])),
                div([class_("abies-ui-toast__message")], [text(options.Message)]),
                .. BuildOptionalNode(dismissButton is not null, () => div([class_("abies-ui-toast__actions")], [button(dismissButton!)]))
            ]);
    }

    /// <summary>
    /// Creates a modal skeleton component for Phase 1.
    /// </summary>
    /// <param name="options">Immutable modal options.</param>
    /// <returns>A modal node or an empty placeholder when closed.</returns>
    public static Node modal(ModalOptions options)
    {
        if (options.CloseOnEscape && options.OnEscape is null)
        {
            throw new InvalidOperationException("ModalOptions.CloseOnEscape requires ModalOptions.OnEscape.");
        }

        if (options.CloseOnEscape && options.OnKeyDownFallback is null)
        {
            throw new InvalidOperationException("ModalOptions.CloseOnEscape requires ModalOptions.OnKeyDownFallback so non-Escape key events are handled explicitly.");
        }

        var dialogId = ResolveElementId(options.Common, "abies-ui-modal");
        var titleId = $"{dialogId}-title";
        var descriptionId = BuildOptionalId(options.Description, dialogId, "description");
        var closeButton = ApplyCloseRequest(ApplyAutoFocus(options.CloseButton, options.AutoFocusCloseButton), options.OnCloseButton);
        var keyDownHandler = ResolveModalKeyDownHandler(options);
        var modalAttributes = new List<Attribute>
        {
            id(dialogId),
            role("dialog"),
            attribute("aria-modal", "true"),
            tabindex("-1")
        };

        modalAttributes.AddRange(BuildOptionalAttribute(HasText(options.AriaLabel), () => ariaLabel(options.AriaLabel!)));
        modalAttributes.AddRange(BuildOptionalAttribute(HasText(options.AriaLabel) is false, () => ariaLabelledby(titleId)));
        modalAttributes.AddRange(BuildOptionalAttribute(HasText(descriptionId), () => ariaDescribedby(descriptionId!)));
        modalAttributes.AddRange(BuildOptionalAttribute(keyDownHandler is not null, () => onkeydown(keyDownHandler!)));
        modalAttributes.AddRange(BuildOptionalAttribute(HasText(options.FocusReturnTargetId), () => attribute("data-focus-return", options.FocusReturnTargetId!)));

        // TODO(issue-152): Add focus trap, escape handling, and backdrop click policy in a follow-up implementation pass.
        return options.IsOpen
            ? div(
                BuildElementAttributes(
                    BuildClassName("abies-ui-modal", "abies-ui-modal--open"),
                    options.Common,
                    [.. modalAttributes]),
                [
                    div(
                        [class_("abies-ui-modal__surface")],
                        [
                            div(
                                [class_("abies-ui-modal__header")],
                                [
                                    h2([id(titleId), class_("abies-ui-modal__title")], [text(options.Title)]),
                                    .. BuildOptionalNode(closeButton is not null, () => div([class_("abies-ui-modal__header-action")], [button(closeButton!)]))
                                ]),
                            .. BuildOptionalNode(HasText(options.Description), () => div([id(descriptionId!), class_("abies-ui-modal__description")], [text(options.Description!)])),
                            div([class_("abies-ui-modal__body")], [options.Content]),
                            .. BuildOptionalNode(options.Footer is { Length: > 0 }, () => div([class_("abies-ui-modal__footer")], [.. options.Footer!]))
                        ])
                ])
            : new Empty();
    }

    /// <summary>
    /// Creates a table skeleton component for Phase 1.
    /// </summary>
    /// <param name="options">Immutable simple table options.</param>
    /// <returns>A table node.</returns>
    public static Node table(TableSimpleOptions options)
    {
        var columnStates = options.Columns.Select(label => new TableColumnState(label, label)).ToArray();
        var rowStates = options.Rows.Select(row => new TableRowState(row)).ToArray();

        return RenderTable(
            columnStates,
            rowStates,
            options.Caption,
            options.EmptyStateText,
            options.LoadingText,
            options.ErrorText,
            options.IsBusy,
            options.AriaLabel,
            options.SelectedRowIndex,
            options.OnRowClick,
            options.OnRowKeyDown,
            options.Common);
    }

    /// <summary>
    /// Creates a rich table skeleton component for Phase 1.
    /// </summary>
    /// <param name="options">Immutable rich table options.</param>
    /// <returns>A table node.</returns>
    public static Node table(TableRichOptions options)
        => RenderTable(
            options.ColumnStates,
            options.RowStates,
            options.Caption,
            options.EmptyStateText,
            options.LoadingText,
            options.ErrorText,
            options.IsBusy,
            options.AriaLabel,
            options.SelectedRowIndex,
            options.OnRowClick,
            options.OnRowKeyDown,
            options.Common);

    private static Node RenderTable(
        TableColumnState[] columnStates,
        TableRowState[] rowStates,
        string? caption,
        string emptyStateText,
        string loadingText,
        string? errorText,
        bool isBusy,
        string? ariaLabelValue,
        int? selectedRowIndex,
        Message? onRowClick,
        Message? onRowKeyDown,
        UiCommonOptions? common)
    {
        var tableId = ResolveElementId(common, "abies-ui-table");
        var headerCells = columnStates.Select(BuildHeaderCell).ToArray();
        var contentRows = rowStates.Select((row, index) => BuildBodyRow(row, index, selectedRowIndex, onRowClick, onRowKeyDown)).ToArray();
        var tableAttributes = new List<Attribute>
        {
            id(tableId)
        };

        tableAttributes.AddRange(BuildOptionalAttribute(isBusy, () => attribute("aria-busy", "true")));
        tableAttributes.AddRange(BuildOptionalAttribute(HasText(caption) is false && HasText(ariaLabelValue), () => ariaLabel(ariaLabelValue!)));

        var bodyRows = contentRows.Length > 0
            ? contentRows
            : [tr([class_("abies-ui-table__row")], [td([colspan(columnStates.Length.ToString())], [text(ResolveTableMessage(isBusy, loadingText, errorText, emptyStateText))])])];

        return element(
            "table",
            BuildElementAttributes(
                BuildClassName(
                    "abies-ui-table",
                    isBusy ? "abies-ui-table--busy" : null,
                    HasText(errorText) ? "abies-ui-table--error" : null,
                    rowStates.Length == 0 && isBusy is false && HasText(errorText) is false ? "abies-ui-table--empty" : null),
                common,
                [.. tableAttributes]),
            [
                .. BuildOptionalNode(HasText(caption), () => element("caption", [class_("abies-ui-table__caption")], [text(caption!)])),
                thead([], [tr([], headerCells)]),
                tbody([], bodyRows)
            ]);
    }

    private static string ResolveElementId(UiCommonOptions? common, string fallback)
        => HasText(common?.Id) ? common!.Id! : fallback;

    private static string ResolveFieldControlId(UiCommonOptions? common, string name, string suffix)
        => HasText(common?.Id) ? $"{common!.Id}-{suffix}" : $"{name}-{suffix}";

    private static string? BuildOptionalId(string? value, string prefix, string suffix)
        => HasText(value) ? $"{prefix}-{suffix}" : null;

    private static string BuildClassName(params string?[] tokens)
        => string.Join(" ", tokens.Where(HasText).Select(token => token!.Trim()));

    private static string? JoinTokens(string separator, params string?[] tokens)
    {
        var values = tokens.Where(HasText).Select(token => token!.Trim()).ToArray();
        return values.Length is 0 ? null : string.Join(separator, values);
    }

    private static bool HasText(string? value)
        => string.IsNullOrWhiteSpace(value) is false;

    private static bool ShouldEmitAriaLabel(string label, string? ariaLabelValue)
        => HasText(ariaLabelValue) && string.Equals(label, ariaLabelValue, StringComparison.Ordinal) is false;

    private static bool RequiresAssertiveLiveRegion(ToastVariant variant)
        => variant is ToastVariant.Error || variant is ToastVariant.Warning;

    private static Attribute[] BuildElementAttributes(string baseClass, UiCommonOptions? common, params Attribute[] attributes)
    {
        var hasExplicitId = attributes.Any(attribute => string.Equals(attribute.Name, "id", StringComparison.Ordinal));
        var mergedAttributes = new List<Attribute>
        {
            class_(BuildClassName(baseClass, common?.Class))
        };

        mergedAttributes.AddRange(BuildOptionalAttribute(HasText(common?.Id) && hasExplicitId is false, () => id(common!.Id!)));
        mergedAttributes.AddRange(BuildOptionalAttribute(HasText(common?.Style), () => style(common!.Style!)));
        mergedAttributes.AddRange(BuildOptionalAttribute(HasText(common?.DataTestId), () => attribute("data-testid", common!.DataTestId!)));
        mergedAttributes.AddRange(attributes);

        if (common?.Attributes is { Length: > 0 })
        {
            mergedAttributes.AddRange(common.Attributes);
        }

        return [.. mergedAttributes];
    }

    private static Attribute[] BuildOptionalAttribute(bool condition, Func<Attribute> attributeFactory)
        => condition ? [attributeFactory()] : [];

    private static Node[] BuildOptionalNode(bool condition, Func<Node> nodeFactory)
        => condition ? [nodeFactory()] : [];

    private static Node[] BuildFieldChildren(Node[] fieldLabel, Node control, Node[] description, Node[] validation)
        => [.. fieldLabel, control, .. description, .. validation];

    private static Node RenderButton(string label, ButtonType buttonType, ButtonVariant variant, string? ariaLabelOverride, UiCommonOptions? common, bool isDisabled, bool isLoading, string? loadingText)
    {
        var componentAttributes = new List<Attribute>
        {
            type(ToButtonTypeValue(buttonType)),
            ariaDisabled(isDisabled ? "true" : "false")
        };

        componentAttributes.AddRange(BuildOptionalAttribute(isLoading, () => attribute("aria-busy", "true")));
        componentAttributes.AddRange(BuildOptionalAttribute(ShouldEmitAriaLabel(label, ariaLabelOverride), () => ariaLabel(ariaLabelOverride!)));
        componentAttributes.AddRange(BuildOptionalAttribute(isDisabled, () => disabled()));

        var buttonAttributes = BuildElementAttributes(
            BuildClassName(
                "abies-ui-button",
                $"abies-ui-button--{ToButtonVariantCss(variant)}",
                isDisabled ? "abies-ui-button--disabled" : null,
                isLoading ? "abies-ui-button--loading" : null),
            common,
            [.. componentAttributes]);

        return element(
            "button",
            buttonAttributes,
            [
                span([class_("abies-ui-button__label")], [text(label)]),
                .. BuildOptionalNode(isLoading, () => span([class_("abies-ui-button__status")], [text(loadingText ?? "Loading")]))
            ]);
    }

    private static Node RenderTextInput(TextInputOptions options, bool isDisabled, bool isReadOnly)
    {
        var inputId = ResolveFieldControlId(options.Common, options.Name, "input");
        var descriptionId = BuildOptionalId(options.Description, inputId, "description");
        var errorId = BuildOptionalId(options.ErrorText, inputId, "error");
        var describedBy = JoinTokens(" ", descriptionId, errorId);

        var inputAttributes = new List<Attribute>
        {
            class_("abies-ui-text-input__control"),
            id(inputId),
            type(ToTextInputTypeValue(options.Type)),
            name(options.Name),
            value(options.Value)
        };

        inputAttributes.AddRange(BuildOptionalAttribute(HasText(options.Placeholder), () => placeholder(options.Placeholder!)));
        inputAttributes.AddRange(BuildOptionalAttribute(isDisabled, () => disabled()));
        inputAttributes.AddRange(BuildOptionalAttribute(isReadOnly, () => readonly_()));
        inputAttributes.AddRange(BuildOptionalAttribute(options.IsRequired, () => required()));
        inputAttributes.AddRange(BuildOptionalAttribute(HasText(options.AutoComplete), () => autocomplete(options.AutoComplete!)));
        inputAttributes.AddRange(BuildOptionalAttribute(HasText(describedBy), () => ariaDescribedby(describedBy!)));
        inputAttributes.AddRange(BuildOptionalAttribute(HasText(options.Label) is false, () => ariaLabel(options.AriaLabel ?? options.Name)));
        inputAttributes.AddRange(BuildOptionalAttribute(HasText(options.ErrorText), () => attribute("aria-invalid", "true")));

        return div(
            BuildElementAttributes(
                BuildClassName(
                    "abies-ui-text-input",
                    isDisabled ? "abies-ui-text-input--disabled" : null,
                    isReadOnly ? "abies-ui-text-input--readonly" : null,
                    HasText(options.ErrorText) ? "abies-ui-text-input--error" : null),
                options.Common),
            BuildFieldChildren(
                fieldLabel: BuildOptionalNode(HasText(options.Label), () => element("label", [for_(inputId), class_("abies-ui-text-input__label")], [text(options.Label!)])),
                control: element("input", [.. inputAttributes], []),
                description: BuildOptionalNode(HasText(options.Description), () => div([id(descriptionId!), class_("abies-ui-text-input__description")], [text(options.Description!)])),
                validation: BuildOptionalNode(HasText(options.ErrorText), () => div([id(errorId!), class_("abies-ui-text-input__error")], [text(options.ErrorText!)]))));
    }

    private static Node RenderSelect(SelectOptions options, bool isDisabled, bool isReadOnly)
    {
        var selectId = ResolveFieldControlId(options.Common, options.Name, "select");
        var descriptionId = BuildOptionalId(options.Description, selectId, "description");
        var errorId = BuildOptionalId(options.ErrorText, selectId, "error");
        var describedBy = JoinTokens(" ", descriptionId, errorId);

        var children = options.Items
            .Select(item =>
                item.Value == options.SelectedValue
                    ? element("option", [value(item.Value), selected()], [text(item.Label)])
                    : element("option", [value(item.Value)], [text(item.Label)]))
            .ToArray();

        var selectAttributes = new List<Attribute>
        {
            class_("abies-ui-select__control"),
            id(selectId),
            name(options.Name)
        };

        selectAttributes.AddRange(BuildOptionalAttribute(isDisabled, () => disabled()));
        selectAttributes.AddRange(BuildOptionalAttribute(options.IsRequired, () => required()));
        selectAttributes.AddRange(BuildOptionalAttribute(HasText(describedBy), () => ariaDescribedby(describedBy!)));
        selectAttributes.AddRange(BuildOptionalAttribute(HasText(options.ErrorText), () => attribute("aria-invalid", "true")));
        selectAttributes.AddRange(BuildOptionalAttribute(isReadOnly, () => attribute("aria-readonly", "true")));
        selectAttributes.AddRange(BuildOptionalAttribute(isReadOnly, () => attribute("data-readonly", "true")));
        selectAttributes.AddRange(BuildOptionalAttribute(HasText(options.Label) is false, () => ariaLabel(options.AriaLabel ?? options.Name)));

        // TODO(issue-152): Native select has no true readonly mode. Preserve value semantics now and add interaction locking in a follow-up pass.
        return div(
            BuildElementAttributes(
                BuildClassName(
                    "abies-ui-select",
                    isDisabled ? "abies-ui-select--disabled" : null,
                    isReadOnly ? "abies-ui-select--readonly" : null,
                    HasText(options.ErrorText) ? "abies-ui-select--error" : null),
                options.Common),
            BuildFieldChildren(
                fieldLabel: BuildOptionalNode(HasText(options.Label), () => element("label", [for_(selectId), class_("abies-ui-select__label")], [text(options.Label!)])),
                control: element("select", [.. selectAttributes], children),
                description: BuildOptionalNode(HasText(options.Description), () => div([id(descriptionId!), class_("abies-ui-select__description")], [text(options.Description!)])),
                validation: BuildOptionalNode(HasText(options.ErrorText), () => div([id(errorId!), class_("abies-ui-select__error")], [text(options.ErrorText!)]))));
    }

    private static string ResolveTableMessage(bool isBusy, string loadingText, string? errorText, string emptyStateText)
        => isBusy
            ? loadingText
            : HasText(errorText)
                ? errorText!
                : emptyStateText;

    private static Node BuildHeaderCell(TableColumnState column)
    {
        var sortDirection = ResolveSortDirection(column.SortDirection);
        var headerAttributes = new List<Attribute>
        {
            scope("col")
        };

        headerAttributes.AddRange(BuildOptionalAttribute(column.IsSortable, () => attribute("aria-sort", ToAriaSortValue(sortDirection))));

        return th(
            [.. headerAttributes],
            column.IsSortable
                ?
                [
                    element(
                        "button",
                        BuildElementAttributes(
                            BuildClassName("abies-ui-table__sort-button"),
                            column.SortButtonCommon,
                            type("button"),
                            ariaLabel(ResolveSortButtonAriaLabel(column, sortDirection))),
                        [text(column.Label)])
                ]
                : [text(column.Label)]);
    }

    private static Node BuildBodyRow(TableRowState row, int index, int? selectedRowIndex, Message? onRowClick, Message? onRowKeyDown)
    {
        var requestIndex = row.SourceIndex ?? index;
        var isSelected = row.IsSelected || (selectedRowIndex is int selectedIndex && selectedIndex == requestIndex);
        var rowAttributes = new List<Attribute>();
        var hasRowInteraction = onRowClick is not null || onRowKeyDown is not null;
        var rowIsFocusable = row.IsFocusable || isSelected || hasRowInteraction;

        rowAttributes.AddRange(BuildOptionalAttribute(isSelected, () => ariaSelected("true")));
        rowAttributes.AddRange(BuildOptionalAttribute(rowIsFocusable, () => tabindex("0")));
        rowAttributes.AddRange(BuildOptionalAttribute(onRowClick is not null && HasEventHandler(row.Common?.Attributes, "click") is false, () => onclick(onRowClick!)));
        rowAttributes.AddRange(BuildOptionalAttribute(onRowKeyDown is not null && HasEventHandler(row.Common?.Attributes, "keydown") is false, () => onkeydown(onRowKeyDown!)));

        return tr(
            BuildElementAttributes(
                BuildClassName("abies-ui-table__row", isSelected ? "abies-ui-table__row--selected" : null),
                row.Common,
                [.. rowAttributes]),
            row.Cells.Select(cell => td([], [text(cell)])).ToArray());
    }

    private static bool HasEventHandler(Attribute[]? attributes, string eventName)
        => attributes?.Any(attribute => string.Equals(attribute.Name, $"data-event-{eventName}", StringComparison.Ordinal)) is true;

    private static TableSortDirection ResolveSortDirection(TableSortDirection? sortDirection)
        => sortDirection ?? TableSortDirection.None;

    private static string ToAriaSortValue(TableSortDirection sortDirection)
        => sortDirection switch
        {
            TableSortDirection.Ascending => "ascending",
            TableSortDirection.Descending => "descending",
            TableSortDirection.Other => "other",
            _ => "none"
        };

    private static string ResolveSortButtonAriaLabel(TableColumnState column, TableSortDirection sortDirection)
        => HasText(column.SortButtonAriaLabel)
            ? column.SortButtonAriaLabel!
            : sortDirection switch
            {
                TableSortDirection.Ascending => $"Sort by {column.Label}. Currently sorted ascending.",
                TableSortDirection.Descending => $"Sort by {column.Label}. Currently sorted descending.",
                TableSortDirection.Other => $"Sort by {column.Label}. Custom sort applied.",
                _ => $"Sort by {column.Label}."
            };

    private static string ToButtonTypeValue(ButtonType buttonType)
        => buttonType switch
        {
            ButtonType.Submit => "submit",
            ButtonType.Reset => "reset",
            _ => "button"
        };

    private static string ToButtonVariantCss(ButtonVariant buttonVariant)
        => buttonVariant switch
        {
            ButtonVariant.Secondary => "secondary",
            ButtonVariant.Ghost => "ghost",
            _ => "primary"
        };

    private static string ToTextInputTypeValue(TextInputType inputType)
        => inputType switch
        {
            TextInputType.Email => "email",
            TextInputType.Password => "password",
            TextInputType.Search => "search",
            TextInputType.Tel => "tel",
            TextInputType.Url => "url",
            _ => "text"
        };

    private static string ToSpinnerSizeCss(SpinnerSize spinnerSize)
        => spinnerSize switch
        {
            SpinnerSize.Small => "small",
            SpinnerSize.Large => "large",
            _ => "medium"
        };

    private static string ToToastVariantCss(ToastVariant toastVariant)
        => toastVariant switch
        {
            ToastVariant.Success => "success",
            ToastVariant.Warning => "warning",
            ToastVariant.Error => "error",
            _ => "info"
        };

    private static string ToToastLiveModeValue(ToastLiveMode liveMode)
        => liveMode is ToastLiveMode.Assertive ? "assertive" : "polite";

    private static ButtonOptions ApplyToastDismissVariant(ButtonOptions buttonOptions)
        => buttonOptions.Variant is ButtonVariant.Primary
            ? buttonOptions with { Variant = ButtonVariant.Ghost }
            : buttonOptions;

    private static ButtonOptions? ApplyAutoFocus(ButtonOptions? buttonOptions, bool shouldAutoFocus)
    {
        if (buttonOptions is null || shouldAutoFocus is false)
        {
            return buttonOptions;
        }

        var common = buttonOptions.Common ?? new UiCommonOptions();
        var existingAttributes = common.Attributes ?? [];
        var mergedAttributes = new Attribute[existingAttributes.Length + 1];

        Array.Copy(existingAttributes, mergedAttributes, existingAttributes.Length);
        mergedAttributes[^1] = autofocus();

        return buttonOptions with
        {
            Common = common with
            {
                Attributes = mergedAttributes
            }
        };
    }

    private static ButtonOptions? ApplyCloseRequest(ButtonOptions? buttonOptions, Message? onCloseButton)
    {
        if (buttonOptions is null || onCloseButton is null)
        {
            return buttonOptions;
        }

        var common = buttonOptions.Common ?? new UiCommonOptions();
        var existingAttributes = common.Attributes ?? [];

        if (existingAttributes.Any(attribute => string.Equals(attribute.Name, "data-event-click", StringComparison.Ordinal)))
        {
            return buttonOptions;
        }

        var mergedAttributes = new Attribute[existingAttributes.Length + 1];
        Array.Copy(existingAttributes, mergedAttributes, existingAttributes.Length);
        mergedAttributes[^1] = onclick(onCloseButton);

        return buttonOptions with
        {
            Common = common with
            {
                Attributes = mergedAttributes
            }
        };
    }

    private static Func<KeyEventData?, Message>? ResolveModalKeyDownHandler(ModalOptions options)
    {
        if (options.CloseOnEscape is false)
        {
            return null;
        }

        return eventData => IsEscapeKey(eventData)
            ? options.OnEscape!
            : options.OnKeyDownFallback!;
    }

    private static bool IsEscapeKey(KeyEventData? eventData)
        => string.Equals(eventData?.Key, "Escape", StringComparison.Ordinal);

    #pragma warning restore IDE1006
}

/// <summary>
/// Shared immutable extensibility options for Phase 1 UI components.
/// </summary>
public sealed record UiCommonOptions(
    string? Id = null,
    string? Class = null,
    string? Style = null,
    string? DataTestId = null,
    Attribute[]? Attributes = null);

/// <summary>
/// Immutable options for the button component.
/// </summary>
public sealed record ButtonOptions(
    string Label,
    ButtonType Type = ButtonType.Button,
    ButtonVariant Variant = ButtonVariant.Primary,
    string? AriaLabel = null,
    UiCommonOptions? Common = null);

public enum ButtonType
{
    Button,
    Submit,
    Reset
}

public enum ButtonVariant
{
    Primary,
    Secondary,
    Ghost
}

/// <summary>
/// Immutable options for the loading button component.
/// </summary>
public sealed record LoadingButtonOptions(
    string Label,
    string LoadingText = "Loading",
    ButtonType Type = ButtonType.Button,
    ButtonVariant Variant = ButtonVariant.Primary,
    string? AriaLabel = null,
    UiCommonOptions? Common = null);

/// <summary>
/// Immutable options for the text input component.
/// </summary>
public sealed record TextInputOptions(
    string Name,
    string Value,
    string Placeholder = "",
    TextInputType Type = TextInputType.Text,
    string? Label = null,
    string? Description = null,
    string? ErrorText = null,
    bool IsRequired = false,
    string? AutoComplete = null,
    string? AriaLabel = null,
    UiCommonOptions? Common = null);

public enum TextInputType
{
    Text,
    Email,
    Password,
    Search,
    Tel,
    Url
}

/// <summary>
/// Immutable options for one selectable item.
/// </summary>
public sealed record SelectItem(
    string Value,
    string Label);

/// <summary>
/// Immutable options for the select component.
/// </summary>
public sealed record SelectOptions(
    string Name,
    string SelectedValue,
    SelectItem[] Items,
    string? Label = null,
    string? Description = null,
    string? ErrorText = null,
    bool IsRequired = false,
    string? AriaLabel = null,
    UiCommonOptions? Common = null);

/// <summary>
/// Immutable options for the spinner component.
/// </summary>
public sealed record SpinnerOptions(
    string Label = "Loading",
    string? ScreenReaderLabelOverride = null,
    SpinnerSize Size = SpinnerSize.Medium,
    bool IsInline = false,
    UiCommonOptions? Common = null);

public enum SpinnerSize
{
    Small,
    Medium,
    Large
}

/// <summary>
/// Immutable options for the toast skeleton component.
/// </summary>
public sealed record ToastOptions(
    string Message,
    ToastVariant Variant = ToastVariant.Info,
    string? Title = null,
    ToastLiveMode? LiveMode = null,
    ButtonOptions? DismissButton = null,
    UiCommonOptions? Common = null);

public enum ToastVariant
{
    Info,
    Success,
    Warning,
    Error
}

public enum ToastLiveMode
{
    Polite,
    Assertive
}

/// <summary>
/// Immutable options for the modal skeleton component.
/// </summary>
public sealed record ModalOptions(
    bool IsOpen,
    string Title,
    Node Content,
    string? Description = null,
    string? AriaLabel = null,
    ButtonOptions? CloseButton = null,
    bool AutoFocusCloseButton = false,
    bool CloseOnEscape = false,
    Message? OnEscape = null,
    Message? OnKeyDownFallback = null,
    Message? OnCloseButton = null,
    Node[]? Footer = null,
    // When set, stamps data-focus-return="{id}" on the modal root so the
    // consuming application (not the runtime) can restore focus after close.
    string? FocusReturnTargetId = null,
    UiCommonOptions? Common = null);

/// <summary>
/// Immutable metadata for a table column.
/// </summary>
public sealed record TableColumnState(
    string Key,
    string Label,
    bool IsSortable = false,
    TableSortDirection? SortDirection = null,
    string? SortButtonAriaLabel = null,
    UiCommonOptions? SortButtonCommon = null);

public enum TableSortDirection
{
    None,
    Ascending,
    Descending,
    Other
}

/// <summary>
/// Immutable metadata for a table row.
/// </summary>
public sealed record TableRowState(
    string[] Cells,
    bool IsSelected = false,
    bool IsFocusable = false,
    int? SourceIndex = null,
    UiCommonOptions? Common = null);

/// <summary>
/// Immutable options for the simple table skeleton component.
/// </summary>
public sealed record TableSimpleOptions(
    string[] Columns,
    string[][] Rows,
    string? Caption = null,
    string EmptyStateText = "No rows available",
    string LoadingText = "Loading rows",
    string? ErrorText = null,
    bool IsBusy = false,
    string? AriaLabel = null,
    int? SelectedRowIndex = null,
    Message? OnRowClick = null,
    Message? OnRowKeyDown = null,
    UiCommonOptions? Common = null);

/// <summary>
/// Immutable options for the rich table skeleton component.
/// </summary>
public sealed record TableRichOptions(
    TableColumnState[] ColumnStates,
    TableRowState[] RowStates,
    string? Caption = null,
    string EmptyStateText = "No rows available",
    string LoadingText = "Loading rows",
    string? ErrorText = null,
    bool IsBusy = false,
    string? AriaLabel = null,
    int? SelectedRowIndex = null,
    Message? OnRowClick = null,
    Message? OnRowKeyDown = null,
    UiCommonOptions? Common = null);
