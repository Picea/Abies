# Tutorial 5: Forms

This tutorial teaches form handling, input binding, and validation in Abies.

**Prerequisites:** [Tutorial 4: Routing](./04-routing.md)

**Time:** 25 minutes

## What You'll Build

A registration form with:

- Text inputs for username, email, password
- Real-time validation
- Error display
- Form submission

## Form Handling in Abies

Forms in Abies follow the same MVU pattern:

1. Store form field values in the Model
2. Handle input events as Messages
3. Validate in Update or View
4. Submit via Commands

## Step 1: Define the Model

```csharp
public record FormField(string Value, string? Error);

public record Model(
    FormField Username,
    FormField Email,
    FormField Password,
    FormField ConfirmPassword,
    bool IsSubmitting,
    string? SubmitError,
    bool IsSuccess
);
```

Each field has a value and optional error message.

## Step 2: Define Messages

```csharp
// Field updates
public record UsernameChanged(string Value) : Message;
public record EmailChanged(string Value) : Message;
public record PasswordChanged(string Value) : Message;
public record ConfirmPasswordChanged(string Value) : Message;

// Form actions
public record Submit : Message;
public record SubmitSuccess : Message;
public record SubmitFailed(string Error) : Message;

// Commands
public record RegisterUser(string Username, string Email, string Password) : Command;
```

## Step 3: Initialize

```csharp
public static (Model, Command) Initialize(Url url, Arguments argument)
    => (new Model(
        Username: new FormField("", null),
        Email: new FormField("", null),
        Password: new FormField("", null),
        ConfirmPassword: new FormField("", null),
        IsSubmitting: false,
        SubmitError: null,
        IsSuccess: false
    ), Commands.None);
```

## Step 4: Validation Functions

Keep validation logic pure and reusable:

```csharp
static class Validation
{
    public static string? ValidateUsername(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Username is required";
        if (value.Length < 3)
            return "Username must be at least 3 characters";
        if (value.Length > 20)
            return "Username must be at most 20 characters";
        if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^[a-zA-Z0-9_]+$"))
            return "Username can only contain letters, numbers, and underscores";
        return null;
    }

    public static string? ValidateEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Email is required";
        if (!value.Contains('@') || !value.Contains('.'))
            return "Please enter a valid email address";
        return null;
    }

    public static string? ValidatePassword(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Password is required";
        if (value.Length < 8)
            return "Password must be at least 8 characters";
        return null;
    }

    public static string? ValidateConfirmPassword(string password, string confirm)
    {
        if (string.IsNullOrWhiteSpace(confirm))
            return "Please confirm your password";
        if (password != confirm)
            return "Passwords do not match";
        return null;
    }

    public static bool IsFormValid(Model model) =>
        model.Username.Error == null &&
        model.Email.Error == null &&
        model.Password.Error == null &&
        model.ConfirmPassword.Error == null &&
        !string.IsNullOrWhiteSpace(model.Username.Value) &&
        !string.IsNullOrWhiteSpace(model.Email.Value) &&
        !string.IsNullOrWhiteSpace(model.Password.Value) &&
        !string.IsNullOrWhiteSpace(model.ConfirmPassword.Value);
}
```

## Step 5: Implement Update

Validate on every change for immediate feedback:

```csharp
public static (Model model, Command command) Update(Message message, Model model)
    => message switch
    {
        UsernameChanged changed =>
            (model with 
            { 
                Username = new FormField(
                    changed.Value, 
                    Validation.ValidateUsername(changed.Value))
            }, Commands.None),
        
        EmailChanged changed =>
            (model with 
            { 
                Email = new FormField(
                    changed.Value, 
                    Validation.ValidateEmail(changed.Value))
            }, Commands.None),
        
        PasswordChanged changed =>
            (model with 
            { 
                Password = new FormField(
                    changed.Value, 
                    Validation.ValidatePassword(changed.Value)),
                // Re-validate confirm password when password changes
                ConfirmPassword = new FormField(
                    model.ConfirmPassword.Value,
                    Validation.ValidateConfirmPassword(changed.Value, model.ConfirmPassword.Value))
            }, Commands.None),
        
        ConfirmPasswordChanged changed =>
            (model with 
            { 
                ConfirmPassword = new FormField(
                    changed.Value, 
                    Validation.ValidateConfirmPassword(model.Password.Value, changed.Value))
            }, Commands.None),
        
        Submit when Validation.IsFormValid(model) =>
            (model with { IsSubmitting = true, SubmitError = null },
             new RegisterUser(model.Username.Value, model.Email.Value, model.Password.Value)),
        
        Submit => // Form invalid, don't submit
            (model, Commands.None),
        
        SubmitSuccess =>
            (model with { IsSubmitting = false, IsSuccess = true }, 
             Commands.None),
        
        SubmitFailed failed =>
            (model with { IsSubmitting = false, SubmitError = failed.Error }, 
             Commands.None),
        
        _ => (model, Commands.None)
    };
```

