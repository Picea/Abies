# Error Handling Guide

Patterns for handling errors gracefully in Abies applications.

## Overview

Error handling in MVU follows a predictable pattern:

1. **Model** stores error state
2. **Commands** can fail and dispatch error messages
3. **Update** handles error messages and updates model
4. **View** displays errors to users

## Error State in Model

Store error information in your model:

```csharp
public record Model(
    bool IsLoading,
    IReadOnlyList<Article> Articles,
    string? Error,                    // Single error message
    IReadOnlyList<string> Errors      // Multiple errors (e.g., validation)
);

// Or use a dedicated error type
public interface LoadState<T>
{
    record Loading : LoadState<T>;
    record Success(T Data) : LoadState<T>;
    record Failed(string Error) : LoadState<T>;
}

public record Model(
    LoadState<IReadOnlyList<Article>> ArticlesState
);
```

## Error Messages

Define message types for errors:

```csharp
public interface Message { }

// Success messages
public record ArticlesLoaded(IReadOnlyList<Article> Articles) : Message;
public record ArticleCreated(Article Article) : Message;

// Error messages
public record LoadArticlesFailed(string Error) : Message;
public record CreateArticleFailed(IReadOnlyList<string> Errors) : Message;

// Generic error
public record ApiError(string Message, string? Details = null) : Message;
```

## Handling Errors in Commands

Wrap async operations in try-catch:

```csharp
public static async Task HandleCommand(Command command, Func<Message, ValueTuple> dispatch)
{
    switch (command)
    {
        case LoadArticlesCommand:
            try
            {
                var response = await httpClient.GetAsync("/api/articles");
                
                if (response.IsSuccessStatusCode)
                {
                    var articles = await response.Content
                        .ReadFromJsonAsync<ArticlesResponse>();
                    dispatch(new ArticlesLoaded(articles.Articles));
                }
                else
                {
                    var error = response.StatusCode switch
                    {
                        HttpStatusCode.NotFound => "Articles not found",
                        HttpStatusCode.Unauthorized => "Please log in to view articles",
                        HttpStatusCode.Forbidden => "You don't have access to these articles",
                        _ => $"Failed to load articles: {response.StatusCode}"
                    };
                    dispatch(new LoadArticlesFailed(error));
                }
            }
            catch (HttpRequestException ex)
            {
                dispatch(new LoadArticlesFailed($"Network error: {ex.Message}"));
            }
            catch (JsonException ex)
            {
                dispatch(new LoadArticlesFailed($"Invalid response: {ex.Message}"));
            }
            break;
    }
}
```

## Handling Errors in Update

Process error messages and update model:

```csharp
public static (Model, Command) Update(Message msg, Model model)
    => msg switch
    {
        // Loading state
        LoadArticles => 
            (model with { IsLoading = true, Error = null }, new LoadArticlesCommand()),
        
        // Success
        ArticlesLoaded loaded => 
            (model with { IsLoading = false, Articles = loaded.Articles }, Commands.None),
        
        // Error
        LoadArticlesFailed failed => 
            (model with { IsLoading = false, Error = failed.Error }, Commands.None),
        
        // Dismiss error
        DismissError => 
            (model with { Error = null }, Commands.None),
        
        // Retry
        RetryLoad => 
            (model with { Error = null }, new LoadArticlesCommand()),
        
        _ => (model, Commands.None)
    };
```

## Displaying Errors

Show errors in the view:

```csharp
static Element<Model, Unit> ErrorBanner(string error)
    => div(
        @class("error-banner"),
        role("alert"),
        ariaLive("polite"),
        span(text(error)),
        button(
            onClick(new DismissError()),
            ariaLabel("Dismiss error"),
            text("×")
        )
    );

public static Document View(Model model)
    => new("App", div(
        model.Error is not null ? ErrorBanner(model.Error) : empty(),
        model.IsLoading ? LoadingSpinner() : ArticleList(model.Articles)
    ));
```

### Form Validation Errors

Display multiple errors for forms:

```csharp
static Element<Model, Unit> ValidationErrors(IReadOnlyList<string> errors)
    => errors.Count == 0 ? empty() : ul(
        @class("error-list"),
        role("alert"),
        errors.Select(error => 
            li(@class("error-item"), text(error))
        ).ToArray()
    );

static Element<Model, Unit> FormField(
    string name, 
    string value, 
    string? error,
    Message onInput)
    => div(
        @class("form-field"),
        label(
            @for(name),
            text(name)
        ),
        input(
            id(name),
            Html.Attributes.name(name),
            Html.Attributes.value(value),
            ariaInvalid(error is not null ? "true" : "false"),
            ariaDescribedby(error is not null ? $"{name}-error" : null),
            onInput(v => onInput)
        ),
        error is not null 
            ? span(id($"{name}-error"), @class("field-error"), text(error)) 
            : empty()
    );
```

## Result Type Pattern

Use a Result type for operations that can fail:

```csharp
public interface Result<T>
{
    record Ok(T Value) : Result<T>;
    record Error(string Message, IReadOnlyList<string>? Details = null) : Result<T>;
}

// In command handler
async Task<Result<Article>> CreateArticle(CreateArticleRequest request)
{
    try
    {
        var response = await httpClient.PostAsJsonAsync("/api/articles", request);
        
        if (response.IsSuccessStatusCode)
        {
            var article = await response.Content.ReadFromJsonAsync<Article>();
            return new Result<Article>.Ok(article);
        }
        
        var errors = await response.Content.ReadFromJsonAsync<ErrorsResponse>();
        return new Result<Article>.Error(
            "Failed to create article", 
            errors?.Errors
        );
    }
    catch (Exception ex)
    {
        return new Result<Article>.Error(ex.Message);
    }
}
```

