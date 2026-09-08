namespace Picea.Abies.History;

/// <summary>
/// One retained step of a history: the model on this step's own side, and the edge
/// leading to it. <see cref="Cause"/> and <see cref="AtTicks"/> name the <em>edge</em>,
/// not the step — an edge's identity survives being crossed in either direction, so
/// crossing it via <see cref="History{TModel}.StepBack"/> or
/// <see cref="History{TModel}.StepForward"/> carries the same <see cref="Cause"/> and
/// <see cref="AtTicks"/> onto the step pushed to the opposite side: it is the same
/// edge seen from the other side, not a newly-recorded one.
/// </summary>
/// <typeparam name="TModel">The wrapped program's model type.</typeparam>
internal sealed record Step<TModel>
{
    /// <summary>The model retained by this step.</summary>
    public TModel Model { get; init; }

    /// <summary>
    /// The message that produced the edge leading to this step. Names the edge, not
    /// the step — see the type-level remarks.
    /// </summary>
    public Message Cause { get; init; }

    /// <summary>Whether the edge into this step may still be crossed, and if not, why.</summary>
    public EdgeState Edge { get; init; }

    /// <summary>
    /// The dispatch tick the edge leading to this step was recorded at. Names the
    /// edge, not the step, for the same reason as <see cref="Cause"/>.
    /// </summary>
    public long AtTicks { get; init; }

    internal Step(TModel model, Message cause, EdgeState edge, long atTicks)
    {
        Model = model;
        Cause = cause;
        Edge = edge;
        AtTicks = atTicks;
    }
}

/// <summary>
/// Whether the edge between two retained states may still be crossed by undo or
/// redo, and if not, the message responsible — so that a refusal can name itself
/// (INV-5).
/// </summary>
internal abstract record EdgeState
{
    /// <summary>
    /// Blocks a public parameterless constructor. Does <em>not</em>, by itself,
    /// close the hierarchy — see <see cref="HierarchySeal"/> for what does and why
    /// this alone does not.
    /// </summary>
    private EdgeState() { }

    /// <summary>
    /// Not a domain member — closes the case hierarchy for real. A record's
    /// compiler-generated copy constructor is <c>protected</c> regardless of this
    /// type's own accessibility, so the private constructor above does not stop
    /// <c>sealed record Evil(EdgeState o) : EdgeState(o);</c> from compiling
    /// wherever this type is visible and reaching the default arm of an exhaustive
    /// switch. This member closes it: it is <c>internal</c>, so only a case
    /// declared in this assembly can implement it, and a <c>sealed</c> record
    /// cannot itself be declared <c>abstract</c> to route around that — an
    /// unlisted subclass fails to compile (<c>CS0534</c>) rather than merely being
    /// discouraged. Every case below implements it identically; the value is
    /// never read.
    /// </summary>
    internal abstract bool HierarchySeal { get; }

    /// <summary>Nothing has spoken to the world across this edge. Undo or redo may cross it.</summary>
    public sealed record Crossable : EdgeState
    {
        internal override bool HierarchySeal => true;
    }

    /// <summary>The application issued a non-silent command across this edge.</summary>
    /// <param name="Cause">The message that issued the command.</param>
    public sealed record SealedByEffect(Message Cause) : EdgeState
    {
        internal override bool HierarchySeal => true;
    }

    /// <summary>Something outside the dispatch that produced this edge moved the projection.</summary>
    /// <param name="Cause">The message the world's change arrived as.</param>
    public sealed record SealedByWorld(Message Cause) : EdgeState
    {
        internal override bool HierarchySeal => true;
    }

    /// <summary>A later action recorded a new undo stop, superseding the branch this edge sits on.</summary>
    /// <param name="Cause">The message that superseded this branch.</param>
    public sealed record SupersededByNewAction(Message Cause) : EdgeState
    {
        internal override bool HierarchySeal => true;
    }
}

