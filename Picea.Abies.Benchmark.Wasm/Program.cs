// =============================================================================
// Picea Abies js-framework-benchmark Implementation
// =============================================================================
// Standard benchmark for comparing JavaScript framework performance.
// See: https://github.com/krausest/js-framework-benchmark
//
// Operations:
// - Create 1,000 / 10,000 rows
// - Append 1,000 rows
// - Update every 10th row
// - Select row (highlight)
// - Swap rows (positions 2 and 999, i.e. indices 1 and 998)
// - Delete row
// - Clear all rows
//
// This implementation uses the Picea kernel's MVU runtime.
// The view function uses lazy memoization for row rendering,
// enabling ReferenceEquals bailout in the diff algorithm for
// unchanged rows.
// =============================================================================

using Picea;
using Picea.Abies;
using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Events;

// =============================================================================
// WASM Entry Point
// =============================================================================

await Picea.Abies.Browser.Runtime.Run<BenchmarkApp, BenchmarkModel, Unit>();

// =============================================================================
// Model
// =============================================================================

/// <summary>
/// A single row in the benchmark table.
/// </summary>
public record Row(int Id, string Label);

/// <summary>
/// The application model (state).
/// </summary>
public record BenchmarkModel(List<Row> Data, int? Selected, int NextId);

// =============================================================================
// Messages
// =============================================================================

/// <summary>Create 1,000 rows (replacing existing).</summary>
public record Create : Message;

/// <summary>Create 10,000 rows (replacing existing).</summary>
public record CreateLots : Message;

/// <summary>Append 1,000 rows to existing.</summary>
public record Append : Message;

/// <summary>Update every 10th row by appending " !!!" to the label.</summary>
public record UpdateEvery10th : Message;

/// <summary>Clear all rows.</summary>
public record Clear : Message;

/// <summary>Swap rows at positions 2 and 999 (1-indexed, i.e. indices 1 and 998).</summary>
public record SwapRows : Message;

/// <summary>Select a row by ID.</summary>
public record Select(int Id) : Message;

/// <summary>Delete a row by ID.</summary>
public record Delete(int Id) : Message;

// =============================================================================
// Benchmark Application
// =============================================================================

/// <summary>
/// The js-framework-benchmark application implementing the Picea MVU pattern.
/// </summary>
public sealed class BenchmarkApp : Program<BenchmarkModel, Unit>
{
    // =========================================================================
    // Label Generation
    // =========================================================================

    private static readonly string[] _adjectives =
    [
        "pretty", "large", "big", "small", "tall", "short", "long", "handsome",
        "plain", "quaint", "clean", "elegant", "easy", "angry", "crazy", "helpful",
        "mushy", "odd", "unsightly", "adorable", "important", "inexpensive",
        "cheap", "expensive", "fancy"
    ];

    private static readonly string[] _colours =
    [
        "red", "yellow", "blue", "green", "pink", "brown", "purple", "brown",
        "white", "black", "orange"
    ];

    private static readonly string[] _nouns =
    [
        "table", "chair", "house", "bbq", "desk", "car", "pony", "cookie",
        "sandwich", "burger", "pizza", "mouse", "keyboard"
    ];

    private static readonly Random _rng = new();

    private static string GenerateLabel() =>
        $"{_adjectives[_rng.Next(_adjectives.Length)]} {_colours[_rng.Next(_colours.Length)]} {_nouns[_rng.Next(_nouns.Length)]}";

    private static List<Row> BuildData(int count, int startId)
    {
        var data = new List<Row>(count);
        for (int i = 0; i < count; i++)
        {
            data.Add(new Row(startId + i, GenerateLabel()));
        }

        return data;
    }

    // =========================================================================
    // Picea Interface
    // =========================================================================

    /// <summary>
    /// Initialize with an empty model.
    /// </summary>
    public static (BenchmarkModel, Command) Initialize(Unit argument) =>
        (new BenchmarkModel([], null, 1), Commands.None);

    /// <summary>
    /// Transition the model based on incoming messages.
    /// </summary>
    public static (BenchmarkModel, Command) Transition(BenchmarkModel model, Message message) =>
        message switch
        {
            Create => (model with
            {
                Data = BuildData(1000, model.NextId),
                NextId = model.NextId + 1000,
                Selected = null
            }, Commands.None),

            CreateLots => (model with
            {
                Data = BuildData(10000, model.NextId),
                NextId = model.NextId + 10000,
                Selected = null
            }, Commands.None),

            Append => (model with
            {
                Data = [.. model.Data, .. BuildData(1000, model.NextId)],
                NextId = model.NextId + 1000
            }, Commands.None),

            UpdateEvery10th => (model with { Data = UpdateEvery10thRow(model.Data) }, Commands.None),

            Clear => (model with { Data = [], Selected = null }, Commands.None),

            SwapRows => (model with { Data = SwapRowsAtPositions(model.Data) }, Commands.None),

            Select s => (model with { Selected = s.Id }, Commands.None),

            Delete d => (model with { Data = model.Data.Where(r => r.Id != d.Id).ToList() }, Commands.None),

            _ => (model, Commands.None)
        };

