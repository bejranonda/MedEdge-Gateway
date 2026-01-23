# Cloudflare Pages Deployment Guide

## Overview

This guide explains how to deploy the MedEdge Blazor Dashboard to Cloudflare Pages free-tier while keeping the backend services separately hosted.

## Architecture

```
Cloudflare Pages (Static Hosting)
├── Blazor WASM Dashboard
└── config.js (Runtime configuration)

External Backend (Required)
├── FHIR API (REST/Swagger)
├── SignalR Hub (WebSocket)
├── Device API (Real-time)
└── AI Service (Anomaly detection)
```

## Prerequisites

1. **Cloudflare Account** (Free tier available)
2. **GitHub Repository** connected to Cloudflare Pages
3. **Backend API Hosted** elsewhere (Azure, AWS, Heroku, etc.)
4. **.NET 8.0 SDK** for local builds

## Quick Start

```bash
# 1. Build Dashboard
cd src/Web/MedEdge.Dashboard
dotnet publish -c Release -o ./publish

# 2. Update config.js (set backend URLs)
cd wwwroot
# Edit config.js with your backend URLs

# 3. Deploy to Cloudflare Pages
# Go to Cloudflare Dashboard → Pages → Connect to Git
```

## Detailed Steps

### 1. Backend API Setup

Your backend API must be accessible and support CORS:

```csharp
// In your backend Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("CloudflarePages", policy =>
    {
        policy.WithOrigins("https://your-dashboard.pages.dev")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

### 2. Local Build & Configuration

#### Build the Dashboard

```bash
cd /d/Git/Werapol/MedEdge/src/Web/MedEdge.Dashboard

# Clean build
dotnet clean
dotnet build -c Release

