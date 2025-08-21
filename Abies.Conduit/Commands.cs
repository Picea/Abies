using Abies.Conduit.Main;
using Abies.Conduit.Services;
using System.Collections.Generic;

namespace Abies.Conduit;

public sealed record LoginCommand(string Email, string Password) : Abies.Command;

public sealed record RegisterCommand(string Username, string Email, string Password) : Abies.Command;

public sealed record UpdateUserCommand(string Username, string Email, string Bio, string Image, string? Password) : Abies.Command;

public sealed record LoadArticlesCommand(string? Tag = null, string? Author = null, string? FavoritedBy = null, int Limit = 10, int Offset = 0) : Abies.Command;

public sealed record LoadFeedCommand(int Limit = 10, int Offset = 0) : Abies.Command;

public sealed record LoadTagsCommand() : Abies.Command;

public sealed record LoadArticleCommand(string Slug) : Abies.Command;

public sealed record LoadCommentsCommand(string Slug) : Abies.Command;

public sealed record SubmitCommentCommand(string Slug, string Body) : Abies.Command;

public sealed record DeleteCommentCommand(string Slug, string CommentId) : Abies.Command;

public sealed record CreateArticleCommand(string Title, string Description, string Body, List<string> TagList) : Abies.Command;

public sealed record UpdateArticleCommand(string Slug, string Title, string Description, string Body) : Abies.Command;
public sealed record LoadArticleForEditorCommand(string Slug) : Abies.Command;

public sealed record ToggleFavoriteCommand(string Slug, bool CurrentState) : Abies.Command;

public sealed record LoadProfileCommand(string Username) : Abies.Command;

public sealed record ToggleFollowCommand(string Username, bool CurrentState) : Abies.Command;

public sealed record DeleteArticleCommand(string Slug) : Abies.Command;
