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

        // Focus trap is handled by abies-ui.js (MutationObserver on role="dialog"). See issue #166.
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

        selectAttributes.AddRange(BuildOptionalAttribute(isDisabled || isReadOnly, () => disabled()));
        selectAttributes.AddRange(BuildOptionalAttribute(options.IsRequired, () => required()));
        selectAttributes.AddRange(BuildOptionalAttribute(HasText(describedBy), () => ariaDescribedby(describedBy!)));
        selectAttributes.AddRange(BuildOptionalAttribute(HasText(options.ErrorText), () => attribute("aria-invalid", "true")));
        selectAttributes.AddRange(BuildOptionalAttribute(isReadOnly, () => attribute("aria-readonly", "true")));
        selectAttributes.AddRange(BuildOptionalAttribute(isReadOnly, () => attribute("data-readonly", "true")));
        selectAttributes.AddRange(BuildOptionalAttribute(HasText(options.Label) is false, () => ariaLabel(options.AriaLabel ?? options.Name)));

        // When readOnly: render as disabled (locks interaction) + hidden input (preserves submitted value).
        // Native <select> has no readonly mode (#152); disabled + hidden input is the correct workaround.
        var controlNode = isReadOnly
            ? div(
                [class_("abies-ui-select__readonly-wrap")],
                [
                    element("select", [.. selectAttributes], children),
                    element("input", [attribute("type", "hidden"), name(options.Name), value(options.SelectedValue ?? string.Empty)], [])
                ])
            : element("select", [.. selectAttributes], children);

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
                control: controlNode,
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

    // ── Phase 2 Layout & Feedback components ──────────────────────────────────

    /// <summary>
    /// Creates a stack layout component that arranges children in a flex container.
    /// </summary>
    /// <param name="options">Immutable stack options.</param>
    /// <returns>A flex-stack container node.</returns>
    public static Node stack(StackOptions options)
    {
        var gapCss = ToStackGapCss(options.Gap);
        return div(
            BuildElementAttributes(
                BuildClassName(
                    "abies-ui-stack",
                    $"abies-ui-stack--{ToStackDirectionCss(options.Direction)}",
                    gapCss is not null ? $"abies-ui-stack--gap-{gapCss}" : null,
                    $"abies-ui-stack--align-{ToStackAlignCss(options.Align)}",
                    $"abies-ui-stack--justify-{ToStackJustifyCss(options.Justify)}"),
                options.Common),
            options.Children);
    }

    /// <summary>
    /// Creates a card layout component with configurable elevation and padding.
    /// </summary>
    /// <param name="options">Immutable card options.</param>
    /// <returns>A card container node.</returns>
    public static Node card(CardOptions options)
        => div(
            BuildElementAttributes(
                BuildClassName(
                    "abies-ui-card",
                    $"abies-ui-card--elevation-{ToCardElevationCss(options.Elevation)}",
                    $"abies-ui-card--pad-{ToCardPaddingCss(options.Padding)}"),
                options.Common),
            options.Children);

    /// <summary>
    /// Creates a divider component — a plain rule or a labeled section separator.
    /// </summary>
    /// <param name="options">Immutable divider options.</param>
    /// <returns>A divider node.</returns>
    public static Node divider(DividerOptions options)
    {
        var orientationClass = $"abies-ui-divider--{ToDividerOrientationCss(options.Orientation)}";

        if (HasText(options.Label))
        {
            return div(
                BuildElementAttributes(
                    BuildClassName("abies-ui-divider", orientationClass, "abies-ui-divider--labeled"),
                    options.Common),
                [
                    element("hr", [role("presentation"), attribute("aria-hidden", "true")], []),
                    span([class_("abies-ui-divider__label")], [text(options.Label!)]),
                    element("hr", [role("presentation"), attribute("aria-hidden", "true")], [])
                ]);
        }

        return element(
            "hr",
            BuildElementAttributes(
                BuildClassName("abies-ui-divider", orientationClass),
                options.Common,
                role("separator")),
            []);
    }

    /// <summary>
    /// Creates a CSS grid layout component.
    /// </summary>
    /// <param name="options">Immutable grid options.</param>
    /// <returns>A CSS-grid container node.</returns>
    public static Node grid(GridOptions options)
        => div(
            BuildElementAttributes(
                BuildClassName(
                    "abies-ui-grid",
                    $"abies-ui-grid--cols-{options.Columns}",
                    options.Gap > 0 ? $"abies-ui-grid--gap-{options.Gap}" : null),
                options.Common),
            options.Children);

    /// <summary>
    /// Creates a progress bar feedback component, supporting both determinate and indeterminate states.
    /// </summary>
    /// <param name="options">Immutable progress bar options.</param>
    /// <returns>A progress bar node.</returns>
    public static Node progressBar(ProgressBarOptions options)
    {
        var isDeterminate = options.Value is not null;
        var percent = isDeterminate
            ? FormatProgressPercent(options.Value!.Value, options.Min, options.Max)
            : null;

        var trackAttrs = new List<Attribute>
        {
            class_("abies-ui-progress-bar__track"),
            role("progressbar"),
            attribute("aria-valuemin", FormatDouble(options.Min)),
            attribute("aria-valuemax", FormatDouble(options.Max))
        };

        trackAttrs.AddRange(BuildOptionalAttribute(isDeterminate, () => attribute("aria-valuenow", FormatDouble(options.Value!.Value))));

        var accessibleName = HasText(options.Label) ? options.Label : "Progress";
        trackAttrs.Add(ariaLabel(accessibleName));

        var fillAttrs = new List<Attribute> { class_("abies-ui-progress-bar__fill") };
        fillAttrs.AddRange(BuildOptionalAttribute(isDeterminate, () => style($"width:{percent}%")));

        return div(
            BuildElementAttributes(
                BuildClassName(
                    "abies-ui-progress-bar",
                    !isDeterminate ? "abies-ui-progress-bar--indeterminate" : null),
                options.Common),
            [
                .. BuildOptionalNode(options.ShowLabel, () => span([class_("abies-ui-progress-bar__label")], [text(options.Label)])),
                div([.. trackAttrs], [div([.. fillAttrs], [])]),
                .. BuildOptionalNode(options.ShowValue && isDeterminate, () => span([class_("abies-ui-progress-bar__value")], [text($"{percent}%")]))
            ]);
    }

    /// <summary>
    /// Creates an alert feedback component with semantic live-region support.
    /// </summary>
    /// <param name="options">Immutable alert options.</param>
    /// <returns>An alert node.</returns>
    public static Node alert(AlertOptions options)
    {
        var roleValue = options.IsLive ? "alert" : "status";
        var liveValue = options.IsLive ? "assertive" : "polite";

        return div(
            BuildElementAttributes(
                BuildClassName("abies-ui-alert", $"abies-ui-alert--{ToAlertVariantCss(options.Variant)}"),
                options.Common,
                role(roleValue),
                ariaLive(liveValue),
                attribute("aria-atomic", "true")),
            [
                .. BuildOptionalNode(HasText(options.Icon), () => span([class_("abies-ui-alert__icon"), attribute("aria-hidden", "true")], [text(options.Icon!)])),
                div(
                    [class_("abies-ui-alert__content")],
                    [
                        .. BuildOptionalNode(HasText(options.Title), () => div([class_("abies-ui-alert__title")], [text(options.Title!)])),
                        div([class_("abies-ui-alert__message")], [text(options.Message)])
                    ])
            ]);
    }

    /// <summary>
    /// Creates a skeleton loading-state placeholder component.
    /// </summary>
    /// <param name="options">Immutable skeleton options.</param>
    /// <returns>A skeleton placeholder node.</returns>
    public static Node skeleton(SkeletonOptions options)
    {
        var shapeClass = $"abies-ui-skeleton--{ToSkeletonShapeCss(options.Shape)}";
        var styleValue = BuildSkeletonStyle(options.Width, options.Height);

        var skeletonAttrs = new List<Attribute>
        {
            attribute("aria-busy", "true"),
            ariaLabel(options.Label)
        };

        skeletonAttrs.AddRange(BuildOptionalAttribute(HasText(styleValue), () => style(styleValue!)));

        var children = options.Shape is SkeletonShape.Text && options.Lines > 1
            ? Enumerable.Range(0, options.Lines)
                .Select(_ => (Node)div([class_("abies-ui-skeleton abies-ui-skeleton--text-line")], []))
                .ToArray()
            : Array.Empty<Node>();

        return div(
            BuildElementAttributes(
                BuildClassName("abies-ui-skeleton", shapeClass),
                options.Common,
                [.. skeletonAttrs]),
            children);
    }

    private static string ToStackDirectionCss(StackDirection direction)
        => direction switch
        {
            StackDirection.Horizontal => "horizontal",
            _ => "vertical"
        };

    private static string? ToStackGapCss(StackGap gap)
        => gap switch
        {
            StackGap.Gap1 => "1",
            StackGap.Gap2 => "2",
            StackGap.Gap3 => "3",
            StackGap.Gap4 => "4",
            StackGap.Gap5 => "5",
            StackGap.Gap6 => "6",
            _ => null
        };

    private static string ToStackAlignCss(StackAlign align)
        => align switch
        {
            StackAlign.Start => "start",
            StackAlign.Center => "center",
            StackAlign.End => "end",
            StackAlign.Baseline => "baseline",
            _ => "stretch"
        };

    private static string ToStackJustifyCss(StackJustify justify)
        => justify switch
        {
            StackJustify.Center => "center",
            StackJustify.End => "end",
            StackJustify.SpaceBetween => "space-between",
            StackJustify.SpaceAround => "space-around",
            _ => "start"
        };

    private static string ToCardElevationCss(CardElevation elevation)
        => elevation switch
        {
            CardElevation.Low => "low",
            CardElevation.Medium => "medium",
            CardElevation.High => "high",
            _ => "none"
        };

    private static string ToCardPaddingCss(CardPadding padding)
        => padding switch
        {
            CardPadding.Sm => "sm",
            CardPadding.Md => "md",
            CardPadding.Lg => "lg",
            _ => "none"
        };

    private static string ToDividerOrientationCss(DividerOrientation orientation)
        => orientation switch
        {
            DividerOrientation.Vertical => "vertical",
            _ => "horizontal"
        };

    private static string ToAlertVariantCss(AlertVariant variant)
        => variant switch
        {
            AlertVariant.Success => "success",
            AlertVariant.Warning => "warning",
            AlertVariant.Danger => "danger",
            _ => "info"
        };

    private static string ToSkeletonShapeCss(SkeletonShape shape)
        => shape switch
        {
            SkeletonShape.Heading => "heading",
            SkeletonShape.Avatar => "avatar",
            SkeletonShape.Rectangle => "rectangle",
            SkeletonShape.Circle => "circle",
            _ => "text"
        };

    private static string FormatDouble(double value)
        => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static string FormatProgressPercent(double value, double min, double max)
    {
        var range = max - min;
        var rawPercent = range > 0.0 ? (value - min) / range * 100.0 : 0.0;
        var clamped = Math.Clamp(rawPercent, 0.0, 100.0);
        return clamped % 1.0 == 0.0 ? $"{clamped:F0}" : $"{clamped:F1}";
    }

    private static string? BuildSkeletonStyle(string? width, string? height)
    {
        var parts = new List<string>(2);
        if (HasText(width)) parts.Add($"width:{width}");
        if (HasText(height)) parts.Add($"height:{height}");
        return parts.Count > 0 ? string.Join(";", parts) : null;
    }

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

