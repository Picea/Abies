using System.Linq;
using System.Runtime.Versioning;
using Abies;
using Abies.DOM;
using Abies.Html;
using Praefixum;
using static Abies.Html.Attributes;
using static Abies.Html.Elements;
using static Abies.Html.Events;

[assembly: SupportedOSPlatform("browser")]

await Runtime.Run<Presentation, Arguments, Model>(new Arguments());

public record Arguments;

public record Model(
    int SlideIndex,
    int DemoCount,
    int TickCount,
    DateTimeOffset? LastTick,
    bool DemoTimer,
    bool TrackMouse,
    int MouseX,
    int MouseY,
    IReadOnlyList<string> Log,
    DateTimeOffset? LastMouseLogAt,
    string DemoInput);

public interface Message : Abies.Message
{
    public sealed record NextSlide : Message;
    public sealed record PrevSlide : Message;
    public sealed record GoToSlide(int Index) : Message;
    public sealed record KeyPressed(string Key, bool Repeat) : Message;
    public sealed record IncrementDemo : Message;
    public sealed record ResetDemo : Message;
    public sealed record ToggleDemoTimer : Message;
    public sealed record Tick(DateTimeOffset Now) : Message;
    public sealed record ToggleMouse : Message;
    public sealed record MouseMoved(PointerEventData Data, DateTimeOffset At) : Message;
    public sealed record ClearLog : Message;
    public sealed record DemoInputChanged(string Value) : Message;
    public sealed record NoOp : Message;
}

