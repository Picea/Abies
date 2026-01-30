# Deployment Guide

How to build and deploy Abies applications to production.

## Overview

Abies applications are .NET WebAssembly apps that can be deployed to any static hosting provider. This guide covers:

- Production builds
- Hosting options
- Configuration
- Monitoring

## Production Build

### Build Command

```bash
dotnet publish -c Release
```

This produces optimized output in `bin/Release/net9.0/publish/wwwroot/`.

### Build Options

Configure in your `.csproj`:

```xml
<PropertyGroup>
    <!-- AOT compilation for faster startup -->
    <RunAOTCompilation>true</RunAOTCompilation>
    
    <!-- IL trimming for smaller bundles -->
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>link</TrimMode>
    
    <!-- Brotli compression -->
    <BlazorEnableCompression>true</BlazorEnableCompression>
</PropertyGroup>
```

### Build Output

After publishing:

```text
bin/Release/net9.0/publish/wwwroot/
├── _framework/
│   ├── dotnet.js
│   ├── dotnet.wasm
│   ├── YourApp.dll
│   └── ...
├── index.html
├── css/
└── ...
```

## Hosting Options

### Static File Hosting

Abies apps are static files and can be hosted anywhere:

#### Azure Static Web Apps

```bash
# Install Azure CLI
az login

# Create Static Web App
az staticwebapp create \
    --name myapp \
    --resource-group mygroup \
    --source https://github.com/user/repo \
    --branch main \
    --app-location "/MyApp" \
    --output-location "wwwroot"
```

Or use `staticwebapp.config.json`:

```json
{
    "navigationFallback": {
        "rewrite": "/index.html"
    }
}
```

#### GitHub Pages

Add workflow `.github/workflows/deploy.yml`:

```yaml
name: Deploy to GitHub Pages

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      
      - name: Publish
        run: dotnet publish -c Release
      
      - name: Deploy
        uses: peaceiris/actions-gh-pages@v3
        with:
          github_token: ${{ secrets.GITHUB_TOKEN }}
          publish_dir: ./bin/Release/net9.0/publish/wwwroot
```

#### Netlify

Add `netlify.toml`:

```toml
[build]
  command = "dotnet publish -c Release"
  publish = "bin/Release/net9.0/publish/wwwroot"

[[redirects]]
  from = "/*"
  to = "/index.html"
  status = 200
```

#### Vercel

Add `vercel.json`:

```json
{
    "rewrites": [
        { "source": "/(.*)", "destination": "/index.html" }
    ]
}
```

### Docker

Create a `Dockerfile`:

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

# Runtime stage - use nginx for static hosting
FROM nginx:alpine
COPY --from=build /app/wwwroot /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

With `nginx.conf`:

```nginx
server {
    listen 80;
    root /usr/share/nginx/html;
    index index.html;
    
    # SPA fallback
    location / {
        try_files $uri $uri/ /index.html;
    }
    
    # Cache static assets
    location /_framework {
        add_header Cache-Control "public, max-age=31536000";
    }
}
```

Build and run:

```bash
docker build -t myapp .
docker run -p 8080:80 myapp
```

### Azure App Service

Deploy with Azure CLI:

```bash
az webapp up \
    --name myapp \
    --resource-group mygroup \
    --runtime "DOTNET|9.0"
```

## Configuration

### Environment Variables

Access configuration at runtime:

```csharp
public class MyProgram : Program<Model, AppConfig>
{
    public static (Model, Command) Initialize(Url url, AppConfig config)
    {
        var apiUrl = config.ApiUrl;
        return (new Model(ApiUrl: apiUrl), Commands.None);
    }
}

// In Program.Main:
var config = new AppConfig(
    ApiUrl: Environment.GetEnvironmentVariable("API_URL") ?? "https://api.example.com"
);
await Runtime.Run<MyProgram, AppConfig, Model>(config);
```

### Build-Time Configuration

Use MSBuild properties:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <DefineConstants>PRODUCTION</DefineConstants>
</PropertyGroup>
```

```csharp
#if PRODUCTION
    var apiUrl = "https://api.example.com";
#else
    var apiUrl = "http://localhost:5000";
#endif
```

### appsettings.json

For complex configuration:

```json
{
    "ApiUrl": "https://api.example.com",
    "Features": {
        "EnableAnalytics": true,
        "MaxPageSize": 20
    }
}
```

Load in JavaScript and pass to .NET:

```javascript
// In index.html or separate script
fetch('appsettings.json')
    .then(r => r.json())
    .then(config => {
        window.appConfig = config;
        // Start the app
    });