// ── Phase 2 Layout options ─────────────────────────────────────────────────

/// <summary>
/// Immutable options for the stack layout component.
/// </summary>
public sealed record StackOptions(
    Node[] Children,
    StackDirection Direction = StackDirection.Vertical,
    StackGap Gap = StackGap.Gap3,
    StackAlign Align = StackAlign.Stretch,
    StackJustify Justify = StackJustify.Start,
    UiCommonOptions? Common = null);

/// <summary>Direction of the stack's main flex axis.</summary>
public enum StackDirection
{
    Vertical,
    Horizontal
}

/// <summary>Gap token index for the stack component — maps to <c>--abies-ui-space-N</c>.</summary>
public enum StackGap
{
    None,
    Gap1,
    Gap2,
    Gap3,
    Gap4,
    Gap5,
    Gap6
}

/// <summary>Cross-axis alignment values for the stack component.</summary>
public enum StackAlign
{
    Stretch,
    Start,
    Center,
    End,
    Baseline
}

/// <summary>Main-axis justification values for the stack component.</summary>
public enum StackJustify
{
    Start,
    Center,
    End,
    SpaceBetween,
    SpaceAround
}

/// <summary>
/// Immutable options for the card layout component.
/// </summary>
public sealed record CardOptions(
    Node[] Children,
    CardElevation Elevation = CardElevation.Low,
    CardPadding Padding = CardPadding.Md,
    UiCommonOptions? Common = null);

