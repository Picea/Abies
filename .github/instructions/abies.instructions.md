---
description: 'Guidelines for the Abies framework'
applyTo: '**/*'
---

# Abies Framework

The Abies framework is a Model-View-Update (MVU) architecture implementation for Blazor WebAssembly.

## Build System

### Important: abies.js Source of Truth

The canonical source for `abies.js` is located at:
```
Abies/wwwroot/abies.js
```

**This file is automatically copied to all consuming projects during build via the `SyncAbiesJs` MSBuild target.**

Consuming projects include:
- `Abies.Conduit/wwwroot/abies.js`
- `Abies.Counter/wwwroot/abies.js`
- `Abies.Presentation/wwwroot/abies.js`

⚠️ **NEVER edit the copied files directly** - always edit `Abies/wwwroot/abies.js` first. The copied files will be overwritten on build.

## Telemetry

### Browser OpenTelemetry

The `abies.js` file includes browser-side OpenTelemetry that:
- Loads OTel SDK from CDN (with fallback shim)
- Creates spans for DOM events (`dispatchEvent`)
- Propagates `traceparent` header on fetch requests
- Exports to `/otlp/v1/traces` proxy endpoint

### Verbosity Levels

Tracing supports three verbosity levels:

| Level | Traces | Use Case |
|-------|--------|----------|
| `off` | Nothing | Disable tracing entirely |
| `user` | UI Events + HTTP calls | **Default** - Production |
| `debug` | Everything (DOM, attrs) | Framework debugging |

Configure via:
```html
<meta name="otel-verbosity" content="user">
```
Or at runtime:
```javascript
window.__otel.setVerbosity('debug');
```

### .NET Runtime Tracing

The `Runtime.cs` file creates spans for:
- `Message: {Type}` - MVU message processing
- `Command: {Type}` - Command execution  
- `Update` - Model updates

## Conduit Application

The Conduit application is a real-world example of a web application built to showcase the abilities of the Abies framework. Abies.Conduit is that application in this solution.

### Specification

All specifications for the Conduit application can be found at the website: https://docs.realworld.show/ . The implementation of the showcase app MUST follow these specifications. 

### Testing

All user journeys MUST have an E2E test and integration tests. The user journeys are described in the specifications.