public class Presentation : Program<Model, Arguments>
{
    // ═══════════════════════════════════════════════════════════════════════════════
    // SLIDE DECK: Abies Conference Keynote (~1 hour presentation, 36 slides)
    // ═══════════════════════════════════════════════════════════════════════════════
    private static readonly Slide[] Slides =
    [
        // ───────────────────────────────────────────────────────────────────────────
        // SECTION 1: INTRODUCTION (5 slides, ~8 minutes)
        // ───────────────────────────────────────────────────────────────────────────
        new(
            Id: "title",
            Kicker: "Conference Keynote",
            Title: "Abies: Functional Web Apps in .NET",
            Subtitle: "Building reliable, testable, and beautiful web applications with Model-View-Update",
            Points:
            [
                "WebAssembly-powered .NET framework",
                "Elm-inspired architecture for C# developers",
                "Pure functions, immutable state, explicit effects"
            ],
            Code: null,
            Callout: "Navigate with arrow keys or Space",
            Takeaway: "A new paradigm for .NET web development",
            NextStep: "What problems does Abies solve?",
            Kind: SlideKind.Intro
        ),
        new(
            Id: "why-abies",
            Kicker: "The Problem",
            Title: "Why Another Web Framework?",
            Subtitle: "Traditional approaches have fundamental challenges that compound over time",
            Points:
            [
                "State scattered across components",
                "Side effects hidden in lifecycle methods",
                "Testing requires mocking the world",
                "Debugging is archaeology, not science"
            ],
            Code: null,
            Callout: "\"Where did this state come from?\" - Every developer, eventually",
            Takeaway: "Complexity grows faster than features",
            NextStep: "What if state transitions were explicit?",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "philosophy",
            Kicker: "Core Philosophy",
            Title: "Functional Programming for the Real World",
            Subtitle: "Abies applies proven FP principles without requiring a PhD",
            Points:
            [
                "Immutable data: state never mutated, always replaced",
                "Pure functions: same input = same output, always",
                "Explicit effects: side effects are data, not surprises",
                "C# records + pattern matching = natural fit"
            ],
            Code: "// State is data\npublic record Model(int Count, string Name);\n\n// Messages describe what happened\npublic record Incremented : Message;\npublic record NameChanged(string Value) : Message;",
            Callout: "If Elm and C# had a baby...",
            Takeaway: "FP makes complex apps simpler",
            NextStep: "Meet the MVU architecture",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "mvu-overview",
            Kicker: "Architecture",
            Title: "Model-View-Update: The Complete Picture",
            Subtitle: "One loop to rule them all - every app follows the same pattern",
            Points:
            [
                "Model: single source of truth for all state",
                "View: pure function renders state to virtual DOM",
                "Update: pure function handles state transitions",
                "Runtime: orchestrates the loop, handles effects"
            ],
            Code: "Model -> View -> User Interaction -> Message -> Update -> New Model -> View -> ...",
            Callout: "Unidirectional data flow eliminates 90% of bugs",
            Takeaway: "One mental model for everything",
            NextStep: "Let's build something!",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "agenda",
            Kicker: "Today's Journey",
            Title: "What We'll Cover",
            Subtitle: "From first principles to production-ready applications",
            Points:
            [
                "Part 1: The MVU Loop - Model, View, Update",
                "Part 2: HTML API - Elements, attributes, events",
                "Part 3: Effects - Commands and subscriptions",
                "Part 4: Real World - Routing, forms, API calls",
                "Part 5: Testing - Why it's almost too easy"
            ],
            Code: null,
            Callout: "Ask questions anytime!",
            Takeaway: "You'll build a complete app today",
            NextStep: "Start with the Model",
            Kind: SlideKind.Intro
        ),

        // ───────────────────────────────────────────────────────────────────────────
        // SECTION 2: THE MODEL (4 slides, ~7 minutes)
        // ───────────────────────────────────────────────────────────────────────────
        new(
            Id: "model-intro",
            Kicker: "Part 1: The Model",
            Title: "State as Plain Data",
            Subtitle: "Your entire application state in one immutable record",
            Points:
            [
                "Single source of truth - no hidden state",
                "Immutable - use 'with' expressions to update",
                "Serializable - time-travel debugging is free",
                "Testable - compare values, not references"
            ],
            Code: "public record Model(\n    User? CurrentUser,\n    IReadOnlyList<Article> Articles,\n    bool IsLoading,\n    string? Error,\n    Route CurrentRoute\n);",
            Callout: "Records give you immutability + equality for free",
            Takeaway: "All state lives in the Model",
            NextStep: "How do we change state?",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "messages",
            Kicker: "State Transitions",
            Title: "Messages: What Happened",
            Subtitle: "Events are data - explicit, typed, and exhaustive",
            Points:
            [
                "Messages describe events, not commands",
                "Past tense naming: Clicked, Loaded, Failed",
                "Carry only necessary data",
                "Pattern matching ensures all cases handled"
            ],
            Code: "public interface Message : Abies.Message\n{\n    // User actions\n    public record ButtonClicked : Message;\n    public record TextEntered(string Value) : Message;\n\n    // System events\n    public record ArticlesLoaded(List<Article> Data) : Message;\n    public record LoadFailed(string Error) : Message;\n}",
            Callout: "No string-based action types!",
            Takeaway: "Messages are the only way to change state",
            NextStep: "Wire messages to state changes",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "update-function",
            Kicker: "The Brain",
            Title: "Update: Pure State Transitions",
            Subtitle: "Given a message and current state, return new state",
            Points:
            [
                "Pure function - no side effects",
                "Pattern matching for exhaustive handling",
                "Returns tuple: (new model, command)",
                "Command = description of side effect to perform"
            ],
            Code: "public static (Model, Command) Update(Message msg, Model model)\n    => msg switch\n    {\n        Increment => (model with { Count = model.Count + 1 }, Commands.None),\n        Decrement => (model with { Count = model.Count - 1 }, Commands.None),\n        Reset => (model with { Count = 0 }, Commands.None),\n        _ => (model, Commands.None)\n    };",
            Callout: "This is your entire business logic surface area",
            Takeaway: "Update is trivially testable",
            NextStep: "Display the state with View",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "immutability-demo",
            Kicker: "Live Demo",
            Title: "Immutable Updates in Action",
            Subtitle: "Watch the MVU loop process messages in real-time",
            Points:
            [
                "Each message creates a new model",
                "Previous states are preserved",
                "Time-travel debugging becomes possible",
                "State transitions are fully traceable"
            ],
            Code: "// Old model is never modified\nvar oldModel = new Model(Count: 5);\nvar newModel = oldModel with { Count = 6 };\n\n// oldModel.Count is still 5\n// newModel.Count is 6",
            Callout: "Try the buttons in the demo panel",
            Takeaway: "Immutability eliminates whole categories of bugs",
            NextStep: "Now let's render this state",
            Kind: SlideKind.Demo
        ),

        // ───────────────────────────────────────────────────────────────────────────
        // SECTION 3: THE VIEW (5 slides, ~8 minutes)
        // ───────────────────────────────────────────────────────────────────────────
        new(
            Id: "view-intro",
            Kicker: "Part 2: The View",
            Title: "UI as a Pure Function",
            Subtitle: "Same model always produces the same output",
            Points:
            [
                "View(model) -> Virtual DOM tree",
                "No side effects, no surprises",
                "Compose complex UIs from simple functions",
                "Abies handles the actual DOM updates"
            ],
            Code: "public static Document View(Model model)\n    => new(\"My App\",\n        div([class_(\"container\")], [\n            h1([], [text($\"Count: {model.Count}\")]),\n            button([onclick(new Increment())], [text(\"+\")]),\n            button([onclick(new Decrement())], [text(\"-\")])\n        ]));",
            Callout: "Declarative UI: describe what, not how",
            Takeaway: "Views are just data transformations",
            NextStep: "Explore the HTML API",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "html-elements",
            Kicker: "HTML API",
            Title: "Elements: Building Blocks",
            Subtitle: "Type-safe functions for every HTML element",
            Points:
            [
                "All standard HTML elements as functions",
                "div, span, button, input, form, table...",
                "Custom elements via element(\"tag\", ...)",
                "No string templates, no XSS vulnerabilities"
            ],
            Code: "// Standard elements\ndiv([class_(\"card\")], [\n    header([], [h2([], [text(\"Title\")])]),\n    p([], [text(\"Content goes here\")]),\n    footer([], [small([], [text(\"Footer\")])])\n])\n\n// Custom/Web components\nelement(\"fluent-button\", [attribute(\"appearance\", \"accent\")], [\n    text(\"Click me\")\n])",
            Callout: "IntelliSense guides you through the API",
            Takeaway: "HTML is just C# function calls",
            NextStep: "Add attributes and styles",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "html-attributes",
            Kicker: "HTML API",
            Title: "Attributes: Configuring Elements",
            Subtitle: "Type-safe attributes with proper naming",
            Points:
            [
                "class_() - trailing underscore avoids C# keyword clash",
                "id(), href(), src(), type(), value()...",
                "Boolean attributes: disabled(), checked(), readonly_()",
                "ARIA: role(), ariaLabel(), ariaDescribedby()"
            ],
            Code: "input([\n    type(\"email\"),\n    id(\"user-email\"),\n    class_(\"form-input\"),\n    placeholder(\"Enter your email\"),\n    required(),\n    ariaLabel(\"Email address\"),\n    autocomplete(\"email\")\n], [])",
            Callout: "Accessibility built into the API",
            Takeaway: "Attributes are validated at compile time",
            NextStep: "Handle user interactions",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "html-events",
            Kicker: "HTML API",
            Title: "Events: User Interactions",
            Subtitle: "Strongly-typed event handlers that dispatch messages",
            Points:
            [
                "onclick(), oninput(), onchange(), onsubmit()...",
                "Simple: dispatch a message directly",
                "With data: extract event payload first",
                "Keyboard/mouse: full event data available"
            ],
            Code: "// Simple click\nbutton([onclick(new Submit())], [text(\"Submit\")])\n\n// Input with value extraction\ninput([\n    type(\"text\"),\n    value(model.Query),\n    oninput(data => new QueryChanged(data?.Value ?? \"\"))\n], [])\n\n// Keyboard events\ndiv([onkeydown(e => new KeyPressed(e.Key, e.CtrlKey))], [...])",
            Callout: "Events become messages, messages trigger Update",
            Takeaway: "User actions flow into the MVU loop",
            NextStep: "See it all together",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "virtual-dom",
            Kicker: "Under the Hood",
            Title: "Virtual DOM: Efficient Updates",
            Subtitle: "Abies calculates minimal DOM changes automatically",
            Points:
            [
                "View returns a virtual DOM tree (just data)",
                "Abies diffs old vs new virtual DOM",
                "Only changed elements are updated in browser",
                "Use id: for keyed diffing (unlike React's key=)"
            ],
            Code: "// Use id: for stable element identity in lists\n// Unlike React/Vue/Elm, no separate \"key\" attribute needed!\nul([], model.Items.Select(item =>\n    li([], [text(item.Name)], id: $\"item-{item.Id}\")\n))\n\n// Why? Praefixum generates unique IDs at compile time\n// so every element already has an ID for patching.\n// ADR-016 explains the full design.",
            Callout: "One concept (id:) instead of two (key + id)",
            Takeaway: "Performance without manual optimization",
            NextStep: "Handle side effects",
            Kind: SlideKind.Concept
        ),

        // ───────────────────────────────────────────────────────────────────────────
        // SECTION 4: COMMANDS & EFFECTS (5 slides, ~8 minutes)
        // ───────────────────────────────────────────────────────────────────────────
        new(
            Id: "effects-intro",
            Kicker: "Part 3: Side Effects",
            Title: "Commands: Effects as Data",
            Subtitle: "Describe what should happen, don't do it directly",
            Points:
            [
                "Update returns (model, command)",
                "Command is a description, not execution",
                "Runtime executes commands asynchronously",
                "Results dispatch new messages"
            ],
            Code: "public static (Model, Command) Update(Message msg, Model model)\n    => msg switch\n    {\n        LoadArticles => (\n            model with { IsLoading = true },\n            new FetchArticlesCommand()  // Describe the effect\n        ),\n        ArticlesLoaded loaded => (\n            model with { Articles = loaded.Data, IsLoading = false },\n            Commands.None\n        ),\n        _ => (model, Commands.None)\n    };",
            Callout: "Update stays pure - no async, no try/catch",
            Takeaway: "Commands separate 'what' from 'how'",
            NextStep: "Execute commands in HandleCommand",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "handle-command",
            Kicker: "Effect Execution",
            Title: "HandleCommand: Where Effects Live",
            Subtitle: "Async operations, API calls, and browser interactions",
            Points:
            [
                "Switch on command type",
                "Perform async work (API calls, storage, etc.)",
                "Dispatch result messages back to Update",
                "Error handling in one place"
            ],
            Code: "public static async Task HandleCommand(\n    Command cmd,\n    Func<Message, ValueTuple> dispatch)\n{\n    switch (cmd)\n    {\n        case FetchArticlesCommand:\n            try {\n                var articles = await _api.GetArticles();\n                dispatch(new ArticlesLoaded(articles));\n            } catch (Exception ex) {\n                dispatch(new LoadFailed(ex.Message));\n            }\n            break;\n    }\n}",
            Callout: "All async complexity is isolated here",
            Takeaway: "Effects are explicit and testable",
            NextStep: "Built-in navigation commands",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "navigation-commands",
            Kicker: "Built-in Commands",
            Title: "Navigation: URL Management",
            Subtitle: "Abies includes commands for SPA routing",
            Points:
            [
                "PushState - navigate, add history entry",
                "ReplaceState - navigate, no history entry",
                "Load - full page navigation (external links)",
                "Back/Forward - history navigation"
            ],
            Code: "// Navigate to a new page\nnew Navigation.Command.PushState(Url.Create(\"/articles/123\"))\n\n// Replace current URL without history\nnew Navigation.Command.ReplaceState(Url.Create(\"/articles?page=2\"))\n\n// External link\nnew Navigation.Command.Load(\"https://github.com\")",
            Callout: "URLs are first-class citizens",
            Takeaway: "Routing is just another command",
            NextStep: "Subscribe to external events",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "subscriptions-concept",
            Kicker: "External Events",
            Title: "Subscriptions: Push-Based Inputs",
            Subtitle: "Timers, keyboard, mouse, WebSocket - all become messages",
            Points:
            [
                "Declare active subscriptions based on model",
                "Runtime manages subscribe/unsubscribe lifecycle",
                "Keyed for stability across re-renders",
                "Compose multiple subscriptions with Batch"
            ],
            Code: "public static Subscription Subscriptions(Model model)\n{\n    var subs = new List<Subscription>\n    {\n        SubscriptionModule.OnKeyDown(e => new KeyPressed(e.Key))\n    };\n\n    if (model.IsPlaying)\n        subs.Add(SubscriptionModule.Every(\n            TimeSpan.FromSeconds(1),\n            now => new Tick(now)));\n\n    return SubscriptionModule.Batch(subs);\n}",
            Callout: "Subscriptions are declarative, not imperative",
            Takeaway: "External world flows into messages",
            NextStep: "See subscriptions live",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "subscriptions-demo",
            Kicker: "Live Demo",
            Title: "Subscriptions in Action",
            Subtitle: "Watch timer ticks and input events flow through the loop",
            Points:
            [
                "Timer subscription fires every second",
                "Keyboard events captured globally",
                "Mouse tracking shows position updates",
                "Toggle subscriptions on/off dynamically"
            ],
            Code: "// Currently active subscriptions:\n// - OnKeyDown -> KeyPressed message\n// - Every(1s) -> Tick message (when timer on)\n// - OnMouseMove -> MouseMoved (when tracking on)",
            Callout: "Try the toggle buttons in the demo panel",
            Takeaway: "Subscriptions react to model changes",
            NextStep: "Build a real application",
            Kind: SlideKind.Demo
        ),

        // ───────────────────────────────────────────────────────────────────────────
        // SECTION 5: REAL WORLD PATTERNS (6 slides, ~10 minutes)
        // ───────────────────────────────────────────────────────────────────────────
        new(
            Id: "routing-intro",
            Kicker: "Part 4: Real World",
            Title: "Client-Side Routing",
            Subtitle: "Parse URLs into typed routes, render the right page",
            Points:
            [
                "Route is part of your model",
                "URL changes dispatch messages",
                "View switches based on current route",
                "Deep links work automatically"
            ],
            Code: "public interface Route\n{\n    record Home : Route;\n    record Article(string Slug) : Route;\n    record Profile(string Username) : Route;\n    record Settings : Route;\n    record NotFound : Route;\n}",
            Callout: "Routes are just data, like everything else",
            Takeaway: "Navigation is state, not magic",
            NextStep: "Parse URLs into routes",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "url-parsing",
            Kicker: "URL Parsing",
            Title: "Parser Combinators for Routing",
            Subtitle: "Compose parsers like LEGO blocks for type-safe URL handling",
            Points:
            [
                "Match segments, extract parameters",
                "Combine with .Then(), .Map()",
                "Type-safe: compiler catches route errors",
                "Bidirectional: also generate URLs from routes"
            ],
            Code: "static Route Parse(Url url) =>\n    Route.Parse.Match(url.Path,\n        Route.Parse.Segment(\"article\")\n            .Then(Route.Parse.String)\n            .Map(slug => (Route)new Route.Article(slug)),\n        Route.Parse.Segment(\"profile\")\n            .Then(Route.Parse.String)\n            .Map(user => (Route)new Route.Profile(user)),\n        Route.Parse.End\n            .Map(_ => (Route)new Route.Home())\n    ) ?? new Route.NotFound();",
            Callout: "No regex, no string matching, no typos",
            Takeaway: "URLs are typed at compile time",
            NextStep: "Handle forms and validation",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "forms",
            Kicker: "Form Handling",
            Title: "Forms: Controlled Inputs",
            Subtitle: "State-driven form fields with validation",
            Points:
            [
                "Form state lives in the model",
                "Input events update model via messages",
                "Validation runs on every change",
                "Submit triggers command with form data"
            ],
            Code: "public record FormModel(\n    string Title,\n    string Body,\n    IReadOnlyList<string> Errors\n);\n\n// In View:\ninput([\n    type(\"text\"),\n    value(model.Form.Title),\n    oninput(d => new TitleChanged(d?.Value ?? \"\")),\n    ariaInvalid(hasError ? \"true\" : \"false\")\n], [])",
            Callout: "Form state is debuggable and testable",
            Takeaway: "Forms follow the same MVU pattern",
            NextStep: "Connect to APIs",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "api-integration",
            Kicker: "HTTP Clients",
            Title: "API Integration: Commands + HttpClient",
            Subtitle: "Clean separation between UI logic and network calls",
            Points:
            [
                "HttpClient injected into HandleCommand",
                "API calls return domain types or errors",
                "Loading states handled in model",
                "Retry logic isolated from UI"
            ],
            Code: "case CreateArticleCommand create:\n    try {\n        var response = await _http.PostAsJsonAsync(\n            \"/api/articles\", create.Request);\n\n        if (response.IsSuccessStatusCode) {\n            var article = await response.Content\n                .ReadFromJsonAsync<Article>();\n            dispatch(new ArticleCreated(article!));\n        } else {\n            var errors = await response.Content\n                .ReadFromJsonAsync<ErrorsResponse>();\n            dispatch(new CreateFailed(errors!.Errors));\n        }\n    } catch (Exception ex) {\n        dispatch(new CreateFailed([ex.Message]));\n    }\n    break;",
            Callout: "API complexity stays out of your Update function",
            Takeaway: "Network code is isolated and testable",
            NextStep: "Component composition",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "components",
            Kicker: "Composition",
            Title: "Reusable Components",
            Subtitle: "Build UI libraries from pure functions",
            Points:
            [
                "Stateless: just a function returning Node",
                "Stateful: implement Element<TModel, TArg>",
                "Compose via model nesting + message routing",
                "No props drilling - explicit data flow"
            ],
            Code: "// Stateless component\nstatic Node Avatar(string url, string name, string size = \"md\")\n    => img([\n        src(url),\n        alt(name),\n        class_($\"avatar avatar-{size}\")\n    ], []);\n\n// Usage\ndiv([class_(\"user-card\")], [\n    Avatar(user.ImageUrl, user.Name, \"lg\"),\n    span([], [text(user.Name)])\n])",
            Callout: "Most components are just functions",
            Takeaway: "Composition over inheritance",
            NextStep: "Conduit: the reference app",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "conduit",
            Kicker: "Reference Application",
            Title: "Conduit: Real-World Example",
            Subtitle: "A full-featured Medium clone built with Abies",
            Points:
            [
                "Authentication (register, login, logout)",
                "Articles (CRUD, comments, favorites)",
                "Profiles (follow, view articles)",
                "Tags, pagination, error handling"
            ],
            Code: "// Conduit project structure:\n// Main.cs        - Program, Model, top-level Update\n// Route.cs       - URL parsing\n// Navigation.cs  - Route -> URL generation\n// Commands.cs    - API commands\n// Page/          - Page-specific models & views\n//   Home.cs, Article.cs, Editor.cs, Profile.cs\n// Services/      - API client, local storage",
            Callout: "~3000 lines of C# for a complete app",
            Takeaway: "MVU scales to real applications",
            NextStep: "Why testing is almost cheating",
            Kind: SlideKind.Concept
        ),

        // ───────────────────────────────────────────────────────────────────────────
        // SECTION 6: TESTING (4 slides, ~6 minutes)
        // ───────────────────────────────────────────────────────────────────────────
        new(
            Id: "testing-intro",
            Kicker: "Part 5: Testing",
            Title: "Testing Pure Functions is Trivial",
            Subtitle: "No mocks, no setup, no teardown - just input and output",
            Points:
            [
                "Update is pure: test with values",
                "View is pure: snapshot test the output",
                "Commands are data: assert on returned command",
                "No browser, no async, no flakiness"
            ],
            Code: "[Fact]\npublic void Increment_IncreasesCount()\n{\n    var model = new Model(Count: 5);\n\n    var (newModel, command) = Update(new Increment(), model);\n\n    Assert.Equal(6, newModel.Count);\n    Assert.Equal(Commands.None, command);\n}",
            Callout: "Your hardest test is easier than their easiest",
            Takeaway: "MVU makes testing almost boring",
            NextStep: "Test complex flows",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "testing-flows",
            Kicker: "Integration Tests",
            Title: "Testing Message Sequences",
            Subtitle: "Replay a sequence of messages, verify final state",
            Points:
            [
                "Simulate user journeys with message sequences",
                "Assert intermediate and final states",
                "No UI rendering required",
                "Fast, deterministic, reproducible"
            ],
            Code: "[Fact]\npublic void LoadArticles_ShowsArticlesWhenSuccessful()\n{\n    var model = Model.Initial;\n\n    // User triggers load\n    var (loading, cmd) = Update(new LoadArticles(), model);\n    Assert.True(loading.IsLoading);\n    Assert.IsType<FetchArticlesCommand>(cmd);\n\n    // API returns data\n    var articles = new[] { new Article(\"test\", \"Test\") };\n    var (loaded, _) = Update(new ArticlesLoaded(articles), loading);\n\n    Assert.False(loaded.IsLoading);\n    Assert.Single(loaded.Articles);\n}",
            Callout: "Full user journeys without a browser",
            Takeaway: "Test business logic, not framework code",
            NextStep: "E2E tests with Playwright",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "e2e-testing",
            Kicker: "End-to-End",
            Title: "Playwright for UI Verification",
            Subtitle: "Browser automation when you need visual confirmation",
            Points:
            [
                "Verify rendering in real browser",
                "Test accessibility, responsive design",
                "Integration with real API or mocks",
                "Complement unit tests, don't replace them"
            ],
            Code: "[Fact]\npublic async Task CanNavigateArticles()\n{\n    await Page.GotoAsync(\"/\");\n    await Expect(Page.Locator(\".article-preview\"))\n        .ToHaveCountAsync(10);\n\n    await Page.ClickAsync(\"text=Read more\");\n    await Expect(Page).ToHaveURLAsync(\"/article/\");\n    await Expect(Page.Locator(\"h1\")).ToBeVisibleAsync();\n}",
            Callout: "Unit tests are 95% of coverage, E2E is the final 5%",
            Takeaway: "Right tool for the right job",
            NextStep: "Debugging strategies",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "debugging",
            Kicker: "Troubleshooting",
            Title: "Debugging: Follow the Data",
            Subtitle: "When state is explicit, bugs have nowhere to hide",
            Points:
            [
                "Log messages as they arrive",
                "Snapshot model after each update",
                "Replay message history to reproduce bugs",
                "Time-travel: step backward through states"
            ],
            Code: "// Message logging middleware\npublic static (Model, Command) Update(Message msg, Model model)\n{\n    Console.WriteLine($\"Message: {msg}\");\n    Console.WriteLine($\"Before: {model}\");\n\n    var (newModel, cmd) = UpdateInternal(msg, model);\n\n    Console.WriteLine($\"After: {newModel}\");\n    Console.WriteLine($\"Command: {cmd}\");\n\n    return (newModel, cmd);\n}",
            Callout: "Bug reports become message sequences",
            Takeaway: "Debugging is data analysis, not guesswork",
            NextStep: "Performance and deployment",
            Kind: SlideKind.Concept
        ),

        // ───────────────────────────────────────────────────────────────────────────
        // SECTION 7: PRODUCTION (4 slides, ~6 minutes)
        // ───────────────────────────────────────────────────────────────────────────
        new(
            Id: "performance",
            Kicker: "Optimization",
            Title: "Performance Best Practices",
            Subtitle: "Abies is fast by default, but here's how to go faster",
            Points:
            [
                "Virtual DOM diff is O(n) - very fast",
                "Use id: for dynamic lists (not key= like React)",
                "Avoid deep nesting in view trees",
                "Memoize expensive static content"
            ],
            Code: "// Use id: for stable element identity\n// (Unlike React/Vue, no separate 'key' needed)\nul([], model.Items.Select(item =>\n    li([], [text(item.Name)], id: $\"item-{item.Id}\")\n))\n\n// Why no key()? Praefixum already gives every element\n// a unique ID. See ADR-016 for details.",
            Callout: "One concept (id:) instead of two (key + id)",
            Takeaway: "Declarative code is usually fast enough",
            NextStep: "WebAssembly deployment",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "deployment",
            Kicker: "Production",
            Title: "Deploying Abies Apps",
            Subtitle: "Static files to any hosting provider",
            Points:
            [
                "Output is static HTML + WASM + JS",
                "Deploy to Azure Static Web Apps, GitHub Pages, Netlify",
                "AOT compilation for faster startup",
                "Brotli compression reduces bundle size"
            ],
            Code: "# Published output\nbin/Release/net9.0/publish/wwwroot/\n  index.html\n  _framework/\n    dotnet.js\n    dotnet.wasm\n    YourApp.dll\n  css/\n\n# Build command\ndotnet publish -c Release",
            Callout: "No server required - pure static hosting",
            Takeaway: "Deploy anywhere that serves files",
            NextStep: "Observability with OpenTelemetry",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "observability",
            Kicker: "Monitoring",
            Title: "OpenTelemetry Integration",
            Subtitle: "Traces for every message through the MVU loop",
            Points:
            [
                "Built-in ActivitySource for tracing",
                "Each message creates a span",
                "Command execution traced separately",
                "Integrates with Aspire, Jaeger, etc."
            ],
            Code: "// Automatic tracing in Runtime:\nusing var activity = Instrumentation\n    .ActivitySource\n    .StartActivity(\"Message\");\n\nactivity?.SetTag(\"message.type\", msg.GetType().Name);\n\n// View traces in Aspire dashboard,\n// Jaeger, Zipkin, or any OTLP collector",
            Callout: "Production debugging with full context",
            Takeaway: "Observability built into the framework",
            NextStep: "Browser-to-backend tracing",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "distributed-tracing",
            Kicker: "End-to-End",
            Title: "Distributed Tracing: Browser to Backend",
            Subtitle: "Follow user journeys across the entire stack",
            Points:
            [
                "UI Events create parent spans in browser",
                "HTTP calls inherit trace context automatically",
                "W3C traceparent header propagates to API",
                "Verbosity levels: 'user' (default) or 'debug'"
            ],
            Code: "// User clicks button → trace begins\nUI Event: Click Button \"Submit\"     [Browser]\n├── HTTP POST /api/articles          [Browser]\n│   └── POST /api/articles           [API]\n│       └── INSERT article           [Database]\n\n// Configure verbosity:\n<meta name=\"otel-verbosity\" content=\"user\">\n// or at runtime:\nwindow.__otel.setVerbosity('debug');",
            Callout: "\"user\" traces actions, \"debug\" traces everything",
            Takeaway: "Trace context flows seamlessly across boundaries",
            NextStep: "The Abies ecosystem",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "ecosystem",
            Kicker: "Tooling",
            Title: "The Abies Ecosystem",
            Subtitle: "Everything you need to build production apps",
            Points:
            [
                "Abies - Core runtime and HTML API",
                "Abies.Conduit - Reference application",
                ".NET Aspire - Local development with tracing",
                "Playwright - E2E testing integration"
            ],
            Code: "// Project references\n<PackageReference Include=\"Abies\" Version=\"1.0.0\" />\n\n// Optional integrations\n<PackageReference Include=\"Aspire.Hosting\" />\n<PackageReference Include=\"Microsoft.Playwright\" />",
            Callout: "Batteries included, opinions optional",
            Takeaway: "Complete toolkit for web development",
            NextStep: "Recap and next steps",
            Kind: SlideKind.Concept
        ),

        // ───────────────────────────────────────────────────────────────────────────
        // SECTION 8: CONCLUSION (3 slides, ~5 minutes)
        // ───────────────────────────────────────────────────────────────────────────
        new(
            Id: "recap",
            Kicker: "Summary",
            Title: "What We Covered",
            Subtitle: "The complete Abies mental model",
            Points:
            [
                "MVU Loop: Model -> View -> Update -> Model",
                "Messages: explicit, typed events",
                "Commands: side effects as data",
                "Subscriptions: external events as messages",
                "Testing: pure functions = trivial tests"
            ],
            Code: null,
            Callout: "One architecture, zero surprises",
            Takeaway: "You can build anything with this pattern",
            NextStep: "Where to go from here",
            Kind: SlideKind.Outro
        ),
        new(
            Id: "resources",
            Kicker: "Learn More",
            Title: "Getting Started Resources",
            Subtitle: "Documentation, examples, and community",
            Points:
            [
                "docs/ - Comprehensive documentation",
                "Abies.Conduit - Full reference app",
                "Abies.Counter - Minimal starter",
                "GitHub Discussions - Community support",
                "GitHub Issues - Bug reports & features"
            ],
            Code: "# Clone the repo\ngit clone https://github.com/YourOrg/Abies\n\n# Run the counter example\ncd Abies.Counter\ndotnet run\n\n# Run the Conduit app\ncd Abies.Conduit\ndotnet run",
            Callout: "Star the repo if you like what you see!",
            Takeaway: "The documentation covers everything",
            NextStep: "Questions?",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "thanks",
            Kicker: "Thank You!",
            Title: "Questions & Discussion",
            Subtitle: "Let's build something amazing together",
            Points:
            [
                "Architecture questions",
                "Implementation details",
                "Migration strategies",
                "Your use cases"
            ],
            Code: null,
            Callout: "Find me afterwards for deep dives!",
            Takeaway: "Functional web development is here",
            NextStep: "Go build something!",
            Kind: SlideKind.Outro
        )
    ];

