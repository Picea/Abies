# Choosing a Render Mode

This guide helps you choose the right Abies render mode for your project. All four modes use the same `Program<TModel, TArgument>` interface — you can switch modes at any time without changing your MVU code.

## Quick Reference

| Mode | Best For | First Paint | Time to Interactive | Connection | JS Payload |
| --- | --- | --- | --- | --- | --- |
| **Static** | Content pages, SEO | ⚡ Fastest | N/A | None | 0 KB |
| **InteractiveServer** | Internal tools, data-heavy apps | ⚡ Fast | ⚡ Instant | WebSocket | ~15 KB |
| **InteractiveWasm** | PWAs, offline apps | ⚡ Fast | ⏳ After WASM loads | None | ~1.1 MB |
| **InteractiveAuto** | Public-facing apps | ⚡ Fast | ⚡ Instant | WebSocket → None | ~1.1 MB |

## Decision Matrix

### Start with these questions:

**1. Does the page need interactivity?**

If no → **Static**. This gives you the fastest first paint, zero JavaScript, and works with JavaScript disabled. Perfect for marketing pages, documentation, SEO-critical content.

**2. Does the app need to work offline?**

If yes → **InteractiveWasm**. The .NET runtime runs entirely in the browser. Combined with a service worker, the app works without any server connection.

**3. Is first-interaction latency critical?**

If yes → **InteractiveServer** or **InteractiveAuto**. Both provide instant interactivity because the server starts processing events immediately — no waiting for a WASM bundle to download.

**4. Can you maintain a persistent WebSocket connection?**

If yes → **InteractiveServer**. This is the simplest server-side mode. Each client gets a server-side Session that holds the MVU runtime.

If no → **InteractiveAuto**. The server handles initial interactions, then the WASM runtime takes over and the WebSocket is closed.

## Scenarios

### 🏢 Internal Business Application

**Recommended: InteractiveServer**

- Users are on a reliable network (corporate LAN/VPN)
- App accesses databases, APIs, secrets directly on the server
- Persistent WebSocket connection is acceptable
- No need for offline support
- Instant interactivity without WASM download

### 🌐 Public-Facing SaaS Application

**Recommended: InteractiveAuto**

- First impression matters — instant interactivity on first visit
- Users may have slow connections (WASM download takes time)
- Long sessions should eventually be connectionless (scalability)
- Server resources are shared across many users

### 📱 Progressive Web App (PWA)

**Recommended: InteractiveWasm**

- Must work offline
- Installed on device, runs like a native app
- No persistent server connection needed
- Latency-sensitive interactions (everything is client-side)

### 📝 Content Website / Blog

**Recommended: Static**

- SEO is the priority
- Content doesn't need interactivity
- Fastest possible first paint
- Works with JavaScript disabled
- Can be served from a CDN

### 📋 Mixed Application

**Recommended: Mix modes per page**

Abies render modes are per-page, not per-application. A single server can serve:
- Static HTML for the landing page
- InteractiveServer for the admin dashboard
- InteractiveWasm for the public-facing editor

```csharp
app.MapGet("/", () => Page.Render<Landing, LandingModel, Unit>(new RenderMode.Static()));
app.MapAbies<Dashboard, DashboardModel, Unit>("/admin", new RenderMode.InteractiveServer());
app.MapAbies<Editor, EditorModel, Unit>("/editor", new RenderMode.InteractiveWasm());
```

## Performance Implications

| Metric | Static | InteractiveServer | InteractiveWasm | InteractiveAuto |
| --- | --- | --- | --- | --- |
| First Paint | ~20 ms | ~60 ms | ~60 ms | ~60 ms |
| Time to Interactive | N/A | ~60 ms | ~4–5 s (WASM load) | ~60 ms |
| Interaction Latency | N/A | Network RTT | ~0 ms | Network RTT → ~0 ms |
| Server Memory | 0 | ~2 MB/session | 0 | ~2 MB (temporary) |
| Bandwidth | HTML only | Small binary patches | WASM bundle + patches | Both (transition) |

## Migration Between Modes

Because all modes share the same `Program<TModel, TArgument>` interface, switching is a configuration change:

```csharp
// Change one line to switch from WASM to Server:
// Before:
var html = Page.Render<MyApp, MyModel, Unit>(new RenderMode.InteractiveWasm());
// After:
var html = Page.Render<MyApp, MyModel, Unit>(new RenderMode.InteractiveServer());
```

Your `Initialize`, `Transition`, `View`, and `Subscriptions` functions remain identical.

> **Note:** The command interpreter may need different implementations for browser vs. server. Browser-side interpreters use browser APIs (fetch, localStorage); server-side interpreters use server APIs (HttpClient, database). The MVU logic itself is unchanged.

## Next

- [**Render Modes Concept**](../concepts/render-modes.md) — Deep dive into how each mode works
- [**Deployment Guide**](deployment.md) — Deploying each mode to production
- [**Performance Guide**](performance.md) — Optimization strategies per mode