/// <summary>Shadow elevation presets for the card component.</summary>
public enum CardElevation
{
    None,
    Low,
    Medium,
    High
}

/// <summary>Padding presets for the card component.</summary>
public enum CardPadding
{
    None,
    Sm,
    Md,
    Lg
}

/// <summary>
/// Immutable options for the divider component.
/// </summary>
public sealed record DividerOptions(
    string? Label = null,
    DividerOrientation Orientation = DividerOrientation.Horizontal,
    UiCommonOptions? Common = null);

/// <summary>Orientation of the divider rule.</summary>
public enum DividerOrientation
{
    Horizontal,
    Vertical
}

/// <summary>
/// Immutable options for the grid layout component.
/// </summary>
public sealed record GridOptions(
    Node[] Children,
    int Columns = 12,
    int Gap = 4,
    UiCommonOptions? Common = null);

// ── Phase 2 Feedback options ───────────────────────────────────────────────

/// <summary>
/// Immutable options for the progress bar feedback component.
/// </summary>
public sealed record ProgressBarOptions(
    string Label,
    double? Value = null,
    double Min = 0,
    double Max = 100,
    bool ShowLabel = true,
    bool ShowValue = false,
    UiCommonOptions? Common = null);

/// <summary>
/// Immutable options for the alert feedback component.
/// </summary>
public sealed record AlertOptions(
    string Message,
    AlertVariant Variant = AlertVariant.Info,
    string? Title = null,
    bool IsLive = false,
    string? Icon = null,
    UiCommonOptions? Common = null);

/// <summary>Semantic variant for the alert component.</summary>
public enum AlertVariant
{
    Info,
    Success,
    Warning,
    Danger
}

/// <summary>
/// Immutable options for the skeleton loading-state feedback component.
/// </summary>
public sealed record SkeletonOptions(
    SkeletonShape Shape = SkeletonShape.Text,
    string? Width = null,
    string? Height = null,
    string Label = "Loading",
    int Lines = 1,
    UiCommonOptions? Common = null);

/// <summary>Visual shape preset for the skeleton component.</summary>
public enum SkeletonShape
{
    Text,
    Heading,
    Avatar,
    Rectangle,
    Circle
}