    public static (Model, Command) Initialize(Url url, Arguments argument)
    {
        var initialIndex = TryGetSlideIndex(url) ?? 0;
        return (new Model(
            SlideIndex: ClampSlide(initialIndex),
            DemoCount: 0,
            TickCount: 0,
            LastTick: null,
            DemoTimer: true,
            TrackMouse: false,
            MouseX: 0,
            MouseY: 0,
            Log: [],
            LastMouseLogAt: null,
            DemoInput: ""),
            Commands.None);
    }

    public static Abies.Message OnLinkClicked(UrlRequest urlRequest)
        => urlRequest switch
        {
            UrlRequest.Internal internalRequest => TryGetSlideIndex(internalRequest.Url) is { } index
                ? new Message.GoToSlide(index)
                : new Message.NoOp(),
            _ => new Message.NoOp()
        };

    public static Abies.Message OnUrlChanged(Url url)
        => TryGetSlideIndex(url) is { } index
            ? new Message.GoToSlide(index)
            : new Message.NoOp();

    public static Subscription Subscriptions(Model model)
    {
        var subscriptions = new List<Subscription>
        {
            SubscriptionModule.OnKeyDown(evt => new Message.KeyPressed(evt.Key, evt.Repeat))
        };

        if (model.DemoTimer)
        {
            subscriptions.Add(SubscriptionModule.Every(TimeSpan.FromSeconds(1), now => new Message.Tick(now)));
        }

        if (model.TrackMouse)
        {
            subscriptions.Add(SubscriptionModule.OnMouseMove(evt => new Message.MouseMoved(evt, DateTimeOffset.UtcNow)));
        }

        return SubscriptionModule.Batch(subscriptions);
    }