    private static List<Row> UpdateEvery10thRow(List<Row> data)
    {
        var result = new List<Row>(data.Count);
        for (int i = 0; i < data.Count; i++)
        {
            if (i % 10 == 0)
            {
                var row = data[i];
                result.Add(row with { Label = row.Label + " !!!" });
            }
            else
            {
                result.Add(data[i]);
            }
        }

        return result;
    }

    private static List<Row> SwapRowsAtPositions(List<Row> data)
    {
        if (data.Count < 999)
            return data;

        var result = new List<Row>(data);
        (result[1], result[998]) = (result[998], result[1]);
        return result;
    }

    // =========================================================================
    // View
    // =========================================================================

    /// <summary>
    /// Render the benchmark UI.
    /// </summary>
    public static Document View(BenchmarkModel model) =>
        new("Abies keyed",
            div([class_("container")],
            [
                Jumbotron(),
                table([class_("table table-hover table-striped test-data")],
                [
                    tbody([id("tbody")],
                    [
                        .. model.Data.ConvertAll(row => MemoizedRow(row, model.Selected == row.Id))
                    ])
                ]),
                span([class_("preloadicon glyphicon glyphicon-remove"), ariaHidden("true")], [])
            ]));

    // =========================================================================
    // Lazy Row ID Cache
    // =========================================================================
    // Lazy cache identifiers are allocated once per unique row ID and reused
    // across render cycles. This avoids per-render string interpolation
    // allocations ($"lazy-row-{id}") that could skew benchmark results away
    // from DOM/diff costs.

    private static readonly Dictionary<int, string> _lazyRowIdCache = new();

    /// <summary>
    /// Get a stable lazy cache identifier for a given row id, allocating only once.
    /// </summary>
    private static string GetLazyRowId(int id)
    {
        if (_lazyRowIdCache.TryGetValue(id, out var cached))
        {
            return cached;
        }

        var value = $"lazy-row-{id}";
        _lazyRowIdCache[id] = value;
        return value;
    }

    /// <summary>
    /// Memoize table rows for select performance.
    /// Key is (Row, isSelected) — only re-diff when row data or selection changes.
    /// </summary>
    private static Node MemoizedRow(Row row, bool isSelected) =>
        lazy((row, isSelected), () => TableRow(row, isSelected), id: GetLazyRowId(row.Id));

    private static Node Jumbotron() =>
        div([class_("jumbotron")],
        [
            div([class_("row")],
            [
                div([class_("col-md-6")],
                [
                    h1([], [text("Abies-keyed")])
                ]),
                div([class_("col-md-6")],
                [
                    div([class_("row")],
                    [
                        ActionButton("run", "Create 1,000 rows", new Create()),
                        ActionButton("runlots", "Create 10,000 rows", new CreateLots()),
                        ActionButton("add", "Append 1,000 rows", new Append()),
                        ActionButton("update", "Update every 10th row", new UpdateEvery10th()),
                        ActionButton("clear", "Clear", new Clear()),
                        ActionButton("swaprows", "Swap Rows", new SwapRows())
                    ])
                ])
            ])
        ]);

    private static Node ActionButton(string buttonId, string label, Message msg) =>
        div([class_("col-sm-6 smallpad")],
        [
            button(
            [
                type("button"),
                class_("btn btn-primary btn-block"),
                id(buttonId),
                onclick(msg)
            ],
            [
                text(label)
            ])
        ]);

    private static Node TableRow(Row row, bool isSelected) =>
        tr([class_(isSelected ? "danger" : ""), id(row.Id.ToString())],
        [
            td([class_("col-md-1")], [text(row.Id.ToString(), id: $"id-{row.Id}")]),
            td([class_("col-md-4")],
            [
                a([class_("lbl"), onclick(new Select(row.Id))],
                    [text(row.Label, id: $"lbl-{row.Id}")],
                    id: $"sel-{row.Id}")
            ]),
            td([class_("col-md-1")],
            [
                a([onclick(new Delete(row.Id))],
                [
                    span([class_("glyphicon glyphicon-remove"), ariaHidden("true")], [],
                        id: $"del-icon-{row.Id}")
                ],
                    id: $"del-{row.Id}")
            ]),
            td([class_("col-md-6")], [])
        ],
            id: $"row-{row.Id}");

    // =========================================================================
    // Subscriptions (not used in benchmark)
    // =========================================================================

    /// <summary>
    /// No subscriptions needed for the benchmark.
    /// </summary>
    public static Subscription Subscriptions(BenchmarkModel model) =>
        new Subscription.None();
}
