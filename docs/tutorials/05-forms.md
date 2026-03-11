# Tutorial 5: Forms

Learn how to handle form inputs, validate user data, and submit forms using the command/interpreter pattern.

**Prerequisites:** [Tutorial 4: Routing](04-routing.md)

**Time:** 25 minutes

**What you'll learn:**

- Handling text inputs, checkboxes, selects, and textareas
- The two `oninput` overloads and when to use each
- Client-side validation patterns
- Form submission as a command
- Displaying server-side validation errors

## Input Handling Recap

In Abies, every form input change produces a message. There are two event handler overloads:

| Overload | Use Case |
| --- | --- |
| `oninput(new MyMessage())` | You don't need the input value (rare for form inputs) |
| `oninput(data => new MyMessage(data?.Value ?? ""))` | You need the current input value |

For form fields, you'll almost always use the factory overload to capture the value.

## Building a Registration Form

Let's build a user registration form with validation.

### Model

```csharp
using Abies.DOM;
using Abies.Subscriptions;
using Automaton;
using static Abies.Html.Attributes;
using static Abies.Html.Elements;
using static Abies.Html.Events;

namespace RegistrationForm;

public record Model(
    string Username,
    string Email,
    string Password,
    string PasswordConfirm,
    bool AgreeToTerms,
    IReadOnlyList<string> ValidationErrors,
    IReadOnlyList<string> ServerErrors,
    bool IsSubmitting,
    bool IsSuccess);
```

The model holds:

- **Field values** — Each input field's current text
- **ValidationErrors** — Client-side validation messages (computed before submit)
- **ServerErrors** — Errors returned by the API after submission
- **IsSubmitting** — Loading state during API call
- **IsSuccess** — Whether registration succeeded

### Messages

```csharp
public interface FormMessage : Message;

// Field changes
public record UsernameChanged(string Value) : FormMessage;
public record EmailChanged(string Value) : FormMessage;
public record PasswordChanged(string Value) : FormMessage;
public record PasswordConfirmChanged(string Value) : FormMessage;
public record AgreeChanged(bool Value) : FormMessage;

// Form submission
public record SubmitForm : FormMessage;

// API responses
public record RegistrationSucceeded : FormMessage;
public record RegistrationFailed(IReadOnlyList<string> Errors) : FormMessage;
```

### Commands

```csharp
/// <summary>Submit the registration to the API.</summary>
public record RegisterUser(
    string Username,
    string Email,
    string Password) : Command;
```

### Validation

Client-side validation is a **pure function** — it takes the model and returns a list of errors:

```csharp
public static class Validation
{
    public static IReadOnlyList<string> Validate(Model model)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(model.Username))
            errors.Add("Username is required.");
        else if (model.Username.Length < 3)
            errors.Add("Username must be at least 3 characters.");

        if (string.IsNullOrWhiteSpace(model.Email))
            errors.Add("Email is required.");
        else if (!model.Email.Contains('@'))
            errors.Add("Email must be a valid email address.");

        if (string.IsNullOrWhiteSpace(model.Password))
            errors.Add("Password is required.");
        else if (model.Password.Length < 8)
            errors.Add("Password must be at least 8 characters.");

        if (model.Password != model.PasswordConfirm)
            errors.Add("Passwords do not match.");

        if (!model.AgreeToTerms)
            errors.Add("You must agree to the terms.");

        return errors;
    }
}
```

Because validation is pure, it's trivially testable — no mocks, no setup.

### Transition

```csharp
public sealed class Registration : Program<Model, Unit>
{
    public static (Model, Command) Initialize(Unit _) =>
        (new Model(
            Username: "",
            Email: "",
            Password: "",
            PasswordConfirm: "",
            AgreeToTerms: false,
            ValidationErrors: [],
            ServerErrors: [],
            IsSubmitting: false,
            IsSuccess: false
        ), Commands.None);

    public static (Model, Command) Transition(Model model, Message message) =>
        message switch
        {
            // Field updates: clear validation errors on change
            UsernameChanged msg =>
                (model with { Username = msg.Value, ValidationErrors = [] },
                 Commands.None),

            EmailChanged msg =>
                (model with { Email = msg.Value, ValidationErrors = [] },
                 Commands.None),

            PasswordChanged msg =>
                (model with { Password = msg.Value, ValidationErrors = [] },
                 Commands.None),

            PasswordConfirmChanged msg =>
                (model with { PasswordConfirm = msg.Value, ValidationErrors = [] },
                 Commands.None),

            AgreeChanged msg =>
                (model with { AgreeToTerms = msg.Value, ValidationErrors = [] },
                 Commands.None),

            // Submit: validate first, then send command if valid
            SubmitForm => HandleSubmit(model),

            // API responses
            RegistrationSucceeded =>
                (model with { IsSubmitting = false, IsSuccess = true },
                 Commands.None),

            RegistrationFailed msg =>
                (model with { IsSubmitting = false, ServerErrors = msg.Errors },
                 Commands.None),

            _ => (model, Commands.None)
        };

    private static (Model, Command) HandleSubmit(Model model)
    {
        var errors = Validation.Validate(model);

        if (errors.Count > 0)
            return (model with { ValidationErrors = errors }, Commands.None);

        return (
            model with { IsSubmitting = true, ServerErrors = [] },
            new RegisterUser(model.Username, model.Email, model.Password)
        );
    }
```