```

## SPA Routing

All hosting platforms need to redirect unknown routes to `index.html` for client-side routing to work.

### Common Configurations

**Apache** (`.htaccess`):

```apache
<IfModule mod_rewrite.c>
    RewriteEngine On
    RewriteBase /
    RewriteRule ^index\.html$ - [L]
    RewriteCond %{REQUEST_FILENAME} !-f
    RewriteCond %{REQUEST_FILENAME} !-d
    RewriteRule . /index.html [L]
</IfModule>
```

**IIS** (`web.config`):

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <system.webServer>
        <rewrite>
            <rules>
                <rule name="SPA" stopProcessing="true">
                    <match url=".*" />
                    <conditions logicalGrouping="MatchAll">
                        <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
                        <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
                    </conditions>
                    <action type="Rewrite" url="/index.html" />
                </rule>
            </rules>
        </rewrite>
    </system.webServer>
</configuration>
```

## Caching

### Recommended Headers

```nginx
# Framework files (fingerprinted) - cache forever
location /_framework {
    add_header Cache-Control "public, max-age=31536000, immutable";
}

# index.html - no cache
location = /index.html {
    add_header Cache-Control "no-cache, no-store, must-revalidate";
}

# Other static assets
location /assets {
    add_header Cache-Control "public, max-age=86400";
}
```

### Service Worker

For offline support, add a service worker:

```javascript
// service-worker.js
const CACHE_NAME = 'app-v1';

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => {
            return cache.addAll([
                '/',
                '/index.html',
                '/_framework/dotnet.js',
                '/_framework/dotnet.wasm',
                // ... other assets
            ]);
        })
    );
});

self.addEventListener('fetch', event => {
    event.respondWith(
        caches.match(event.request).then(response => {
            return response || fetch(event.request);
        })
    );
});
```

Register in `index.html`:

```html
<script>
    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.register('/service-worker.js');
    }
</script>
```

## Monitoring

### Error Tracking

Integrate with error tracking services:

```csharp
public static async Task HandleCommand(Command command, Func<Message, ValueTuple> dispatch)
{
    try
    {
        // ... handle command
    }
    catch (Exception ex)
    {
        // Send to error tracking
        await Interop.ReportError(ex.ToString());
        dispatch(new ErrorOccurred(ex.Message));
    }
}
```

### Analytics

Track page views:

```csharp
public static Message OnUrlChanged(Url url)
{
    // Track page view
    Interop.TrackPageView(url.Path.Value);
    return new UrlChanged(url);
}
```

### OpenTelemetry

Configure tracing export:

```csharp
// In Aspire host
builder.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Abies")
        .AddOtlpExporter());
```

## Health Checks

Add a health endpoint if using ASP.NET Core hosting:

```csharp
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
```

## Security

### Content Security Policy

Add CSP headers:

```nginx
add_header Content-Security-Policy "
    default-src 'self';
    script-src 'self' 'wasm-unsafe-eval';
    style-src 'self' 'unsafe-inline';
    connect-src 'self' https://api.example.com;
";
```

### HTTPS

Always use HTTPS in production. Most hosting providers handle this automatically.

## Deployment Checklist

Before deploying:

- [ ] Build in Release mode
- [ ] Enable AOT compilation
- [ ] Enable trimming
- [ ] Enable compression
- [ ] Configure SPA routing
- [ ] Set up caching headers
- [ ] Configure environment variables
- [ ] Set up error tracking
- [ ] Configure analytics
- [ ] Enable HTTPS
- [ ] Add CSP headers
- [ ] Test on target platform

## Troubleshooting

### App Won't Load

Check:

1. Browser console for errors
2. Network tab for failed requests
3. MIME types for `.wasm` files (should be `application/wasm`)

### Routes 404

Ensure SPA fallback is configured on your hosting platform.

### Slow Initial Load

- Enable AOT compilation
- Enable compression
- Use CDN for static assets
- Add loading indicator

### CORS Errors

Configure your API to allow requests from your app's domain:

```csharp
// In API
app.UseCors(policy => policy
    .WithOrigins("https://myapp.com")
    .AllowAnyMethod()
    .AllowAnyHeader());
```

## See Also

- [Guide: Performance](./performance.md) — Build optimization
- [Getting Started: Installation](../getting-started/installation.md) — Development setup
- [Tutorial: Real World App](../tutorials/07-real-world-app.md) — Complete example
