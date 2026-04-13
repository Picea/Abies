# Next Steps

Now that you've built your first Abies application, here's where to go next.

## Learn the Concepts

Understand the foundations:

1. **[MVU Architecture](../concepts/mvu-architecture.md)** — The Model-View-Update pattern and how Abies implements it on the Picea kernel
2. **[Pure Functions](../concepts/pure-functions.md)** — Why purity matters and how Abies enforces it
3. **[Commands and Effects](../concepts/commands-effects.md)** — Handling side effects with the interpreter pattern
4. **[Subscriptions](../concepts/subscriptions.md)** — Listening to timers, WebSockets, and other external events
5. **[Composition](../concepts/components.md)** — Building reusable UI with function composition, memoization, and the Node type hierarchy
6. **[Virtual DOM](../concepts/virtual-dom.md)** — How diffing, patching, and the binary batch protocol work

## Understand Render Modes

Abies supports four render modes — choose the right one for your use case:

| Mode | What it does | Best for |
| ---- | ------------ | -------- |
| **Static** | One-shot HTML, zero JavaScript | Landing pages, emails |
| **InteractiveServer** | Server-side MVU over WebSocket | Dashboards, admin panels |
| **InteractiveWasm** | Client-side WASM after download | SPAs, offline-first |
| **InteractiveAuto** | Server-first, then WASM handoff | Best of both worlds |

Read the full guide: **[Render Modes](../concepts/render-modes.md)** and **[Choosing a Render Mode](../guides/render-mode-selection.md)**.

## Add Real Features

Progress from the counter to real-world patterns:

### HTTP Requests

```csharp
// Define a command
public record FetchArticles : Command;

// Return it from Transition
case LoadPage:
    return (model with { IsLoading = true }, new FetchArticles());

// Handle it in the interpreter
case FetchArticles:
    var articles = await api.GetArticles();
    return Ok([new ArticlesLoaded(articles)]);
```

See: [Commands and Effects](../concepts/commands-effects.md)

### Client-Side Routing

```csharp
// Subscribe to URL changes
public static Subscription Subscriptions(Model model)
    => Navigation.UrlChanges(url => new UrlChanged(url));

// Handle URL changes in Transition
case UrlChanged changed:
    var page = Router.Parse(changed.Url);
    return (model with { Page = page }, Commands.None);

// Navigate programmatically
case GoToArticle(var slug):
    return (model, Navigation.PushUrl(
        new Url(["article", slug], new(), Option<string>.None)));
```

### Timers and Subscriptions

```csharp
public static Subscription Subscriptions(Model model)
    => model.IsRunning
        ? SubscriptionModule.Every(TimeSpan.FromSeconds(1), () => new Tick())
        : SubscriptionModule.None;
```

See: [Subscriptions](../concepts/subscriptions.md)

### Performance with Memoization

```csharp
// Skip diffing for unchanged items
lazy((item.Id, item.UpdatedAt), () =>
    ItemView(item))
```

See: [Composition](../concepts/components.md)

## Study the Conduit Application

The **Conduit** application is a full-featured social blogging platform (Medium clone) that showcases all Abies capabilities:

- ✅ Authentication (JWT)
- ✅ CRUD operations (articles, comments)
- ✅ Client-side routing
- ✅ Pagination
- ✅ Favorites and following
- ✅ Real-world API integration
- ✅ E2E tests with Playwright

Browse it: [Picea.Abies.Conduit](https://github.com/Picea/Abies)

## Run the Benchmarks

Abies includes a full js-framework-benchmark integration. See how it compares to Blazor and vanilla JavaScript:

```bash
# In the js-framework-benchmark directory
npm run bench -- --headless keyed/abies
```

Current benchmark results are published in [Performance Benchmarks](../benchmarks.md).

## Explore the Ecosystem

| Package | Purpose |
| ------- | ------- |
| `Picea.Abies` | Core MVU runtime |
| `Picea.Abies.Browser` | WASM platform (DOM interop) |
| `Picea.Abies.Server` | Server-side rendering |
| `Picea.Abies.Server.Kestrel` | Kestrel integration |
| `Picea` | The kernel (automaton theory) |

## Get Help

- **[GitHub Issues](https://github.com/Picea/Abies/issues)** — Report bugs, request features
- **[GitHub Discussions](https://github.com/Picea/Abies/discussions)** — Ask questions, share ideas
- **[Contributing Guide](https://github.com/Picea/Abies/blob/main/CONTRIBUTING.md)** — Help build Abies
