// =============================================================================
// Messages — All MVU Messages for the Conduit Frontend
// =============================================================================

namespace Picea.Abies.Conduit.App;

public interface ConduitMessage : Message;

// ─── Form Input ───────────────────────────────────────────────────────────

public sealed record LoginEmailChanged(string Value) : ConduitMessage;
public sealed record LoginPasswordChanged(string Value) : ConduitMessage;
public sealed record LoginSubmitted : ConduitMessage;

public sealed record RegisterUsernameChanged(string Value) : ConduitMessage;
public sealed record RegisterEmailChanged(string Value) : ConduitMessage;
public sealed record RegisterPasswordChanged(string Value) : ConduitMessage;
public sealed record RegisterSubmitted : ConduitMessage;

public sealed record CommentBodyChanged(string Value) : ConduitMessage;
public sealed record CommentSubmitted : ConduitMessage;

// ─── Settings Form ────────────────────────────────────────────────────────

public sealed record SettingsImageChanged(string Value) : ConduitMessage;
public sealed record SettingsUsernameChanged(string Value) : ConduitMessage;
public sealed record SettingsBioChanged(string Value) : ConduitMessage;
public sealed record SettingsEmailChanged(string Value) : ConduitMessage;
public sealed record SettingsPasswordChanged(string Value) : ConduitMessage;
public sealed record SettingsSubmitted : ConduitMessage;

// ─── Editor Form ──────────────────────────────────────────────────────────

public sealed record EditorTitleChanged(string Value) : ConduitMessage;
public sealed record EditorDescriptionChanged(string Value) : ConduitMessage;
public sealed record EditorBodyChanged(string Value) : ConduitMessage;
public sealed record EditorTagInputChanged(string Value) : ConduitMessage;
public sealed record EditorAddTag : ConduitMessage;
public sealed record EditorTagKeyDown(string Key) : ConduitMessage;
public sealed record EditorRemoveTag(string Tag) : ConduitMessage;
public sealed record EditorSubmitted : ConduitMessage;

// ─── Profile Interaction ───────────────────────────────────────────────────

public sealed record ProfileTabChanged(bool ShowFavorites) : ConduitMessage;

// ─── UI Interaction ───────────────────────────────────────────────────────

public sealed record FeedTabChanged(FeedTab Tab, string? Tag = null) : ConduitMessage;
public sealed record PageChanged(int PageNumber) : ConduitMessage;
public sealed record ToggleFavorite(string Slug, bool Favorited) : ConduitMessage;
public sealed record ToggleFollow(string Username, bool Following) : ConduitMessage;
public sealed record DeleteArticle(string Slug) : ConduitMessage;
public sealed record DeleteComment(string Slug, Guid CommentId) : ConduitMessage;

// ─── API Responses ────────────────────────────────────────────────────────

public sealed record ArticlesLoaded(IReadOnlyList<ArticlePreviewData> Articles, int ArticlesCount) : ConduitMessage;
public sealed record ArticleLoaded(ArticleData Article) : ConduitMessage;
public sealed record CommentsLoaded(IReadOnlyList<CommentData> Comments) : ConduitMessage;
public sealed record TagsLoaded(IReadOnlyList<string> Tags) : ConduitMessage;
public sealed record UserAuthenticated(Session Session) : ConduitMessage;
public sealed record ProfileLoaded(ProfileData Profile) : ConduitMessage;
public sealed record FavoriteToggled(ArticlePreviewData Article) : ConduitMessage;
public sealed record FollowToggled(ProfileData Profile) : ConduitMessage;
public sealed record CommentAdded(CommentData Comment) : ConduitMessage;
public sealed record CommentDeleted(Guid CommentId) : ConduitMessage;
public sealed record ArticleDeleted : ConduitMessage;
public sealed record UserUpdated(Session Session) : ConduitMessage;
public sealed record ArticleSaved(string Slug) : ConduitMessage;
public sealed record ApiError(IReadOnlyList<string> Errors) : ConduitMessage;
public sealed record Logout : ConduitMessage;
