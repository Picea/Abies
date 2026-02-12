namespace Abies.Conduit;

public sealed record LoginCommand(string Email, string Password) : Command;

public sealed record RegisterCommand(string Username, string Email, string Password) : Command;

public sealed record UpdateUserCommand(string Username, string Email, string Bio, string Image, string? Password) : Command;

public sealed record LoadArticlesCommand(string? Tag = null, string? Author = null, string? FavoritedBy = null, int Limit = 10, int Offset = 0) : Command;

public sealed record LoadFeedCommand(int Limit = 10, int Offset = 0) : Command;

public sealed record LoadTagsCommand() : Command;

public sealed record LoadArticleCommand(string Slug) : Command;

public sealed record LoadCommentsCommand(string Slug) : Command;

public sealed record SubmitCommentCommand(string Slug, string Body) : Command;

public sealed record DeleteCommentCommand(string Slug, string CommentId) : Command;

public sealed record CreateArticleCommand(string Title, string Description, string Body, List<string> TagList) : Command;

public sealed record UpdateArticleCommand(string Slug, string Title, string Description, string Body) : Command;
public sealed record LoadArticleForEditorCommand(string Slug) : Command;

public sealed record ToggleFavoriteCommand(string Slug, bool CurrentState) : Command;

public sealed record LoadProfileCommand(string Username) : Command;

public sealed record ToggleFollowCommand(string Username, bool CurrentState) : Command;

public sealed record DeleteArticleCommand(string Slug) : Command;

public sealed record LogoutCommand() : Command;
