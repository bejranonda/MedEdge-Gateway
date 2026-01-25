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