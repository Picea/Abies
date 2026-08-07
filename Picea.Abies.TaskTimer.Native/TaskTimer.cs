// =============================================================================
// TaskTimer — Subscriptions, Keyed Lists and Two-Way Input on Native Controls
// =============================================================================
// The Counter sample proves a program renders. This one exercises the paths
// that only appear in a real app:
//
//   * an interval subscription, which dispatches from a threadpool thread and
//     therefore must be marshaled to the UI dispatcher;
//   * subscriptions that start and stop as the model changes (the interval runs
//     only while a task is running);
//   * a keyed list whose order genuinely changes — rows are sorted by elapsed
//     time, so a running task climbs past the others and the diff emits
//     MoveChild against live controls;
//   * two-way text input, where the model owns the draft text.
// =============================================================================

using Picea;
using Picea.Abies.DOM;
using Picea.Abies.Native;
using Picea.Abies.Subscriptions;
using static Picea.Abies.Native.Elements;
using static Picea.Abies.Native.Events;
using static Picea.Abies.Native.Properties;

namespace Picea.Abies.TaskTimer.Native;

public record TrackedTask(string Id, string Name, int ElapsedSeconds, bool Running);

public record TaskTimerModel(IReadOnlyList<TrackedTask> Tasks, string Draft, int NextId);

public record DraftChanged(string Text) : Message;
public record AddTask : Message;
public record ToggleTask(string Id) : Message;
public record RemoveTask(string Id) : Message;
public record Tick : Message;

public sealed class TaskTimerProgram : ProgramCore<TaskTimerModel, Unit>
{
    public static (TaskTimerModel, Command) Initialize(Unit argument) =>
        (new TaskTimerModel([], string.Empty, 1), Commands.None);

    public static (TaskTimerModel, Command) Transition(TaskTimerModel model, Message message) =>
        (Update(model, message), Commands.None);

    private static TaskTimerModel Update(TaskTimerModel model, Message message) => message switch
    {
        DraftChanged m => model with { Draft = m.Text },

        AddTask when !string.IsNullOrWhiteSpace(model.Draft) => model with
        {
            Tasks = [.. model.Tasks, new TrackedTask($"t{model.NextId}", model.Draft.Trim(), 0, false)],
            Draft = string.Empty,
            NextId = model.NextId + 1,
        },

        ToggleTask m => model with
        {
            Tasks = [.. model.Tasks.Select(t => t.Id == m.Id ? t with { Running = !t.Running } : t)],
        },

        RemoveTask m => model with { Tasks = [.. model.Tasks.Where(t => t.Id != m.Id)] },

        // Sorting here is what makes the list reorder: a running task overtakes
        // idle ones, so the diff has to move existing controls rather than
        // rebuild the list.
        Tick => model with { Tasks = Ordered([.. model.Tasks.Select(Advance)]) },

        _ => model,
    };

    private static TrackedTask Advance(TrackedTask task) =>
        task.Running ? task with { ElapsedSeconds = task.ElapsedSeconds + 1 } : task;

    private static IReadOnlyList<TrackedTask> Ordered(IEnumerable<TrackedTask> tasks) =>
        [.. tasks.OrderByDescending(t => t.ElapsedSeconds).ThenBy(t => t.Id, StringComparer.Ordinal)];

    public static Result<Message[], Message> Decide(TaskTimerModel model, Message command) =>
        Result<Message[], Message>.Ok([command]);

    public static bool IsTerminal(TaskTimerModel model) => false;

    /// <summary>
    /// The interval exists only while something is running, so this also
    /// exercises subscriptions starting and stopping as the model changes.
    /// </summary>
    public static Subscription Subscriptions(TaskTimerModel model) =>
        model.Tasks.Any(t => t.Running)
            ? SubscriptionModule.Every("tick", TimeSpan.FromSeconds(1), () => new Tick())
            : SubscriptionModule.None;
}

public sealed class TaskTimerView : ProgramView<TaskTimerModel>
{
    public static Document View(TaskTimerModel model) =>
        new("Abies TaskTimer",
            StackPanel([Spacing(12), Padding(24), Width(420)],
            [
                TextBlock([FontSize(24), FontWeight(TextWeight.Bold)], "Task Timer"),

                StackPanel([Orientation(StackOrientation.Horizontal), Spacing(8)],
                [
                    TextBox([
                        Width(280),
                        Text(model.Draft),
                        PlaceholderText("What are you working on?"),
                        OnTextChanged(d => new DraftChanged(d!.Text)),
                    ]),
                    Button([OnClick(new AddTask()), Width(96)], "Add"),
                ]),

                model.Tasks.Count == 0
                    ? TextBlock([Opacity(0.6)], "No tasks yet.")
                    : StackPanel([Spacing(6)], [.. model.Tasks.Select(Row)]),
            ]));

    // Elements built in a loop must carry explicit ids: the compile-time id is
    // per call site, so every row would otherwise share one. Key drives the
    // diff's keyed reconciliation, which is what turns a re-sort into moves.
    private static Node Row(TrackedTask task) =>
        Border([Id($"row-{task.Id}"), Key(task.Id), Padding(8), CornerRadius(4), BorderThickness(1), BorderBrush("gray")],
            StackPanel([Id($"row-inner-{task.Id}"), Orientation(StackOrientation.Horizontal), Spacing(8)],
            [
                TextBlock([Id($"name-{task.Id}"), Width(180), VerticalAlignment(Alignment.Center)], task.Name),
                TextBlock([Id($"elapsed-{task.Id}"), Width(56), VerticalAlignment(Alignment.Center)], $"{task.ElapsedSeconds}s"),
                Button([Id($"toggle-{task.Id}"), OnClick(new ToggleTask(task.Id)), Width(72)],
                    task.Running ? "Stop" : "Start"),
                Button([Id($"remove-{task.Id}"), OnClick(new RemoveTask(task.Id)), Width(72)], "Remove"),
            ]));
}
