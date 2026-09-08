using Picea.Abies.History;
using TUnit.Assertions.Enums;

namespace Picea.Abies.Tests.History;

/// <summary>
/// Covers push, pop, replace-top, eviction at the far end, depth invariance under
/// 10,000 pushes, and persistence (operations on a derived instance leave an older
/// instance observably unchanged).
/// </summary>
public sealed class HistoryStackTests
{
    [Test]
    public async Task Empty_has_no_elements()
    {
        await Assert.That(HistoryStack<int>.Empty.Count).IsEqualTo(0);
        await Assert.That(HistoryStack<int>.Empty.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Push_adds_to_the_top_and_peek_returns_it()
    {
        var stack = HistoryStack<int>.Empty.Push(1).Push(2).Push(3);

        await Assert.That(stack.Count).IsEqualTo(3);
        await Assert.That(stack.IsEmpty).IsFalse();
        await Assert.That(stack.Peek()).IsEqualTo(3);
    }

    [Test]
    public async Task Pop_removes_only_the_top_element()
    {
        var stack = HistoryStack<int>.Empty.Push(1).Push(2).Push(3).Pop();

        await Assert.That(stack.Count).IsEqualTo(2);
        await Assert.That(stack.Peek()).IsEqualTo(2);
        await Assert.That(stack.Pop().Peek()).IsEqualTo(1);
    }

    [Test]
    public async Task ReplaceTop_replaces_only_the_top_element()
    {
        var stack = HistoryStack<int>.Empty.Push(1).Push(2).ReplaceTop(99);

        await Assert.That(stack.Count).IsEqualTo(2);
        await Assert.That(stack.Peek()).IsEqualTo(99);
        await Assert.That(stack.Pop().Peek()).IsEqualTo(1);
    }

    [Test]
    public async Task Peek_on_an_empty_stack_throws()
    {
        var act = () => HistoryStack<int>.Empty.Peek();

        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task Pop_on_an_empty_stack_throws()
    {
        var act = () => HistoryStack<int>.Empty.Pop();

        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task ReplaceTop_on_an_empty_stack_throws()
    {
        var act = () => HistoryStack<int>.Empty.ReplaceTop(1);

        await Assert.That(act).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task Trim_to_a_negative_depth_throws()
    {
        var act = () => HistoryStack<int>.Empty.Push(1).Trim(-1);

        await Assert.That(act).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Trim_evicts_from_the_far_end_keeping_the_most_recently_pushed()
    {
        var stack = HistoryStack<int>.Empty.Push(1).Push(2).Push(3).Push(4).Push(5);

        var trimmed = stack.Trim(3);

        await Assert.That(trimmed.Count).IsEqualTo(3);
        await Assert.That(trimmed.ToArray()).IsEquivalentTo([3, 4, 5], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Trim_to_a_depth_at_or_above_the_current_count_is_a_no_op()
    {
        var stack = HistoryStack<int>.Empty.Push(1).Push(2);

        await Assert.That(stack.Trim(2).Count).IsEqualTo(2);
        await Assert.That(stack.Trim(10).Count).IsEqualTo(2);
    }

    [Test]
    public async Task Trim_of_the_empty_stack_is_a_no_op()
    {
        await Assert.That(HistoryStack<int>.Empty.Trim(5).IsEmpty).IsTrue();
        await Assert.That(HistoryStack<int>.Empty.Trim(0).IsEmpty).IsTrue();
    }

    [Test]
    public async Task Depth_never_exceeds_the_bound_across_ten_thousand_pushes()
    {
        const int depth = 100;
        var stack = HistoryStack<int>.Empty;

        for (var i = 0; i < 10_000; i++)
        {
            stack = stack.Push(i).Trim(depth);
            await Assert.That(stack.Count).IsLessThanOrEqualTo(depth);
        }

        await Assert.That(stack.Count).IsEqualTo(depth);
        await Assert.That(stack.Peek()).IsEqualTo(9_999);
        await Assert.That(stack.ToArray()[0]).IsEqualTo(9_900); // oldest surviving element
    }

    [Test]
    public async Task Every_operation_leaves_an_older_instance_observably_unchanged()
    {
        var original = HistoryStack<int>.Empty.Push(1).Push(2).Push(3);

        var pushed = original.Push(4);
        var popped = original.Pop();
        var replaced = original.ReplaceTop(99);
        var trimmed = original.Trim(1);

        // The older instance, inspected after every derived operation, is untouched.
        await Assert.That(original.Count).IsEqualTo(3);
        await Assert.That(original.Peek()).IsEqualTo(3);
        await Assert.That(original.ToArray()).IsEquivalentTo([1, 2, 3], CollectionOrdering.Matching);

        // And the derived instances each reflect only their own operation.
        await Assert.That(pushed.ToArray()).IsEquivalentTo([1, 2, 3, 4], CollectionOrdering.Matching);
        await Assert.That(popped.ToArray()).IsEquivalentTo([1, 2], CollectionOrdering.Matching);
        await Assert.That(replaced.ToArray()).IsEquivalentTo([1, 2, 99], CollectionOrdering.Matching);
        await Assert.That(trimmed.ToArray()).IsEquivalentTo([3], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Enumeration_yields_elements_from_oldest_to_most_recently_pushed()
    {
        var stack = HistoryStack<int>.Empty.Push(1).Push(2).Push(3);

        await Assert.That(stack.ToArray()).IsEquivalentTo([1, 2, 3], CollectionOrdering.Matching);
    }

    // ── Structural equality ──────────────────────────────────────────────────────
    // Every operation returns a fresh instance, so reference equality alone would
    // say two stacks with identical content are different — and History<TModel>'s
    // record-synthesized Equals compares Past/Future through this type's own
    // Equals, so this is what makes two structurally identical histories compare
    // equal too.

    [Test]
    public async Task Two_structurally_identical_stacks_are_equal_and_share_a_hash_code()
    {
        var left = HistoryStack<int>.Empty.Push(1).Push(2).Push(3);
        var right = HistoryStack<int>.Empty.Push(1).Push(2).Push(3);

        await Assert.That(ReferenceEquals(left, right)).IsFalse(); // distinct instances
        await Assert.That(left.Equals(right)).IsTrue();
        await Assert.That(left.Equals((object)right)).IsTrue();
        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
    }

    [Test]
    public async Task Stacks_differing_in_content_or_length_are_not_equal()
    {
        var baseline = HistoryStack<int>.Empty.Push(1).Push(2).Push(3);
        var differentContent = HistoryStack<int>.Empty.Push(1).Push(2).Push(99);
        var differentLength = HistoryStack<int>.Empty.Push(1).Push(2);

        await Assert.That(baseline.Equals(differentContent)).IsFalse();
        await Assert.That(baseline.Equals(differentLength)).IsFalse();
        await Assert.That(baseline.Equals(null)).IsFalse();
    }

    [Test]
    public async Task Two_empty_stacks_are_equal()
    {
        // Two INDEPENDENTLY DERIVED empties, neither of them the Empty singleton —
        // comparing Empty to itself would short-circuit on ReferenceEquals and
        // could never fail for the reason this test is named for.
        var left = HistoryStack<int>.Empty.Push(1).Pop();
        var right = HistoryStack<int>.Empty.Push(2).Pop();

        await Assert.That(ReferenceEquals(left, right)).IsFalse();
        await Assert.That(ReferenceEquals(left, HistoryStack<int>.Empty)).IsFalse();
        await Assert.That(left.Equals(right)).IsTrue();
    }
}