    public static Task HandleCommand(Command command, Func<Abies.Message, System.ValueTuple> dispatch)
        => Task.CompletedTask;

    public static (Model model, Command command) Update(Abies.Message message, Model model)
        => message switch
        {
            Message.NextSlide => SetSlide(model, ClampSlide(model.SlideIndex + 1)),
            Message.PrevSlide => SetSlide(model, ClampSlide(model.SlideIndex - 1)),
            Message.GoToSlide goTo => SetSlide(model, ClampSlide(goTo.Index)),
            Message.KeyPressed key => HandleKeyPress(key.Key, key.Repeat, model),
            Message.IncrementDemo => (
                model with
                {
                    DemoCount = model.DemoCount + 1,
                    Log = AddLog(model.Log, "Demo increment", DateTimeOffset.UtcNow)
                },
                Commands.None
            ),
            Message.ResetDemo => (
                model with
                {
                    DemoCount = 0,
                    TickCount = 0,
                    LastTick = null,
                    Log = AddLog(model.Log, "Demo reset", DateTimeOffset.UtcNow)
                },
                Commands.None
            ),
            Message.ToggleDemoTimer => (
                model with
                {
                    DemoTimer = !model.DemoTimer,
                    Log = AddLog(model.Log, $"Timer {(model.DemoTimer ? "off" : "on")}", DateTimeOffset.UtcNow)
                },
                Commands.None
            ),
            Message.ToggleMouse => (
                model with
                {
                    TrackMouse = !model.TrackMouse,
                    Log = AddLog(model.Log, $"Mouse tracking {(model.TrackMouse ? "off" : "on")}", DateTimeOffset.UtcNow)
                },
                Commands.None
            ),
            Message.Tick tick => (
                model with
                {
                    TickCount = model.TickCount + 1,
                    LastTick = tick.Now,
                    Log = AddLog(model.Log, "Tick", tick.Now)
                },
                Commands.None
            ),
            Message.MouseMoved moved => (
                UpdateMouse(model, moved),
                Commands.None
            ),
            Message.ClearLog => (model with { Log = [] }, Commands.None),
            Message.DemoInputChanged changed => (model with { DemoInput = changed.Value }, Commands.None),
            Message.NoOp => (model, Commands.None),
            _ => (model, Commands.None)
        };

