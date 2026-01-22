# Subscriptions Demo

`Abies.SubscriptionsDemo` is a focused sample app that showcases Abies
subscriptions. It demonstrates timers, browser events, and WebSocket streams
feeding the MVU loop.

## Run it

```bash
dotnet run --project Abies.SubscriptionsDemo
```

## What it shows

- A timer tick counter (`SubscriptionModule.Every`)
- Visibility and resize events (`OnVisibilityChange`, `OnResize`)
- Mouse tracking (`OnMouseMove`)
- Optional WebSocket stream (`WebSocket`)
- A mock WebSocket feed for local, low-latency testing

## Notes

- WebSocket uses `wss://echo.websocket.events` by default.
- Toggle each subscription on and off to see the diffing behavior.
- The mock WebSocket emits a message every 2 seconds to simulate a live feed.