# Publish for deployment
dotnet publish -c Release -o ./publish
```

#### Configure Runtime URLs

Edit `publish/wwwroot/config.js`:

```javascript
window.MedEdgeConfig = {
    // === REQUIRED: Update these URLs ===
    apiBaseUrl: 'https://your-backend-api.com',      // Your backend API base URL
    fhirBaseUrl: 'https://your-fhir-api.com',         // FHIR API endpoint
    signalHubUrl: 'https://your-signalr-api.com/hubs/telemetry',  // SignalR endpoint

    // Feature flags (keep true unless debugging)
    enableSignalR: true,
    enableFhirInspector: true,
    enableFleetView: true,

    // Timeout settings (milliseconds)
    requestTimeout: 30000,
    signalRTimeout: 60000
};
```

### 3. Cloudflare Pages Deployment

#### Option A: GitHub Integration (Recommended)

1. **Connect Repository**
   - Go to [Cloudflare Dashboard](https://dash.cloudflare.com/)
   - Navigate to **Workers & Pages** > **Pages**
   - Click **Create project** > **Connect to Git**
   - Select your GitHub repository
   - **MedEdge-Gateway**

2. **Configure Build Settings**
   ```
   Build command: dotnet publish src/Web/MedEdge.Dashboard/MedEdge.Dashboard.csproj -c Release -o ./publish
   Build output directory: publish/wwwroot
   Root directory: / (empty)
   ```

3. **Environment Variables** (Optional)
   ```env
   ASPNETCORE_ENVIRONMENT=Production
   ```

4. **Deploy**
   - Click **Save and Deploy**
   - Wait for build to complete
   - Visit your `.pages.dev` URL

#### Option B: Manual Upload

1. **Build as above**
2. **Zip the files**:
   ```bash
   cd publish
   zip -r mededge-dashboard.zip wwwroot/*
   ```

3. **Upload via Cloudflare Dashboard**

### 4. Runtime Configuration

#### After Initial Deployment

You can update configuration without rebuilding:

1. **Edit config.js via Cloudflare Dashboard**
   - Go to your Pages project
   - **Settings** > **Pages** > **Static files**
   - Edit `wwwroot/config.js`
   - Save changes

2. **Use Custom Domain** (Optional)
   - Add your domain in Cloudflare Pages
   - Update CORS on backend if needed

### 5. Backend CORS Configuration

Your backend must allow requests from Cloudflare Pages:

```csharp
// Backend API Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("CloudflarePages", policy =>
    {
        policy.WithOrigins("https://your-project.pages.dev")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

## Configuration File Reference

### config.js Properties

| Property | Description | Default |
|---------|-------------|---------|
| `apiBaseUrl` | Base URL for device API | `window.location.origin` |
| `fhirBaseUrl` | Base URL for FHIR API | `window.location.origin` |
| `signalHubUrl` | SignalR WebSocket URL | `${apiBaseUrl}/hubs/telemetry` |
| `enableSignalR` | Enable SignalR connection | `true` |
| `enableFhirInspector` | Enable FHIR browser | `true` |
| `requestTimeout` | HTTP request timeout | `30000` ms |
| `signalRTimeout` | SignalR timeout | `60000` ms |

### _redirects Rules

```
/* /index.html 200
```
- Routes all paths to SPA (Blazor router)

### _headers Configuration

```
/_framework/*
  Cache-Control: public, max-age=31536000, immutable

/*.css, *.js
  Cache-Control: public, max-age=31536000, immutable

/config.js
  Cache-Control: no-cache, no-store, must-revalidate
```

## Testing & Verification

### 1. Load Test Dashboard
```bash
# Visit your Cloudflare Pages URL
https://your-project.pages.dev
```

### 2. Verify Features
- **FHIR Inspector**: Should load Patient/Device data
- **Fleet View**: Should display device cards
- **Vitals Monitor**: Should connect via SignalR

### 3. Browser Console
Check for:
- ❌ CORS errors
- ❌ WebSocket connection errors
- ✌️ All API calls successful

### 4. Network Tab (DevTools)
- Requests should go to your backend
- Response headers include `Access-Control-Allow-Origin`

## Troubleshooting

### CORS Errors
**Problem**: "No 'Access-Control-Allow-Origin' header"
**Solution**: Enable CORS on backend for your Cloudflare Pages domain

### SignalR Connection Fails
**Problem**: WebSocket connection rejected
**Solution**:
1. Check `signalHubUrl` in `config.js`
2. Verify backend allows WebSocket connections
3. Check if backend is accessible from web

### 404 Not Found
**Problem**: API endpoints return 404
**Solution**:
1. Verify `apiBaseUrl` in `config.js`
2. Check backend is running and accessible
3. Ensure URLs end with `/`

### Configuration Not Loading
**Problem**: Dashboard shows default config
**Solution**:
1. Verify `config.js` exists at `/config.js`
2. Check for JavaScript errors in console
3. Ensure `config.js` is not cached

### Build Failures
**Problem**: Cloudflare build fails
**Solution**:
1. Check .NET 8.0 SDK version
2. Verify project syntax
3. Check for missing dependencies

## Performance Optimization

### Caching Strategy
- **Static assets**: 1 year (immutable)
- **Configuration**: No cache
- **HTML**: No cache (always fresh)

### Bundle Size
- Blazor WASM: ~2MB first load
- Framework cached after first visit
- Optimized for mobile 4G connections

### CDN Benefits
- **Global edge caching**: Faster load times worldwide
- **Compression**: Automatic Brotli/Gzip
- **DDoS Protection**: Included with free tier

## Cost Analysis

### Cloudflare Pages Free Tier
- **Builds**: 500/month
- **Bandwidth**: Unlimited
- **Custom Domains**: 1
- **Pages**: 1
- **Cost**: $0

### Estimated Monthly Usage
- **10 users**: ~300 requests/day = ~9,000 requests/month
- **100 users**: ~3,000 requests/day = ~90,000 requests/month
- **Well within free limits**

## Monitoring

### Cloudflare Analytics
Access via Cloudflare Dashboard:
- Page views & unique visitors
- Bandwidth usage
- Error rates
- Popular pages

### Uptime Monitoring
Set up monitoring:
- **Health checks**: `/health` endpoint
- **Uptime monitoring**: External services

## Security Considerations

### HTTPS
- Automatic HTTPS on Cloudflare Pages
- SSL/TLS certificate included

### Input Validation
- Backend validates all inputs
- No secrets in frontend code

### Rate Limiting
- Configure on Cloudflare dashboard
- Protect against abuse

## Advanced Configuration

### Custom Domains
```javascript
// config.js with custom domain
window.MedEdgeConfig = {
    apiBaseUrl: 'https://api.yourdomain.com',
    fhirBaseUrl: 'https://api.yourdomain.com',
    signalHubUrl: 'https://api.yourdomain.com/hubs/telemetry'
};
```

### Environment-Specific Configs
```javascript
// Development
window.MedEdgeConfig = {
    apiBaseUrl: 'http://localhost:5001',
    // ...
};

// Production
window.MedEdgeConfig = {
    apiBaseUrl: 'https://api.yourdomain.com',
    // ...
};
```

## Migration from Docker

### Benefits of Cloudflare Pages
- ✅ **Static hosting**: Better performance
- ✅ **CDN**: Global distribution
- ✅ **Free tier**: Cost-effective
- ✅ **Zero maintenance**: Managed service

### What Changes
- **Dashboard**: Moves to Cloudflare Pages
- **Backend**: Hosted separately (Azure, AWS, etc.)
- **SignalR**: WebSocket proxy may be needed

## Support

### Resources
- [Cloudflare Pages Docs](https://developers.cloudflare.com/pages/)
- [Blazor WebAssembly Deployment](https://docs.microsoft.com/aspnet/core/blazor/host-and-deploy/webassembly)
- [MedEdge Repository](https://github.com/bejranonda/MedEdge-Gateway)

### Issues
- Report bugs: [GitHub Issues](https://github.com/bejranonda/MedEdge-Gateway/issues)
- Cloudflare support: [Cloudflare Status](https://www.cloudflarestatus.com/)

---

**Last Updated**: January 23, 2026
**Version**: 1.0.0
**Status**: Production Ready