    public static Document View(Model model)
    {
        var slide = Slides[model.SlideIndex];
        var slideNumber = model.SlideIndex + 1;
        var progress = (double)slideNumber / Slides.Length * 100;

        return new Document("Abies Presentation",
            div([class_("app")],
            [
                header([class_("topbar")],
                [
                    div([class_("brand")],
                    [
                        img([class_("brand-logo"), src("abies-logo.svg"), alt("Abies logo")]),
                        div([class_("brand-meta")],
                        [
                            div([class_("brand-line")],
                            [
                                span([class_("brand-wordmark")], [text("Abies")]),
                                span([class_("brand-title")], [text("MVU in production")]),
                                FluentBadge([class_("brand-badge"), attribute("appearance", "tint")], [text("Conference edition")])
                            ]),
                            span([class_("brand-subtitle")], [text("Abies conference keynote")])
                        ])
                    ]),
                    div([class_("topbar-actions")],
                    [
                        FluentBadge([class_("pill")], [text($"Slide {slideNumber}/{Slides.Length}")]),
                        div([class_("keys")],
                        [
                            kbd([class_("key")], [text("<")]),
                            kbd([class_("key")], [text(">")]),
                            kbd([class_("key")], [text("Space")])
                        ])
                    ])
                ]),
                div([class_("progress"), role("progressbar"), ariaValuemin("0"), ariaValuemax("100"), ariaValuenow($"{progress:0}"), ariaValuetext($"Slide {slideNumber} of {Slides.Length}")],
                    [div([class_("progress-bar"), Abies.Html.Attributes.style($"width:{progress:0.##}%")], [])]),
                main([class_("deck")],
                [
                    nav([class_("agenda")],
                    [
                        h3([], [text("Agenda")]),
                        ul([], [..Slides.Select((entry, index) =>
                            li([attribute("data-key", $"{index + 1}")],
                                [
                                    a(
                                        [
                                            class_($"agenda-link{(index == model.SlideIndex ? " active" : "")}"),
                                            href($"#slide-{index + 1}"),
                                            ariaCurrent(index == model.SlideIndex ? "page" : "false")
                                        ],
                                        [
                                            span([class_("agenda-index")], [text($"{index + 1:00}")]),
                                            span([class_("agenda-title")], [text(entry.Title)])
                                        ],
                                        $"agenda-link-{index + 1}")
                                ])
                        )])
                    ]),
                    section([class_("content")],
                    [
                        div([class_("content-grid")],
                        [
                            div([class_("slide")], [
                                div([class_("kicker")], [text(slide.Kicker)]),
                                h1([], [text(slide.Title)]),
                                p([class_("subtitle")], [text(slide.Subtitle)]),
                                ul([class_("points")],
                                    [..slide.Points.Select((point, idx) =>
                                        li([attribute("data-key", $"point-{model.SlideIndex + 1}-{idx}")], [text(point)])
                                    )]),
                                slide.Code is null
                                    ? text("")
                                    : div([class_("code-block")],
                                    [
                                        div([class_("code-title")], [text("Snapshot")]),
                                        pre([], [code([], [text(slide.Code)])])
                                    ]),
                                slide.Callout is null
                                    ? text("")
                                    : div([class_("callout")], [text(slide.Callout)])
                            ], $"slide-{slideNumber}"),
                            RenderSidePanel(model, slide)
                        ])
                    ])
                ])
            ]));
    }

