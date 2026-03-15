// =============================================================================
// User Decider Tests — Command Validation and State Transitions
// =============================================================================
// Tests the User Decider's Decide and Transition functions. Each test
// follows the pattern: Given(state) → When(command) → Then(events|error).
// =============================================================================

using Picea.Abies.Conduit.Domain.Shared;
using Picea.Abies.Conduit.Domain.User;

namespace Picea.Abies.Conduit.Tests;

public class UserDeciderTests
{
    private static readonly UserId UserId = UserId.New();
    private static readonly Timestamp Now = Timestamp.Now();

    private static readonly EmailAddress Email =
        EmailAddress.Create("test@example.com").Value;

    private static readonly Username Username =
        Username.Create("testuser").Value;

    private static readonly PasswordHash PasswordHash = new("hashed-password");

    // =========================================================================
    // Initialize
    // =========================================================================

    [Test]
    public async Task Initialize_ReturnsUnregisteredState()
    {
        var (state, effect) = User.Initialize(Unit.Value);

        await Assert.That(state.Registered).IsFalse();
        await Assert.That(effect).IsTypeOf<UserEffect.None>();
    }

    // =========================================================================
    // Register
    // =========================================================================

    [Test]
    public async Task Decide_Register_WhenNotRegistered_ProducesRegisteredEvent()
    {
        var state = UserState.Initial;
        var command = new UserCommand.Register(UserId, Email, Username, PasswordHash, Now);

        var result = User.Decide(state, command);

        await Assert.That(result.IsOk).IsTrue();
        var events = result.Value;
        await Assert.That(events).Count().IsEqualTo(1);
        var registered = events[0];
        var e = await Assert.That(registered).IsTypeOf<UserEvent.Registered>();
        await Assert.That(e.Id).IsEqualTo(UserId);
        await Assert.That(e.Email).IsEqualTo(Email);
        await Assert.That(e.Username).IsEqualTo(Username);
        await Assert.That(e.PasswordHash).IsEqualTo(PasswordHash);
        await Assert.That(e.CreatedAt).IsEqualTo(Now);
    }

    [Test]
    public async Task Decide_Register_WhenAlreadyRegistered_ReturnsError()
    {
        var state = RegisteredState();
        var command = new UserCommand.Register(UserId.New(), Email, Username, PasswordHash, Now);

        var result = User.Decide(state, command);

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsTypeOf<UserError.AlreadyRegistered>();
    }

    [Test]
    public async Task Transition_Registered_SetsAllFields()
    {
        var state = UserState.Initial;
        var e = new UserEvent.Registered(UserId, Email, Username, PasswordHash, Now);

        var (newState, _) = User.Transition(state, e);

        await Assert.That(newState.Id).IsEqualTo(UserId);
        await Assert.That(newState.Email).IsEqualTo(Email);
        await Assert.That(newState.Username).IsEqualTo(Username);
        await Assert.That(newState.PasswordHash).IsEqualTo(PasswordHash);
        await Assert.That(newState.CreatedAt).IsEqualTo(Now);
        await Assert.That(newState.UpdatedAt).IsEqualTo(Now);
        await Assert.That(newState.Registered).IsTrue();
    }

    // =========================================================================
    // Commands before registration
    // =========================================================================

    [Test]
    public async Task Decide_UpdateProfile_WhenNotRegistered_ReturnsNotRegistered()
    {
        var state = UserState.Initial;
        var command = new UserCommand.UpdateProfile(
            Option<EmailAddress>.None, Option<Username>.None,
            Option<PasswordHash>.None, Option<Bio>.None,
            Option<ImageUrl>.None, Now);

        var result = User.Decide(state, command);

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsTypeOf<UserError.NotRegistered>();
    }

    [Test]
    public async Task Decide_Follow_WhenNotRegistered_ReturnsNotRegistered()
    {
        var state = UserState.Initial;

        var result = User.Decide(state, new UserCommand.Follow(UserId.New()));

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsTypeOf<UserError.NotRegistered>();
    }

    // =========================================================================
    // UpdateProfile
    // =========================================================================