## Step 6: Build the View

Create reusable form field components:

```csharp
public static Document View(Model model)
{
    if (model.IsSuccess)
    {
        return new("Registration Complete",
            div([class_("success")], [
                h1([], [text("Welcome!")]),
                p([], [text("Your account has been created successfully.")]),
                a([href("/login")], [text("Go to Login")])
            ]));
    }
    
    return new("Register",
        div([class_("register-form")], [
            h1([], [text("Create Account")]),
            
            model.SubmitError is not null
                ? div([class_("error-banner")], [text(model.SubmitError)])
                : text(""),
            
            form([onsubmit(new Submit())], [
                FormField("Username", "text", model.Username.Value, model.Username.Error,
                    data => new UsernameChanged(data?.Value ?? "")),
                
                FormField("Email", "email", model.Email.Value, model.Email.Error,
                    data => new EmailChanged(data?.Value ?? "")),
                
                FormField("Password", "password", model.Password.Value, model.Password.Error,
                    data => new PasswordChanged(data?.Value ?? "")),
                
                FormField("Confirm Password", "password", model.ConfirmPassword.Value, 
                    model.ConfirmPassword.Error,
                    data => new ConfirmPasswordChanged(data?.Value ?? "")),
                
                button([
                    type("submit"),
                    class_(model.IsSubmitting ? "loading" : ""),
                    disabled(model.IsSubmitting ? "disabled" : null)
                ], [
                    text(model.IsSubmitting ? "Creating account..." : "Create Account")
                ])
            ])
        ]));
}

static Node FormField(
    string labelText, 
    string inputType, 
    string value, 
    string? error,
    Func<InputEventData?, Message> onChange)
{
    var hasError = error is not null;
    
    return div([class_("form-field")], [
        label([], [text(labelText)]),
        input([
            type(inputType),
            value(value),
            class_(hasError ? "error" : ""),
            oninput(onChange)
        ], []),
        hasError
            ? span([class_("error-message")], [text(error!)])
            : text("")
    ]);
}
```

## Step 7: Handle Command

```csharp
public static async Task HandleCommand(Command command, Func<Message, ValueTuple> dispatch)
{
    switch (command)
    {
        case RegisterUser register:
            try
            {
                // Simulate API call
                await Task.Delay(1000);
                
                // In real app: POST to /api/register
                // var response = await _httpClient.PostAsync(...);
                
                dispatch(new SubmitSuccess());
            }
            catch (Exception ex)
            {
                dispatch(new SubmitFailed(ex.Message));
            }
            break;
    }
}
```

## Complete Program