    private static Node RenderSidePanel(Model model, Slide slide)
        => slide.Kind == SlideKind.Demo
            ? RenderDemoPanel(model)
            : div([class_("panel")],
            [
                div([class_("panel-title")], [text("Key takeaway")]),
                div([class_("panel-body")],
                [
                    div([class_("takeaway")], [text(slide.Takeaway ?? "-")]),
                    div([class_("next-step-title")], [text("Next step")]),
                    div([class_("next-step")], [text(slide.NextStep ?? "-")])
                ])
            ]);

    private static Node RenderDemoPanel(Model model)
    {
        var logItems = RenderLogItems(model.Log);

        return div([class_("panel demo")],
        [
            div([class_("panel-title")], [text("Live MVU loop")]),
            div([class_("panel-body")],
            [
                div([class_("demo-metrics")],
                [
                    div([class_("demo-metric")],
                    [
                        span([class_("metric-label")], [text("Demo count")]),
                        span([class_("metric-value")], [text(model.DemoCount.ToString())])
                    ]),
                    div([class_("demo-metric")],
                    [
                        span([class_("metric-label")], [text("Ticks")]),
                        span([class_("metric-value")], [text(model.TickCount.ToString())])
                    ]),
                    div([class_("demo-metric")],
                    [
                        span([class_("metric-label")], [text("Mouse")]),
                        span([class_("metric-value")], [text($"{model.MouseX}, {model.MouseY}")])
                    ])
                ]),
                div([class_("demo-actions")],
                [
                    FluentButton([attribute("appearance", "primary"), onclick(new Message.IncrementDemo())], [text("Dispatch message")]),
                    FluentButton([attribute("appearance", "outline"), onclick(new Message.ResetDemo())], [text("Reset state")])
                ]),
                div([class_("toggle-row")],
                [
                    FluentButton([class_(model.DemoTimer ? "toggle active" : "toggle"), attribute("appearance", "outline"), ariaPressed(model.DemoTimer ? "true" : "false"), onclick(new Message.ToggleDemoTimer())],
                        [text(model.DemoTimer ? "Timer on" : "Timer off")]),
                    FluentButton([class_(model.TrackMouse ? "toggle active" : "toggle"), attribute("appearance", "outline"), ariaPressed(model.TrackMouse ? "true" : "false"), onclick(new Message.ToggleMouse())],
                        [text(model.TrackMouse ? "Mouse on" : "Mouse off")])
                ]),
                div([class_("log")],
                [
                    div([class_("log-header")],
                    [
                        span([], [text("Event log")]),
                        FluentButton([class_("ghost"), attribute("appearance", "subtle"), onclick(new Message.ClearLog())], [text("Clear")])
                    ]),
                    ul([ariaLive("polite")], logItems)
                ])
            ])
        ]);
    }