    [Test]
    public async Task Decide_UpdateProfile_ProducesProfileUpdatedEvent()
    {
        var state = RegisteredState();
        var newEmail = EmailAddress.Create("new@example.com").Value;
        var command = new UserCommand.UpdateProfile(
            Option.Some(newEmail), Option<Username>.None,
            Option<PasswordHash>.None, Option<Bio>.None,
            Option<ImageUrl>.None, Now);

        var result = User.Decide(state, command);

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).Count().IsEqualTo(1);
        var e = await Assert.That(result.Value[0]).IsTypeOf<UserEvent.ProfileUpdated>();
        await Assert.That(e.Email.IsSome).IsTrue();
        await Assert.That(e.Email.Value).IsEqualTo(newEmail);
        await Assert.That(e.Username.IsNone).IsTrue();
    }

    [Test]
    public async Task Transition_ProfileUpdated_OnlyChangesProvidedFields()
    {
        var state = RegisteredState();
        var newEmail = EmailAddress.Create("new@example.com").Value;
        var e = new UserEvent.ProfileUpdated(
            Option.Some(newEmail), Option<Username>.None,
            Option<PasswordHash>.None, Option<Bio>.None,
            Option<ImageUrl>.None, Now);

        var (newState, _) = User.Transition(state, e);

        await Assert.That(newState.Email).IsEqualTo(newEmail);
        await Assert.That(newState.Username).IsEqualTo(state.Username);
        await Assert.That(newState.PasswordHash).IsEqualTo(state.PasswordHash);
        await Assert.That(newState.Bio).IsEqualTo(state.Bio);
        await Assert.That(newState.Image).IsEqualTo(state.Image);
        await Assert.That(newState.UpdatedAt).IsEqualTo(Now);
    }

    // =========================================================================
    // Follow / Unfollow
    // =========================================================================

    [Test]
    public async Task Decide_Follow_ProducesFollowedEvent()
    {
        var state = RegisteredState();
        var followeeId = UserId.New();

        var result = User.Decide(state, new UserCommand.Follow(followeeId));

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).Count().IsEqualTo(1);
        var e = await Assert.That(result.Value[0]).IsTypeOf<UserEvent.Followed>();
        await Assert.That(e.FolloweeId).IsEqualTo(followeeId);
    }

    [Test]
    public async Task Decide_Follow_Self_ReturnsCannotFollowSelf()
    {
        var state = RegisteredState();

        var result = User.Decide(state, new UserCommand.Follow(state.Id));

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsTypeOf<UserError.CannotFollowSelf>();
    }

    [Test]
    public async Task Decide_Follow_AlreadyFollowing_ReturnsError()
    {
        var followeeId = UserId.New();
        var state = RegisteredState() with
        {
            Following = new HashSet<UserId> { followeeId }
        };

        var result = User.Decide(state, new UserCommand.Follow(followeeId));

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsTypeOf<UserError.AlreadyFollowing>();
    }

    [Test]
    public async Task Transition_Followed_AddsToFollowingSet()
    {
        var state = RegisteredState();
        var followeeId = UserId.New();
        var e = new UserEvent.Followed(followeeId);

        var (newState, _) = User.Transition(state, e);

        await Assert.That(newState.Following).Contains(followeeId);
    }

    [Test]
    public async Task Decide_Unfollow_ProducesUnfollowedEvent()
    {
        var followeeId = UserId.New();
        var state = RegisteredState() with
        {
            Following = new HashSet<UserId> { followeeId }
        };

        var result = User.Decide(state, new UserCommand.Unfollow(followeeId));

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).Count().IsEqualTo(1);
        var e = await Assert.That(result.Value[0]).IsTypeOf<UserEvent.Unfollowed>();
        await Assert.That(e.FolloweeId).IsEqualTo(followeeId);
    }

    [Test]
    public async Task Decide_Unfollow_NotFollowing_ReturnsError()
    {
        var state = RegisteredState();

        var result = User.Decide(state, new UserCommand.Unfollow(UserId.New()));

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsTypeOf<UserError.NotFollowing>();
    }

    [Test]
    public async Task Transition_Unfollowed_RemovesFromFollowingSet()
    {
        var followeeId = UserId.New();
        var state = RegisteredState() with
        {
            Following = new HashSet<UserId> { followeeId }
        };
        var e = new UserEvent.Unfollowed(followeeId);

        var (newState, _) = User.Transition(state, e);

        await Assert.That(newState.Following).DoesNotContain(followeeId);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Creates a registered user state for testing post-registration commands.
    /// </summary>
    private static UserState RegisteredState()
    {
        var (initial, _) = User.Initialize(Unit.Value);
        var registered = new UserEvent.Registered(UserId, Email, Username, PasswordHash, Now);
        var (state, _) = User.Transition(initial, registered);
        return state;
    }
}
