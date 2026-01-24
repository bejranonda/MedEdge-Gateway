// Base configuration
window.MedEdgeConfig = {
    // For Cloudflare Pages with external backend, uncomment and modify:
    // apiBaseUrl: 'https://your-backend-api.com',
    // fhirBaseUrl: 'https://your-fhir-api.com',
    // signalHubUrl: 'https://your-signalr-api.com/hubs/telemetry',

    // For local development (same origin):
    apiBaseUrl: window.location.origin,
    fhirBaseUrl: window.location.origin,
    signalHubUrl: window.location.origin + '/hubs/telemetry',

    enableSignalR: true,
    enableFhirInspector: true,
    enableFleetView: true,

    // Timeout settings
    requestTimeout: 30000,
    signalRTimeout: 60000
};

// Load environment-specific configuration (injected during Docker build)
// This will be populated by config-env.js which is created during Docker build
// The config-env.js file is created from environment variables in .env file