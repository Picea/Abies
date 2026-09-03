---
name: vanilla-js-playbook
description: Vanilla JavaScript reference for ES2024+ — Web Components and Custom Elements, framework-replacement patterns (Components → WC, Reactivity → MutationObserver/Proxy, Routing → Navigation API, State → Map/BroadcastChannel, Data → fetch/AbortController/Streams/SSE, Templating → tagged literals, Animations → WAAPI), V8 performance notes, and browser-side OTEL setup. Use when building or reviewing browser-side JavaScript, deciding whether a framework is justified, or wiring browser telemetry to the Aspire dashboard.
---

# Vanilla JavaScript Playbook

The JS Dev's deep reference. Role and protocol live in `.claude/agents/js-dev.md`. The squad's principle that **the platform is the framework** lives in `.claude/docs/decisions.md` — vanilla first, frameworks only with explicit Architect approval after a full design cycle.

This skill catalogs **how to build what frameworks abstract away** using native browser APIs available in any evergreen browser as of 2025/2026.

---

## ES2024+ Landed Features You Should Be Using

These are stable, broadly supported features of modern JavaScript — there's no excuse for not knowing them.

| Feature | Use case | Example |
|---|---|---|
| `?.` and `??` | Optional chaining and nullish coalescing | `user?.profile?.email ?? 'no email'` |
| Top-level await | Modules can `await` at the top level | `const config = await fetch('/config').then(r => r.json())` |
| Private class fields | Truly private state, no underscore convention | `class Counter { #count = 0; }` |
| `Array.prototype.at` | Negative indexing | `arr.at(-1)` for last element |
| `Object.hasOwn` | Replacement for `hasOwnProperty` | `Object.hasOwn(obj, key)` |
| `structuredClone` | Deep clone built into the platform | `const copy = structuredClone(original)` |
| `Array.prototype.findLast` / `findLastIndex` | Reverse search | `users.findLast(u => u.active)` |
| `Array.prototype.toSorted` / `toReversed` / `toSpliced` | Non-mutating array methods | `const sorted = arr.toSorted()` |
| `Object.groupBy` / `Map.groupBy` | Group array by key | `Object.groupBy(items, x => x.category)` |
| `String.prototype.replaceAll` | Replace all occurrences | `s.replaceAll('foo', 'bar')` |
| `Promise.withResolvers` | External resolve/reject control | `const { promise, resolve, reject } = Promise.withResolvers()` |
| `globalThis` | Universal global object | Works in browsers, workers, Node |
| `URL.canParse` | Validate URL without try/catch | `if (URL.canParse(input)) { ... }` |

---

## Web Platform APIs You Should Reach For

| Need | Native API | Don't reach for |
|---|---|---|
| HTTP requests | `fetch` with `AbortController` | `axios`, `superagent` |
| UUIDs | `crypto.randomUUID()` | `uuid` package |
| Cryptography | `SubtleCrypto` (`crypto.subtle`) | hand-rolled or random npm crypto packages |
| Date arithmetic | `Temporal` (when available) or careful `Date` use | Moment.js, date-fns (sometimes justified) |
| Templating | Tagged template literals | Handlebars, EJS for simple cases |
| Reactivity | `Proxy`, `MutationObserver`, `EventTarget` | full reactive frameworks for non-app surfaces |
| Components | Custom Elements + Shadow DOM | React/Vue/Angular for non-app surfaces |
| Routing | Navigation API + `URLPattern` | React Router, Vue Router |
| State sharing across tabs | `BroadcastChannel` | redux-multi-tab and similar |
| Background work | `Web Workers`, `Service Workers` | "main thread only" workarounds |
| Streaming data | `fetch` + `ReadableStream`, Server-Sent Events | polling, full WebSocket libraries when SSE suffices |
| Animations | Web Animations API | `gsap` for simple cases, CSS for triggered transitions |
| Form validation | Constraint Validation API + `:invalid` | reinventing it |
| Deep clones | `structuredClone` | `lodash.cloneDeep`, `JSON.parse(JSON.stringify(...))` |

---

## Framework-Replacement Patterns

### Components — Custom Elements + Shadow DOM

