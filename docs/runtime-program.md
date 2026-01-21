# Program and Runtime

This document explains the `Program<TModel, TArguments>` interface and how the
runtime executes it.

## Program interface

```csharp
public interface Program<TModel, in TArgument>
{
    public static abstract (TModel, Command) Initialize(Url url, TArgument argument);
    public static abstract (TModel model, Command command) Update(Message message, TModel model);
    public static abstract Document View(TModel model);
    public static abstract Message OnUrlChanged(Url url);
    public static abstract Message OnLinkClicked(UrlRequest urlRequest);
    public static abstract Subscription Subscriptions(TModel model);
    public static abstract Task HandleCommand(Command command, Func<Message, System.ValueTuple> dispatch);
}
```

## Runtime flow

`Runtime.Run<TProgram, TArguments, TModel>` performs the following:

1. Reads the current URL and calls `Initialize`.
2. Calls `View` and renders the initial DOM.
3. Executes the initial command via `HandleCommand`.
4. Listens for messages from DOM event handlers or URL changes.
5. Calls `Update`, then `View`, then diffs and patches the DOM.
6. Executes the returned command.

## URL handling

The runtime wires browser navigation to your program:

- On URL changes, it calls `OnUrlChanged`.
- On link clicks or form submits, it calls `OnLinkClicked`.
  - External links arrive as `UrlRequest.External`.
  - Same-origin links arrive as `UrlRequest.Internal`.

Your `Update` logic can return navigation commands (`Navigation.Command.*`) to
push or replace history state.

## Subscriptions

`Subscriptions` exists to support external event sources (timers, sockets,
custom JS), but `Subscription` is currently an empty record. Use commands for
most side effects today.
