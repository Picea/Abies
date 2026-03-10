# Deployment Guide

How to build and deploy Abies applications to production.

## Overview

Abies supports two deployment models:

| Model | Hosting | Best for |
| ----- | ------- | -------- |
| **Browser (WASM)** | Any static hosting | SPAs, offline-first, CDN distribution |
| **Server (Kestrel)** | .NET hosting / Docker | SEO, real-time, thin clients |

## Browser (WASM) Deployment

### Production Build

```bash
dotnet publish -c Release
```

Output: `bin/Release/net10.0/publish/wwwroot/`

### Build Options

```xml
<PropertyGroup>
    <RunAOTCompilation>true</RunAOTCompilation>
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>link</TrimMode>
    <BlazorEnableCompression>true</BlazorEnableCompression>
</PropertyGroup>
```

### Build Output

```text
bin/Release/net10.0/publish/wwwroot/
├── _framework/
│   ├── dotnet.js
│   ├── dotnet.wasm
│   └── ...
├── index.html
└── css/
```

### Static Hosting Options

WASM apps are static files — deploy to any static host.

#### Azure Static Web Apps

```json
// staticwebapp.config.json
{
    "navigationFallback": {
        "rewrite": "/index.html"
    }
}
```

#### GitHub Pages

```yaml
# .github/workflows/deploy.yml
name: Deploy to GitHub Pages
on:
  push:
    branches: [main]
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet publish -c Release
      - uses: peaceiris/actions-gh-pages@v3
        with:
          github_token: ${{ secrets.GITHUB_TOKEN }}
          publish_dir: ./bin/Release/net10.0/publish/wwwroot
```

#### Netlify

```toml
# netlify.toml
[build]
  command = "dotnet publish -c Release"
  publish = "bin/Release/net10.0/publish/wwwroot"

[[redirects]]
  from = "/*"
  to = "/index.html"
  status = 200
```

### SPA Routing

All static hosts need to redirect unknown routes to `index.html` for client-side routing.

## Server Deployment

### Production Build

```bash
dotnet publish -c Release -o ./publish
```

Output: a self-contained ASP.NET Core application.

### Docker

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish MyApp.Server -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "MyApp.Server.dll"]
```

```bash
docker build -t myapp .
docker run -p 8080:8080 myapp
```

### Azure App Service

```bash
az webapp up \
    --name myapp \
    --resource-group mygroup \
    --runtime "DOTNET|10.0"
```

### Azure Container Apps

```bash
az containerapp up \
    --name myapp \
    --resource-group mygroup \
    --image myapp:latest \
    --target-port 8080
```

### WebSocket Considerations

For `InteractiveServer` and `InteractiveAuto` modes, ensure your infrastructure supports WebSockets:

- **Azure App Service**: Enable WebSockets in Configuration → General Settings
- **Nginx reverse proxy**: Add WebSocket upgrade headers
- **Load balancer**: Use sticky sessions or configure WebSocket affinity

```nginx
# Nginx WebSocket proxy
location /ws {
    proxy_pass http://backend;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
    proxy_set_header Host $host;
}
```

## Render Mode Deployment Matrix

| Render Mode | Hosting Required | WebSocket | Static Host OK? |
| ----------- | ---------------- | --------- | --------------- |
| `Static` | Any static host | No | ✅ |
| `InteractiveWasm` | Any static host | No | ✅ |
| `InteractiveServer` | .NET server | Yes | ❌ |
| `InteractiveAuto` | .NET server | Yes (initially) | ❌ |

## Caching

### Recommended Headers

```nginx
# Framework files (fingerprinted) — cache forever
location /_framework {
    add_header Cache-Control "public, max-age=31536000, immutable";
}

# index.html — never cache
location = /index.html {
    add_header Cache-Control "no-cache, no-store, must-revalidate";
}
```

## Security

### Content Security Policy

```nginx
add_header Content-Security-Policy "
    default-src 'self';
    script-src 'self' 'wasm-unsafe-eval';
    style-src 'self' 'unsafe-inline';
    connect-src 'self' wss://yourserver.com https://api.example.com;
";
```

Note: `wss://` is needed for `InteractiveServer` WebSocket connections.

## Monitoring

### OpenTelemetry

Configure tracing export in the Aspire AppHost:

```csharp
builder.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Abies")
        .AddOtlpExporter());
```

### Health Checks

For server deployments:

```csharp
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
```

## Deployment Checklist

### WASM

- [ ] Build in Release mode with AOT + trimming + compression
- [ ] Configure SPA routing fallback on host
- [ ] Set up caching headers
- [ ] Enable HTTPS
- [ ] Add CSP headers with `wasm-unsafe-eval`
- [ ] Test with realistic data volumes

### Server

- [ ] Build in Release mode
- [ ] Enable WebSocket support on host/proxy
- [ ] Configure sticky sessions if load balanced
- [ ] Set up health checks
- [ ] Configure OpenTelemetry export
- [ ] Enable HTTPS
- [ ] Add CSP headers with `wss://` for WebSocket

## Troubleshooting

| Symptom | Cause | Fix |
| ------- | ----- | --- |
| App won't load | Missing WASM MIME type | Configure `application/wasm` on host |
| Routes 404 | No SPA fallback | Add routing redirect to `index.html` |
| Slow initial load | No AOT/compression | Enable build optimizations |
| CORS errors | API on different origin | Configure CORS on API server |
| WebSocket fails | Proxy not forwarding upgrade | Add WebSocket proxy headers |
| Server session drops | No sticky sessions | Configure session affinity |

## See Also

- [Performance](./performance.md) — Build optimization
- [Render Modes](../concepts/render-modes.md) — Choosing the right mode
- [Installation](../getting-started/installation.md) — Development setup