/// <summary>
/// Whether a run of undo/redo presses is a single, un-bracketed movement
/// (<see cref="Settled"/>) or is inside an explicit hold, in which case subscriptions
/// are reconciled against the bracket's anchor rather than the state passed through
/// (<see cref="Held"/>).
/// <para>
/// <see cref="Held"/> stores its anchor erased, as <see cref="object"/>, rather than
/// as the wrapped program's model type: <see cref="Movement"/> is not generic, so a
/// caller pattern-matching on it — chrome rendering "movement in progress" — never
/// needs the model's type parameter in scope. The framework recovers the type when
/// it reads the anchor back, because it is the only thing that ever constructs one.
/// </para>
/// </summary>
public abstract record Movement
{
    /// <summary>
    /// Blocks a public parameterless constructor. Does <em>not</em>, by itself,
    /// close the hierarchy — see <see cref="HierarchySeal"/> for what does and why
    /// this alone does not.
    /// </summary>
    private Movement() { }

    /// <summary>
    /// Not a domain member — closes the case hierarchy for real. A record's
    /// compiler-generated copy constructor is <c>protected</c>, so the private
    /// constructor above does not stop
    /// <c>public sealed record Evil(Movement o) : Movement(o);</c> from compiling
    /// in another assembly and reaching the default arm of an exhaustive switch.
    /// This member closes it: it is <c>internal</c>, so only a case declared in
    /// this assembly can implement it, and a <c>sealed</c> record cannot itself be
    /// declared <c>abstract</c> to route around that — an externally-declared
    /// subclass fails to compile (<c>CS0534</c>) rather than merely being
    /// discouraged. Every case below implements it identically; the value is
    /// never read.
    /// </summary>
    internal abstract bool HierarchySeal { get; }

    /// <summary>No bracket is open. Subscriptions are the present model's own.</summary>
    public sealed record Settled : Movement
    {
        internal override bool HierarchySeal => true;
    }

    /// <summary>
    /// A bracket is open. <see cref="Anchor"/> is the model <c>Hold</c> captured —
    /// the value subscriptions are reconciled against until <c>Settle</c> or
    /// <c>Clear</c> closes the bracket.
    /// </summary>
    public sealed record Held : Movement
    {
        /// <summary>The model the bracket was opened against.</summary>
        public object Anchor { get; }

        internal Held(object anchor) => Anchor = anchor;

        internal override bool HierarchySeal => true;
    }
}

/// <summary>
/// Whether anything has yet been recorded. While <see cref="Fresh"/>, an incoming
/// navigation re-bases the history's location rather than recording an undo stop;
/// once a first step has been recorded the history becomes <see cref="Established"/>
/// and an incoming navigation seals instead.
/// </summary>
internal enum Origin
{
    /// <summary>Nothing has been recorded yet.</summary>
    Fresh,

    /// <summary>At least one step has been recorded.</summary>
    Established,
}

/// <summary>
/// The whole of an application's undo/redo state: what came before <see cref="Present"/>,
/// what a walked-back redo could return to, and whether a bracketed movement is
/// currently open.
/// <para>
/// Constructed and mutated only by the framework — see <see cref="History.Start{TModel}"/>
/// and <c>WithHistory</c> — so that no caller can build a value whose stacks disagree
/// with its <see cref="Movement"/>, or whose stacks were never scrubbed. There is no
/// public constructor, and <see cref="Present"/> and <see cref="Movement"/> — the two
/// members a caller can otherwise read — have no public <c>init</c> either, so
/// <c>with</c> cannot be used from outside this assembly to move one without the
/// other.
/// </para>
/// </summary>
/// <typeparam name="TModel">The wrapped program's model type.</typeparam>
public sealed record History<TModel>
{
    /// <summary>The model the wrapped program currently reports.</summary>
    public TModel Present { get; internal init; }

    /// <summary>Whether a bracketed movement is open, and if so, its anchor.</summary>
    public Movement Movement { get; internal init; }