```javascript
// counter-element.js
class CounterElement extends HTMLElement {
  static observedAttributes = ['initial-value', 'step'];

  #count = 0;
  #step = 1;
  #shadow;

  constructor() {
    super();
    this.#shadow = this.attachShadow({ mode: 'open' });
  }

  connectedCallback() {
    this.#count = Number(this.getAttribute('initial-value') ?? 0);
    this.#step = Number(this.getAttribute('step') ?? 1);
    this.#render();
    this.#shadow.querySelector('.inc').addEventListener('click', () => this.#increment());
    this.#shadow.querySelector('.dec').addEventListener('click', () => this.#decrement());
  }

  attributeChangedCallback(name, oldValue, newValue) {
    if (oldValue === newValue) return;
    if (name === 'initial-value') this.#count = Number(newValue);
    if (name === 'step') this.#step = Number(newValue);
    this.#render();
  }

  #increment() {
    this.#count += this.#step;
    this.#render();
    this.dispatchEvent(new CustomEvent('counter-changed', { detail: { value: this.#count }, bubbles: true }));
  }

  #decrement() {
    this.#count -= this.#step;
    this.#render();
    this.dispatchEvent(new CustomEvent('counter-changed', { detail: { value: this.#count }, bubbles: true }));
  }

  #render() {
    this.#shadow.innerHTML = `
      <style>
        :host { display: inline-flex; gap: 0.5rem; align-items: center; }
        button { padding: 0.25rem 0.75rem; }
      </style>
      <button class="dec" type="button" aria-label="Decrement">−</button>
      <span class="value">${this.#count}</span>
      <button class="inc" type="button" aria-label="Increment">+</button>
    `;
  }
}

customElements.define('app-counter', CounterElement);
```

Used in HTML: `<app-counter initial-value="10" step="5"></app-counter>`. No build step. Self-contained styles. Standard event model. Works in any framework or none.

### Reactivity — `Proxy`, `MutationObserver`

```javascript
function createObservable(target) {
  const subscribers = new Set();
  return {
    state: new Proxy(target, {
      set(obj, key, value) {
        const oldValue = obj[key];
        obj[key] = value;
        if (oldValue !== value) subscribers.forEach(fn => fn(key, value, oldValue));
        return true;
      }
    }),
    subscribe(fn) { subscribers.add(fn); return () => subscribers.delete(fn); }
  };
}

// usage
const { state, subscribe } = createObservable({ count: 0 });
subscribe((key, newValue) => console.log(`${key} = ${newValue}`));
state.count = 42;  // logs "count = 42"
```

`MutationObserver` covers the inverse case: react to DOM changes that originated from somewhere else. Useful for integrating with code you don't control.

### Routing — Navigation API + `URLPattern`

```javascript
const routes = [
  { pattern: new URLPattern({ pathname: '/' }), handler: () => renderHome() },
  { pattern: new URLPattern({ pathname: '/articles/:id' }), handler: ({ id }) => renderArticle(id) },
  { pattern: new URLPattern({ pathname: '/articles/:id/edit' }), handler: ({ id }) => renderEdit(id) },
];

navigation.addEventListener('navigate', (event) => {
  if (!event.canIntercept || event.hashChange) return;

  const url = new URL(event.destination.url);
  const match = routes.find(r => r.pattern.test(url));
  if (!match) return;

  event.intercept({
    handler: async () => {
      const params = match.pattern.exec(url).pathname.groups;
      await match.handler(params);
    }
  });
});
```

### State Sharing — `BroadcastChannel`

```javascript
const channel = new BroadcastChannel('app-state');
channel.postMessage({ type: 'user-logged-out' });

channel.addEventListener('message', (event) => {
  if (event.data.type === 'user-logged-out') {
    redirectToLogin();
  }
});
```

Sync state across tabs of the same origin without a server round-trip.

### Data — `fetch` + `AbortController` + Streams

```javascript
async function fetchArticles({ signal } = {}) {
  const response = await fetch('/api/articles', { signal });
  if (!response.ok) {
    return { ok: false, error: { code: 'http-error', status: response.status } };
  }
  return { ok: true, data: await response.json() };
}

// Cancellation
const controller = new AbortController();
const result = fetchArticles({ signal: controller.signal });
// Later, if the user navigates away:
controller.abort();
```

Streaming with `ReadableStream`:

```javascript
const response = await fetch('/api/articles/stream');
const reader = response.body.pipeThrough(new TextDecoderStream()).getReader();
while (true) {
  const { done, value } = await reader.read();
  if (done) break;
  processChunk(value);
}
```

Server-Sent Events for one-way push:

```javascript
const events = new EventSource('/api/events');
events.addEventListener('article-published', (event) => {
  const article = JSON.parse(event.data);
  renderNewArticle(article);
});
```

### Templating — Tagged Template Literals

```javascript
function html(strings, ...values) {
  // sanitize values, return DocumentFragment or HTMLString
  // ... implementation depends on whether you trust the values
}

const template = html`
  <article>
    <h1>${escapeHtml(title)}</h1>
    <p>${escapeHtml(body)}</p>
  </article>
`;
```

For trusted content, `<template>` element + `cloneNode(true)` is the fastest, simplest approach. For untrusted content, always escape — never `innerHTML` with raw user input.

### Animations — Web Animations API

```javascript
element.animate(
  [
    { transform: 'translateY(0)', opacity: 1 },
    { transform: 'translateY(-20px)', opacity: 0 }
  ],
  { duration: 300, easing: 'ease-in', fill: 'forwards' }
);
```

Cleaner than CSS transitions for one-off animations, scriptable, supports cancel/pause/reverse.

---

## V8 Performance Notes

These are practical performance principles for browser JS:

- **Hidden classes / inline caches.** Add properties to objects in the same order. Don't add new properties after construction. Helps V8 inline-cache property lookups.
- **Avoid megamorphic call sites.** A function called with many different shapes of object becomes hard to optimize. Specialized helpers (one per shape) outperform a generic helper that branches on shape.
- **Don't use `arguments`.** Use rest parameters (`...args`). `arguments` defeats some optimizations.
- **Avoid `delete` on objects.** It transitions the object to dictionary mode. Set to `undefined` or use a `Map` if you need to remove keys.
- **Prefer `Map` for dynamic keyed lookup.** `{}` becomes slow when used as a hash; `Map` is designed for it.
- **Use typed arrays for numeric data.** `Float64Array`, `Int32Array` for any large numeric buffer.
- **`structuredClone` over JSON round-trip** for deep copy.

Don't over-optimize. Measure first with the browser's Performance panel.

---

## Browser-Side OpenTelemetry

Browser-side traces should connect to the same Aspire dashboard the backend uses, completing the end-to-end picture from user click through every backend hop.

### Setup

```javascript
// otel-bootstrap.js
import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-proto';
import { Resource } from '@opentelemetry/resources';
import { SemanticResourceAttributes } from '@opentelemetry/semantic-conventions';
import { BatchSpanProcessor } from '@opentelemetry/sdk-trace-base';
import { ZoneContextManager } from '@opentelemetry/context-zone';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch';
import { DocumentLoadInstrumentation } from '@opentelemetry/instrumentation-document-load';
import { UserInteractionInstrumentation } from '@opentelemetry/instrumentation-user-interaction';
import { W3CTraceContextPropagator } from '@opentelemetry/core';

const provider = new WebTracerProvider({
  resource: new Resource({
    [SemanticResourceAttributes.SERVICE_NAME]: 'web-frontend',
  }),
});

provider.addSpanProcessor(new BatchSpanProcessor(new OTLPTraceExporter({
  url: '/otel/v1/traces',  // proxied through your service to the OTEL collector
})));

provider.register({
  contextManager: new ZoneContextManager(),
  propagator: new W3CTraceContextPropagator(),
});

registerInstrumentations({
  instrumentations: [
    new DocumentLoadInstrumentation(),
    new UserInteractionInstrumentation({ eventNames: ['click', 'submit'] }),
    new FetchInstrumentation({
      propagateTraceHeaderCorsUrls: [/.+/],  // attach traceparent to all fetches
    }),
  ],
});
```

The fetch instrumentation propagates `traceparent` headers, so a button click in the browser becomes a span that links to the backend's request span in the same trace, visible in Aspire dashboard.

### Custom spans for business actions

```javascript
import { trace } from '@opentelemetry/api';
const tracer = trace.getTracer('web-frontend');

async function publishArticle(articleId) {
  return tracer.startActiveSpan('publish_article', async (span) => {
    span.setAttribute('article.id', articleId);
    try {
      const result = await api.publishArticle(articleId);
      span.setStatus({ code: 1 });  // OK
      return result;
    } catch (err) {
      span.recordException(err);
      span.setStatus({ code: 2, message: err.message });  // Error
      throw err;
    } finally {
      span.end();
    }
  });
}
```

---

## Anti-Patterns the Reviewer Flags

- ❌ `var` anywhere.
- ❌ `==` instead of `===`.
- ❌ `innerHTML` with user-provided content (XSS vector).
- ❌ `eval()`, `new Function(...)` with user input.
- ❌ Math.random() for IDs, tokens, or any non-decorative randomness. Use `crypto.randomUUID()` or `crypto.getRandomValues`.
- ❌ Adding a framework to handle a single component.
- ❌ Adding a build step for a project that runs natively in modern browsers.
- ❌ `axios` when `fetch` would do.
- ❌ `lodash` for things native arrays handle.
- ❌ `uuid` package when `crypto.randomUUID()` exists.
- ❌ `<div onclick="...">` instead of `<button>`.
- ❌ `setInterval` for animations (use `requestAnimationFrame`).
- ❌ Promises without `.catch()` or `try/await/catch`.
- ❌ Ignoring `prefers-reduced-motion`.

---

## What This Skill Does NOT Cover

- **Accessibility patterns** for browser code — `ux-review` skill (the JS Dev implements; the UX Expert specifies).
- **Browser-side security headers and CSP** — `security-toolchain` skill (Security Expert specifies).
- **Browser-side load testing** — `performance-engineering` skill.
- **The decision of when to add a build step or a framework** — Architect call. This skill describes how to live without them.