**Key pattern:** Validation runs in `Transition` (pure). If it fails, no command is emitted — errors are stored in the model and displayed. If it passes, a `RegisterUser` command is returned for the interpreter to execute.

### View

```csharp
    public static Document View(Model model) =>
        new("Register",
            div([class_("form-page")],
            [
                h1([], [text("Create Account")]),
                model.IsSuccess
                    ? SuccessView()
                    : FormView(model)
            ]));

    static Node SuccessView() =>
        div([class_("success")],
        [
            h2([], [text("✓ Registration successful!")]),
            p([], [text("You can now sign in with your credentials.")]),
            a([href("/login")], [text("Go to login")])
        ]);

    static Node FormView(Model model) =>
        div([class_("form-container")],
        [
            ErrorList(model.ValidationErrors),
            ErrorList(model.ServerErrors),

            FormField("Username", "text", model.Username,
                data => new UsernameChanged(data?.Value ?? "")),

            FormField("Email", "email", model.Email,
                data => new EmailChanged(data?.Value ?? "")),

            FormField("Password", "password", model.Password,
                data => new PasswordChanged(data?.Value ?? "")),

            FormField("Confirm Password", "password", model.PasswordConfirm,
                data => new PasswordConfirmChanged(data?.Value ?? "")),

            div([class_("field")],
            [
                label([],
                [
                    input_([
                        type_("checkbox"),
                        checked_(model.AgreeToTerms),
                        onclick(new AgreeChanged(!model.AgreeToTerms))
                    ]),
                    text(" I agree to the terms and conditions")
                ])
            ]),

            button([
                class_("submit-btn"),
                onclick(new SubmitForm()),
                disabled(model.IsSubmitting)
            ], [text(model.IsSubmitting ? "Submitting..." : "Register")])
        ]);

    static Node FormField(
        string label,
        string inputType,
        string currentValue,
        Func<InputEventData?, Message> onChange) =>
        div([class_("field")],
        [
            Elements.label([], [text(label)]),
            input_([
                type_(inputType),
                value(currentValue),
                oninput(onChange)
            ])
        ]);

    static Node ErrorList(IReadOnlyList<string> errors) =>
        errors.Count > 0
            ? ul([class_("error-list")],
                errors.Select(e =>
                    li([], [text(e)])
                ).ToArray())
            : text("");

    public static Subscription Subscriptions(Model model) =>
        SubscriptionModule.None;
}
```

**View patterns:**

