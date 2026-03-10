// =============================================================================
// User Decider Tests — Command Validation and State Transitions
// =============================================================================
// Tests the User Decider's Decide and Transition functions. Each test
// follows the pattern: Given(state) → When(command) → Then(events|error).
// =============================================================================

using Picea.Abies.Conduit.Domain.Shared;
using Picea.Abies.Conduit.Domain.User;
using Picea;

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

    [Fact]
    public void Initialize_ReturnsUnregisteredState()
    {
        var (state, effect) = User.Initialize(Unit.Value);

        Assert.False(state.Registered);
        Assert.IsType<UserEffect.None>(effect);
    }

    // =========================================================================
    // Register
    // =========================================================================

    [Fact]
    public void Decide_Register_WhenNotRegistered_ProducesRegisteredEvent()
    {
        var state = UserState.Initial;
        var command = new UserCommand.Register(UserId, Email, Username, PasswordHash, Now);

        var result = User.Decide(state, command);

        Assert.True(result.IsOk);
        var events = result.Value;
        var registered = Assert.Single(events);
        var e = Assert.IsType<UserEvent.Registered>(registered);
        Assert.Equal(UserId, e.Id);
        Assert.Equal(Email, e.Email);
        Assert.Equal(Username, e.Username);
        Assert.Equal(PasswordHash, e.PasswordHash);
        Assert.Equal(Now, e.CreatedAt);
    }

    [Fact]
    public void Decide_Register_WhenAlreadyRegistered_ReturnsError()
    {
        var state = RegisteredState();
        var command = new UserCommand.Register(UserId.New(), Email, Username, PasswordHash, Now);

        var result = User.Decide(state, command);

        Assert.True(result.IsErr);
        Assert.IsType<UserError.AlreadyRegistered>(result.Error);
    }

    [Fact]
    public void Transition_Registered_SetsAllFields()
    {
        var state = UserState.Initial;
        var e = new UserEvent.Registered(UserId, Email, Username, PasswordHash, Now);

        var (newState, _) = User.Transition(state, e);

        Assert.Equal(UserId, newState.Id);
        Assert.Equal(Email, newState.Email);
        Assert.Equal(Username, newState.Username);
        Assert.Equal(PasswordHash, newState.PasswordHash);
        Assert.Equal(Now, newState.CreatedAt);
        Assert.Equal(Now, newState.UpdatedAt);
        Assert.True(newState.Registered);
    }

    // =========================================================================
    // Commands before registration
    // =========================================================================

    [Fact]
    public void Decide_UpdateProfile_WhenNotRegistered_ReturnsNotRegistered()
    {
        var state = UserState.Initial;
        var command = new UserCommand.UpdateProfile(
            Option<EmailAddress>.None, Option<Username>.None,
            Option<PasswordHash>.None, Option<Bio>.None,
            Option<ImageUrl>.None, Now);

        var result = User.Decide(state, command);

        Assert.True(result.IsErr);
        Assert.IsType<UserError.NotRegistered>(result.Error);
    }

    [Fact]
    public void Decide_Follow_WhenNotRegistered_ReturnsNotRegistered()
    {
        var state = UserState.Initial;

        var result = User.Decide(state, new UserCommand.Follow(UserId.New()));

        Assert.True(result.IsErr);
        Assert.IsType<UserError.NotRegistered>(result.Error);
    }

    // =========================================================================
    // UpdateProfile
    // =========================================================================

    [Fact]
    public void Decide_UpdateProfile_ProducesProfileUpdatedEvent()
    {
        var state = RegisteredState();
        var newEmail = EmailAddress.Create("new@example.com").Value;
        var command = new UserCommand.UpdateProfile(
            Option.Some(newEmail), Option<Username>.None,
            Option<PasswordHash>.None, Option<Bio>.None,
            Option<ImageUrl>.None, Now);

        var result = User.Decide(state, command);

        Assert.True(result.IsOk);
        var e = Assert.IsType<UserEvent.ProfileUpdated>(Assert.Single(result.Value));
        Assert.True(e.Email.IsSome);
        Assert.Equal(newEmail, e.Email.Value);
        Assert.True(e.Username.IsNone);
    }

    [Fact]
    public void Transition_ProfileUpdated_OnlyChangesProvidedFields()
    {
        var state = RegisteredState();
        var newEmail = EmailAddress.Create("new@example.com").Value;
        var e = new UserEvent.ProfileUpdated(
            Option.Some(newEmail), Option<Username>.None,
            Option<PasswordHash>.None, Option<Bio>.None,
            Option<ImageUrl>.None, Now);

        var (newState, _) = User.Transition(state, e);

        Assert.Equal(newEmail, newState.Email);
        Assert.Equal(state.Username, newState.Username);
        Assert.Equal(state.PasswordHash, newState.PasswordHash);
        Assert.Equal(state.Bio, newState.Bio);
        Assert.Equal(state.Image, newState.Image);
        Assert.Equal(Now, newState.UpdatedAt);
    }

    // =========================================================================
    // Follow / Unfollow
    // =========================================================================

    [Fact]
    public void Decide_Follow_ProducesFollowedEvent()
    {
        var state = RegisteredState();
        var followeeId = UserId.New();

        var result = User.Decide(state, new UserCommand.Follow(followeeId));

        Assert.True(result.IsOk);
        var e = Assert.IsType<UserEvent.Followed>(Assert.Single(result.Value));
        Assert.Equal(followeeId, e.FolloweeId);
    }

    [Fact]
    public void Decide_Follow_Self_ReturnsCannotFollowSelf()
    {
        var state = RegisteredState();

        var result = User.Decide(state, new UserCommand.Follow(state.Id));

        Assert.True(result.IsErr);
        Assert.IsType<UserError.CannotFollowSelf>(result.Error);
    }

    [Fact]
    public void Decide_Follow_AlreadyFollowing_ReturnsError()
    {
        var followeeId = UserId.New();
        var state = RegisteredState() with
        {
            Following = new HashSet<UserId> { followeeId }
        };

        var result = User.Decide(state, new UserCommand.Follow(followeeId));

        Assert.True(result.IsErr);
        Assert.IsType<UserError.AlreadyFollowing>(result.Error);
    }

    [Fact]
    public void Transition_Followed_AddsToFollowingSet()
    {
        var state = RegisteredState();
        var followeeId = UserId.New();
        var e = new UserEvent.Followed(followeeId);

        var (newState, _) = User.Transition(state, e);

        Assert.Contains(followeeId, newState.Following);
    }

    [Fact]
    public void Decide_Unfollow_ProducesUnfollowedEvent()
    {
        var followeeId = UserId.New();
        var state = RegisteredState() with
        {
            Following = new HashSet<UserId> { followeeId }
        };

        var result = User.Decide(state, new UserCommand.Unfollow(followeeId));

        Assert.True(result.IsOk);
        var e = Assert.IsType<UserEvent.Unfollowed>(Assert.Single(result.Value));
        Assert.Equal(followeeId, e.FolloweeId);
    }

    [Fact]
    public void Decide_Unfollow_NotFollowing_ReturnsError()
    {
        var state = RegisteredState();

        var result = User.Decide(state, new UserCommand.Unfollow(UserId.New()));

        Assert.True(result.IsErr);
        Assert.IsType<UserError.NotFollowing>(result.Error);
    }

    [Fact]
    public void Transition_Unfollowed_RemovesFromFollowingSet()
    {
        var followeeId = UserId.New();
        var state = RegisteredState() with
        {
            Following = new HashSet<UserId> { followeeId }
        };
        var e = new UserEvent.Unfollowed(followeeId);

        var (newState, _) = User.Transition(state, e);

        Assert.DoesNotContain(followeeId, newState.Following);
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