    /// <summary>Steps undo can cross, most recently recorded on top.</summary>
    internal HistoryStack<Step<TModel>> Past { get; init; }

    /// <summary>Steps redo can cross, most recently walked-back-through on top.</summary>
    internal HistoryStack<Step<TModel>> Future { get; init; }

    /// <summary>Whether a step has yet been recorded.</summary>
    internal Origin Origin { get; init; }

    internal History(
        TModel present,
        HistoryStack<Step<TModel>> past,
        HistoryStack<Step<TModel>> future,
        Movement movement,
        Origin origin)
    {
        Present = present;
        Past = past;
        Future = future;
        Movement = movement;
        Origin = origin;
    }

    /// <summary>
    /// Crosses the topmost backward edge. <paramref name="restore"/> combines the
    /// step's remembered model with the current <see cref="Present"/> to produce the
    /// model undo lands on; the state being left is passed through
    /// <paramref name="scrub"/> before it is pushed onto <see cref="Future"/> as a
    /// fresh, crossable step. <see cref="Movement"/> is left exactly as it was — a
    /// press never starts or ends a bracket.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Past"/> is empty.</exception>
    internal History<TModel> StepBack(Func<TModel, TModel, TModel> restore, Func<TModel, TModel> scrub)
    {
        if (Past.IsEmpty)
            throw new InvalidOperationException("Cannot step back: Past is empty.");

        var crossed = Past.Peek();
        var left = new Step<TModel>(scrub(Present), crossed.Cause, new EdgeState.Crossable(), crossed.AtTicks);

        return this with
        {
            Past = Past.Pop(),
            Present = restore(crossed.Model, Present),
            Future = Future.Push(left),
        };
    }

    /// <summary>The mirror image of <see cref="StepBack"/>, crossing the topmost forward edge.</summary>
    /// <exception cref="InvalidOperationException"><see cref="Future"/> is empty.</exception>
    internal History<TModel> StepForward(Func<TModel, TModel, TModel> restore, Func<TModel, TModel> scrub)
    {
        if (Future.IsEmpty)
            throw new InvalidOperationException("Cannot step forward: Future is empty.");

        var crossed = Future.Peek();
        var left = new Step<TModel>(scrub(Present), crossed.Cause, new EdgeState.Crossable(), crossed.AtTicks);

        return this with
        {
            Future = Future.Pop(),
            Present = restore(crossed.Model, Present),
            Past = Past.Push(left),
        };
    }

    /// <summary>
    /// Discards both stacks. <see cref="Present"/>, <see cref="Movement"/> and
    /// <see cref="Origin"/> are unaffected — settling an open bracket is the
    /// caller's own, separate decision.
    /// </summary>
    internal History<TModel> Cleared() =>
        this with { Past = HistoryStack<Step<TModel>>.Empty, Future = HistoryStack<Step<TModel>>.Empty };

    /// <summary>
    /// Marks the top of <see cref="Past"/> and the top of <see cref="Future"/> —
    /// whichever are non-empty — with <paramref name="state"/>. A no-op on a side
    /// that holds nothing.
    /// </summary>
    internal History<TModel> SealTops(EdgeState state) =>
        this with
        {
            Past = Past.IsEmpty ? Past : Past.ReplaceTop(Past.Peek() with { Edge = state }),
            Future = Future.IsEmpty ? Future : Future.ReplaceTop(Future.Peek() with { Edge = state }),
        };
}

/// <summary>Factory for <see cref="History{TModel}"/> values.</summary>
public static class History
{
    /// <summary>
    /// The initial history: no past, no future, settled, and re-basing until the
    /// first step is recorded. The only public way to construct a
    /// <see cref="History{TModel}"/> — everything else is the wrapper's own.
    /// </summary>
    public static History<TModel> Start<TModel>(TModel model) =>
        new(model, HistoryStack<Step<TModel>>.Empty, HistoryStack<Step<TModel>>.Empty, new Movement.Settled(), Origin.Fresh);
}