- **Reusable `FormField` helper** — Extracts the repeated label + input pattern
- **`oninput(onChange)`** — Each field gets a factory function that creates the appropriate message
- **Checkbox handling** — Uses `onclick` with a toggled value (since checkboxes don't need `oninput`)
- **Conditional rendering** — `errors.Count > 0 ? errorList : text("")` shows errors only when present
- **Disabled state** — `disabled(model.IsSubmitting)` prevents double submission

### Interpreter

```csharp
public static class FormInterpreter
{
    private static readonly HttpClient _http = new();

    public static async ValueTask<Result<Message[], PipelineError>> Interpret(
        Command command)
    {
        try
        {
            Message[] messages = command switch
            {
                RegisterUser cmd => await HandleRegister(cmd),
                _ => []
            };

            return Result<Message[], PipelineError>.Ok(messages);
        }
        catch (Exception ex)
        {
            return Result<Message[], PipelineError>.Ok(
                [new RegistrationFailed([ex.Message])]);
        }
    }

    private static async Task<Message[]> HandleRegister(RegisterUser cmd)
    {
        var payload = new { user = new { cmd.Username, cmd.Email, cmd.Password } };
        var response = await _http.PostAsJsonAsync("/api/users", payload);

        if (response.IsSuccessStatusCode)
            return [new RegistrationSucceeded()];

        // Parse validation errors from the API response
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        return [new RegistrationFailed(
            body?.Errors ?? [$"HTTP {(int)response.StatusCode}"])];
    }

    private record ErrorResponse(IReadOnlyList<string> Errors);
}
```

## Handling Different Input Types

### Text Input

```csharp
input_([
    type_("text"),
    value(model.Name),
    placeholder("Enter your name"),
    oninput(data => new NameChanged(data?.Value ?? ""))
])
```

### Textarea

```csharp
textarea([
    value(model.Bio),
    oninput(data => new BioChanged(data?.Value ?? ""))
], [])
```

### Select / Dropdown

```csharp
select([
    oninput(data => new CountryChanged(data?.Value ?? ""))
],
[
    option([value("")], [text("Select a country...")]),
    option([value("us"), selected(model.Country == "us")], [text("United States")]),
    option([value("uk"), selected(model.Country == "uk")], [text("United Kingdom")]),
    option([value("de"), selected(model.Country == "de")], [text("Germany")])
])
```

### Checkbox

```csharp
input_([
    type_("checkbox"),
    checked_(model.Subscribe),
    onclick(new SubscribeChanged(!model.Subscribe))
])
```

Checkboxes use `onclick` with the toggled value rather than `oninput`, since you want the boolean toggle, not a text value.

### Radio Buttons

```csharp
static Node RadioGroup(string name, string current, (string Value, string Label)[] options) =>
    div([],
        options.Select(opt =>
            label([],
            [
                input_([
                    type_("radio"),
                    Attributes.name(name),
                    value(opt.Value),
                    checked_(current == opt.Value),
                    onclick(new PlanChanged(opt.Value))
                ]),
                text($" {opt.Label}")
            ])
        ).ToArray());
```

## Validation Patterns

### Real-Time Validation (On Each Keystroke)

```csharp
public static (Model, Command) Transition(Model model, Message message) =>
    message switch
    {
        EmailChanged msg =>
            (model with
            {
                Email = msg.Value,
                EmailError = ValidateEmail(msg.Value)
            }, Commands.None),
        // ...
    };

static string? ValidateEmail(string email) =>
    string.IsNullOrWhiteSpace(email) ? "Email is required."
    : !email.Contains('@') ? "Invalid email address."
    : null;
```

### Validate On Submit Only

```csharp
SubmitForm =>
    Validation.Validate(model) is { Count: > 0 } errors
        ? (model with { ValidationErrors = errors }, Commands.None)
        : (model with { IsSubmitting = true }, new RegisterUser(...))
```

### Validate On Blur (Focus Lost)

Use `onblur` to validate when the user moves away from a field:

```csharp
input_([
    type_("email"),
    value(model.Email),
    oninput(data => new EmailChanged(data?.Value ?? "")),
    onblur(new ValidateEmail())
])
```

## Testing

### Validation Tests

```csharp
[Fact]
public void Validate_EmptyUsername_ReturnsError()
{
    var model = CreateModel(username: "");

    var errors = Validation.Validate(model);

    Assert.Contains(errors, e => e.Contains("Username is required"));
}

[Fact]
public void Validate_ShortPassword_ReturnsError()
{
    var model = CreateModel(password: "abc");

    var errors = Validation.Validate(model);

    Assert.Contains(errors, e => e.Contains("at least 8 characters"));
}

[Fact]
public void Validate_MismatchedPasswords_ReturnsError()
{
    var model = CreateModel(password: "password1", passwordConfirm: "password2");

    var errors = Validation.Validate(model);

    Assert.Contains(errors, e => e.Contains("do not match"));
}

[Fact]
public void Validate_ValidModel_ReturnsNoErrors()
{
    var model = CreateModel();

    var errors = Validation.Validate(model);

    Assert.Empty(errors);
}
```

### Transition Tests

```csharp
[Fact]
public void SubmitForm_WithInvalidData_SetsValidationErrors()
{
    var model = CreateModel(username: "");

    var (newModel, command) = Registration.Transition(model, new SubmitForm());

    Assert.NotEmpty(newModel.ValidationErrors);
    Assert.Equal(Commands.None, command);  // no API call
}

[Fact]
public void SubmitForm_WithValidData_ReturnsRegisterCommand()
{
    var model = CreateModel();

    var (newModel, command) = Registration.Transition(model, new SubmitForm());

    Assert.True(newModel.IsSubmitting);
    Assert.IsType<RegisterUser>(command);
}
```

## Exercises

1. **Multi-step form** — Split the registration into steps (account info → profile → confirmation). Add a `Step` field to the model and navigate between steps.

2. **Debounced validation** — Use `SubscriptionModule.Every` to debounce username availability checks (validate only after 500ms of no typing).

3. **File upload** — Add a profile image upload field. The interpreter should handle the multipart form data POST.

4. **Form reset** — Add a "Clear" button that resets all fields to their initial values.

## Key Concepts

| Concept | In This Tutorial |
| --- | --- |
| `oninput(data => msg)` | Capture input values from form fields |
| `onclick(msg)` | Handle button clicks and checkbox toggles |
| Pure validation | `Validate(Model) → IReadOnlyList<string>` |
| Submit guard | Validate before emitting the command |
| Server errors | Stored separately from client validation errors |
| Disabled state | `disabled(model.IsSubmitting)` prevents double submit |

## Next Steps

→ [Tutorial 6: Subscriptions](06-subscriptions.md) — Learn about timers, WebSockets, and external event sources