## Global Error Handling

Catch unhandled exceptions:

```csharp
public static async Task HandleCommand(Command command, Func<Message, ValueTuple> dispatch)
{
    try
    {
        await HandleCommandInternal(command, dispatch);
    }
    catch (Exception ex)
    {
        // Log error
        Console.WriteLine($"Unhandled error: {ex}");
        
        // Notify user
        dispatch(new ApiError(
            "An unexpected error occurred",
            ex.Message
        ));
    }
}
```

## Retry Logic

Implement retry for transient failures:

```csharp
public record Model(
    int RetryCount,
    int MaxRetries
);

public static (Model, Command) Update(Message msg, Model model)
    => msg switch
    {
        LoadArticlesFailed failed when model.RetryCount < model.MaxRetries =>
        {
            // Wait before retrying
            var delay = TimeSpan.FromSeconds(Math.Pow(2, model.RetryCount));
            return (
                model with { RetryCount = model.RetryCount + 1 },
                new DelayedCommand(delay, new LoadArticlesCommand())
            );
        },
        
        LoadArticlesFailed failed =>
            (model with { Error = $"Failed after {model.MaxRetries} retries: {failed.Error}" }, Commands.None),
        
        ArticlesLoaded loaded =>
            (model with { RetryCount = 0, Articles = loaded.Articles }, Commands.None),
        
        _ => (model, Commands.None)
    };
```

## Optimistic Updates with Rollback

Show changes immediately and roll back on error:

```csharp
public record Model(
    IReadOnlyList<Article> Articles,
    Article? PendingDelete
);

public static (Model, Command) Update(Message msg, Model model)
    => msg switch
    {
        // Optimistically remove article
        DeleteArticle delete =>
        {
            var article = model.Articles.First(a => a.Slug == delete.Slug);
            var remaining = model.Articles.Where(a => a.Slug != delete.Slug).ToList();
            return (
                model with { Articles = remaining, PendingDelete = article },
                new DeleteArticleCommand(delete.Slug)
            );
        },
        
        // Confirm deletion
        ArticleDeleted =>
            (model with { PendingDelete = null }, Commands.None),
        
        // Rollback on failure
        DeleteArticleFailed failed when model.PendingDelete is not null =>
        {
            var restored = model.Articles.Append(model.PendingDelete).ToList();
            return (
                model with { 
                    Articles = restored, 
                    PendingDelete = null,
                    Error = failed.Error 
                },
                Commands.None
            );
        },
        
        _ => (model, Commands.None)
    };
```

## HTTP Status Code Handling

Map status codes to user-friendly messages:

```csharp
static string GetErrorMessage(HttpStatusCode status, string? serverMessage = null)
    => status switch
    {
        HttpStatusCode.BadRequest => serverMessage ?? "Invalid request",
        HttpStatusCode.Unauthorized => "Please log in to continue",
        HttpStatusCode.Forbidden => "You don't have permission for this action",
        HttpStatusCode.NotFound => "The requested item was not found",
        HttpStatusCode.Conflict => serverMessage ?? "This conflicts with existing data",
        HttpStatusCode.TooManyRequests => "Too many requests. Please wait and try again.",
        HttpStatusCode.InternalServerError => "Server error. Please try again later.",
        HttpStatusCode.ServiceUnavailable => "Service is temporarily unavailable",
        _ => serverMessage ?? $"Request failed: {status}"
    };
```

## Best Practices

### 1. Be Specific

```csharp
// ❌ Vague
dispatch(new Error("Something went wrong"));

// ✅ Specific
dispatch(new LoadArticlesFailed("Network timeout after 30 seconds"));
```

### 2. Preserve Context

```csharp
// ❌ Lost context
catch (Exception ex)
{
    dispatch(new Error(ex.Message));
}

// ✅ Preserved context
catch (Exception ex)
{
    dispatch(new LoadArticlesFailed(
        $"Failed to load articles: {ex.Message}"
    ));
}
```

### 3. Allow Recovery

```csharp
// ❌ No way to recover
static Element<Model, Unit> ErrorView(string error)
    => div(text(error));

// ✅ User can retry
static Element<Model, Unit> ErrorView(string error)
    => div(
        text(error),
        button(onClick(new RetryLoad()), text("Try Again"))
    );
```

### 4. Log for Debugging

```csharp
catch (Exception ex)
{
    Console.WriteLine($"Error in {command.GetType().Name}: {ex}");
    dispatch(new ApiError(GetUserMessage(ex)));
}
```

### 5. Clear Errors Appropriately

```csharp
public static (Model, Command) Update(Message msg, Model model)
    => msg switch
    {
        // Clear error on new action
        LoadArticles => 
            (model with { Error = null, IsLoading = true }, new LoadArticlesCommand()),
        
        // Clear error on navigation
        Navigate _ => 
            (model with { Error = null }, new Navigation.Command.PushState(/* ... */)),
        
        _ => (model, Commands.None)
    };
```

## See Also

- [Concepts: Commands & Effects](../concepts/commands-effects.md) — Async operations
- [Tutorial: API Integration](../tutorials/03-api-integration.md) — Working with APIs
- [Guide: Debugging](./debugging.md) — Troubleshooting errors