    internal static Element FluentButton(Abies.DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Abies.Html.Elements.element("fluent-button", attributes, children, id);

    internal static Element FluentBadge(Abies.DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => Abies.Html.Elements.element("fluent-badge", attributes, children, id);

    private static (Model model, Command command) HandleKeyPress(string key, bool repeat, Model model)
        => repeat
            ? (model, Commands.None)
            : key switch
            {
                "ArrowRight" or "PageDown" or " " or "Spacebar" or "Enter" or "l" or "j"
                    => SetSlide(model, ClampSlide(model.SlideIndex + 1)),
                "ArrowLeft" or "PageUp" or "Backspace" or "h" or "k"
                    => SetSlide(model, ClampSlide(model.SlideIndex - 1)),
                "Home" => SetSlide(model, 0),
                "End" => SetSlide(model, Slides.Length - 1),
                _ => (model, Commands.None)
            };

    private static int ClampSlide(int index)
        => Math.Clamp(index, 0, Slides.Length - 1);

    private static (Model model, Command command) SetSlide(Model model, int index)
        => index == model.SlideIndex
            ? (model, Commands.None)
            : (model with { SlideIndex = index }, new Navigation.Command.ReplaceState(Url.Create($"#slide-{index + 1}")));

    private static int? TryGetSlideIndex(Url url)
    {
        var fragment = url.Fragment.Value;
        if (string.IsNullOrWhiteSpace(fragment))
        {
            return null;
        }

        var value = fragment.TrimStart('#');
        if (!value.StartsWith("slide-", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var numberPart = value.Substring("slide-".Length);
        return int.TryParse(numberPart, out var index)
            ? index - 1
            : null;
    }

    private static IReadOnlyList<string> AddLog(IReadOnlyList<string> entries, string entry, DateTimeOffset? at = null)
    {
        var stamp = at?.ToString("HH:mm:ss");
        var formatted = stamp is null ? entry : $"{stamp} {entry}";
        var next = (string[])[formatted, ..entries];
        return next.Length > 8 ? next[..8] : next;
    }

    private static Node[] RenderLogItems(IReadOnlyList<string> entries)
        => entries.Count == 0
            ? [li([class_("log-empty")], [text("No events yet. Trigger a message to see updates.")])]
            : [..entries.Select((entry, idx) => li([attribute("data-key", $"log-{idx}-{entry.GetHashCode()}")], [text(entry)]))];

    private static Model UpdateMouse(Model model, Message.MouseMoved moved)
    {
        var next = model with
        {
            MouseX = (int)moved.Data.ClientX,
            MouseY = (int)moved.Data.ClientY
        };

        var last = model.LastMouseLogAt;
        if (last is not null && moved.At - last.Value < TimeSpan.FromMilliseconds(200))
        {
            return next;
        }

        return next with
        {
            Log = AddLog(next.Log, $"Mouse {next.MouseX}, {next.MouseY}", moved.At),
            LastMouseLogAt = moved.At
        };
    }

    private sealed record Slide(
        string Id,
        string Kicker,
        string Title,
        string Subtitle,
        string[] Points,
        string? Code,
        string? Callout,
        string? Takeaway,
        string? NextStep,
        SlideKind Kind);

    private enum SlideKind
    {
        Intro,
        Concept,
        Demo,
        Outro
    }
}