```csharp
using Abies;
using Abies.DOM;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

await Runtime.Run<RegisterApp, Arguments, Model>(new Arguments());

public record Arguments;

// Model
public record FormField(string Value, string? Error);

public record Model(
    FormField Username,
    FormField Email,
    FormField Password,
    FormField ConfirmPassword,
    bool IsSubmitting,
    string? SubmitError,
    bool IsSuccess
);

// Messages
public record UsernameChanged(string Value) : Message;
public record EmailChanged(string Value) : Message;
public record PasswordChanged(string Value) : Message;
public record ConfirmPasswordChanged(string Value) : Message;
public record Submit : Message;
public record SubmitSuccess : Message;
public record SubmitFailed(string Error) : Message;

// Commands
public record RegisterUser(string Username, string Email, string Password) : Command;

// Validation
static class Validation
{
    public static string? ValidateUsername(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Username is required";
        if (value.Length < 3) return "Username must be at least 3 characters";
        if (value.Length > 20) return "Username must be at most 20 characters";
        return null;
    }

    public static string? ValidateEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Email is required";
        if (!value.Contains('@')) return "Please enter a valid email";
        return null;
    }

    public static string? ValidatePassword(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Password is required";
        if (value.Length < 8) return "Password must be at least 8 characters";
        return null;
    }

    public static string? ValidateConfirmPassword(string password, string confirm)
    {
        if (string.IsNullOrWhiteSpace(confirm)) return "Please confirm your password";
        if (password != confirm) return "Passwords do not match";
        return null;
    }

    public static bool IsFormValid(Model model) =>
        model.Username.Error == null && model.Email.Error == null &&
        model.Password.Error == null && model.ConfirmPassword.Error == null &&
        !string.IsNullOrWhiteSpace(model.Username.Value) &&
        !string.IsNullOrWhiteSpace(model.Email.Value) &&
        !string.IsNullOrWhiteSpace(model.Password.Value);
}

public class RegisterApp : Program<Model, Arguments>
{
    public static (Model, Command) Initialize(Url url, Arguments argument)
        => (new Model(
            new FormField("", null), new FormField("", null),
            new FormField("", null), new FormField("", null),
            false, null, false
        ), Commands.None);

    public static (Model model, Command command) Update(Message message, Model model)
        => message switch
        {
            UsernameChanged c => (model with { Username = new(c.Value, Validation.ValidateUsername(c.Value)) }, Commands.None),
            EmailChanged c => (model with { Email = new(c.Value, Validation.ValidateEmail(c.Value)) }, Commands.None),
            PasswordChanged c => (model with { 
                Password = new(c.Value, Validation.ValidatePassword(c.Value)),
                ConfirmPassword = new(model.ConfirmPassword.Value, Validation.ValidateConfirmPassword(c.Value, model.ConfirmPassword.Value))
            }, Commands.None),
            ConfirmPasswordChanged c => (model with { ConfirmPassword = new(c.Value, Validation.ValidateConfirmPassword(model.Password.Value, c.Value)) }, Commands.None),
            Submit when Validation.IsFormValid(model) => (model with { IsSubmitting = true }, new RegisterUser(model.Username.Value, model.Email.Value, model.Password.Value)),
            Submit => (model, Commands.None),
            SubmitSuccess => (model with { IsSubmitting = false, IsSuccess = true }, Commands.None),
            SubmitFailed f => (model with { IsSubmitting = false, SubmitError = f.Error }, Commands.None),
            _ => (model, Commands.None)
        };

    public static Document View(Model model)
    {
        if (model.IsSuccess)
            return new("Success", div([], [h1([], [text("Account created!")])]));
        
        return new("Register", div([class_("form")], [
            h1([], [text("Register")]),
            model.SubmitError != null ? div([class_("error")], [text(model.SubmitError)]) : text(""),
            Field("Username", "text", model.Username, d => new UsernameChanged(d?.Value ?? "")),
            Field("Email", "email", model.Email, d => new EmailChanged(d?.Value ?? "")),
            Field("Password", "password", model.Password, d => new PasswordChanged(d?.Value ?? "")),
            Field("Confirm", "password", model.ConfirmPassword, d => new ConfirmPasswordChanged(d?.Value ?? "")),
            button([onclick(new Submit()), disabled(model.IsSubmitting ? "disabled" : null)], 
                [text(model.IsSubmitting ? "Submitting..." : "Register")])
        ]));
    }

    static Node Field(string lbl, string t, FormField f, Func<InputEventData?, Message> onChange) =>
        div([], [
            label([], [text(lbl)]),
            input([type(t), value(f.Value), oninput(onChange)], []),
            f.Error != null ? span([class_("error")], [text(f.Error)]) : text("")
        ]);

    public static Message OnUrlChanged(Url url) => new Submit();
    public static Message OnLinkClicked(UrlRequest r) => new Submit();
    public static Subscription Subscriptions(Model m) => SubscriptionModule.None;
    public static async Task HandleCommand(Command cmd, Func<Message, ValueTuple> dispatch)
    {
        if (cmd is RegisterUser)
        {
            await Task.Delay(1000);
            dispatch(new SubmitSuccess());
        }
    }
}
```

## What You Learned

| Concept | Application |
| ------- | ----------- |
| Form state | Track each field's value and error |
| Real-time validation | Validate on every input change |
| Conditional submission | Guard Submit with validation check |
| Reusable components | FormField helper function |
| Loading states | Disable button during submission |

## Form Patterns

### Debounced validation

```csharp
// Validate only after user stops typing
public record ValidateUsername : Command;

UsernameChanged changed =>
    (model with { Username = new(changed.Value, null) },
     new DelayedCommand(TimeSpan.FromMilliseconds(300), new ValidateUsername())),
```

### Server-side validation

```csharp
// Check if username is taken
case CheckUsername check:
    var available = await api.CheckUsername(check.Username);
    dispatch(available 
        ? new UsernameAvailable() 
        : new UsernameTaken());
    break;
```

### Multi-step forms

```csharp
public record FormStep { 
    public sealed record Personal : FormStep;
    public sealed record Account : FormStep;
    public sealed record Confirm : FormStep;
}

public record Model(FormStep CurrentStep, ...);
```

## Exercises

1. **Add more fields**: Phone number, date of birth
2. **Show/hide password**: Toggle password visibility
3. **Password strength**: Show strength indicator
4. **Remember me**: Add checkbox for persistent login

## Next Tutorial

→ [Tutorial 6: Subscriptions](./06-subscriptions.md) — Learn about external events
