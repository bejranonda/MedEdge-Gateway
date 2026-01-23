using MedEdge.IotHubSimulator.Models;
using MedEdge.IotHubSimulator.Services;

namespace MedEdge.IotHubSimulator.Endpoints;

/// <summary>
/// Security and Authentication Endpoints
/// Maps to Azure IoT Hub Security features:
/// - SAS Token authentication
/// - X.509 certificate validation
/// - TPM 2.0 hardware attestation
/// </summary>
public static class SecurityEndpoints
{
    public static void MapSecurityEndpoints(this IEndpointRouteBuilder app)
    {
        // ===== Authentication Endpoints =====

        var authGroup = app.MapGroup("/iot/auth")
            .WithTags("Authentication")
            .WithOpenApi();

        authGroup.MapPost("/tokens/sas", (string deviceId, TimeSpan? expiry, JwtAuthenticationService authService) =>
        {
            var token = authService.GenerateDeviceSasToken(deviceId, expiry ?? TimeSpan.FromHours(1));
            return Results.Ok(new SasTokenResponse
            {
                Token = token,
                DeviceId = deviceId,
                ExpiresAt = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromHours(1)),
                TokenType = "Sas"
            });
        })
        .WithSummary("Generate SAS token")
        .WithDescription("Generate Shared Access Signature token for device authentication");

        authGroup.MapPost("/tokens/validate", (TokenValidationRequest request, JwtAuthenticationService authService) =>
        {
            var result = authService.ValidateToken(request.Token);
            return result.IsValid ? Results.Ok(result) : Results.UnprocessableEntity(result);
        })
        .WithSummary("Validate SAS token")
        .WithDescription("Validate authentication token and return device identity");

        authGroup.MapPost("/certificates/validate", async (string deviceId, string certificatePem, JwtAuthenticationService authService) =>
        {
            var result = await authService.ValidateCertificateAsync(deviceId, certificatePem);
            return result.IsValid ? Results.Ok(result) : Results.UnprocessableEntity(result);
        })
        .WithSummary("Validate X.509 certificate")
        .WithDescription("Validate device X.509 certificate and check revocation status");

        authGroup.MapPost("/register", async (DeviceRegistrationRequest request, JwtAuthenticationService authService) =>
        {
            var result = await authService.RegisterDeviceWithCertificateAsync(
                request.DeviceId, request.CertificatePem, request.Tags);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithSummary("Register device with certificate")
        .WithDescription("Register new device with X.509 certificate enrollment");

        // ===== TPM 2.0 Attestation Endpoints =====

        var tpmGroup = app.MapGroup("/iot/tpm")
            .WithTags("TPM Attestation")
            .WithOpenApi();

        tpmGroup.MapGet("/identities", (TpmSimulationService tpmService) =>
        {
            return Results.Ok(tpmService.GetAllIdentities());
        })
        .WithSummary("List TPM identities")
        .WithDescription("Get all registered TPM device identities");

        tpmGroup.MapGet("/identities/{deviceId}", (string deviceId, TpmSimulationService tpmService) =>
        {
            var identity = tpmService.GetTpmIdentity(deviceId);
            return identity is not null ? Results.Ok(identity) : Results.NotFound();
        })
        .WithSummary("Get TPM identity")
        .WithDescription("Get TPM identity details for a device");

        tpmGroup.MapPost("/attest", async (TpmAttestationRequest request, TpmSimulationService tpmService) =>
        {
            var result = await tpmService.PerformAttestationAsync(request.DeviceId, request);
            return Results.Ok(result);
        })
        .WithSummary("Perform TPM attestation")
        .WithDescription("Execute TPM 2.0 attestation with EK, SRK, and PCR validation");

        tpmGroup.MapPost("/identities", (string deviceId, string? manufacturer, TpmSimulationService tpmService) =>
        {
            var identity = tpmService.RegisterDevice(deviceId, manufacturer ?? "Generic TPM");
            return Results.Created($"/iot/tpm/identities/{deviceId}", identity);
        })
        .WithSummary("Register TPM identity")
        .WithDescription("Register new TPM identity for a device");

        tpmGroup.MapGet("/identities/{deviceId}/audit", (string deviceId, int count, TpmSimulationService tpmService) =>
        {
            return Results.Ok(tpmService.GetAuditTrail(deviceId, count));
        })
        .WithSummary("Get TPM audit trail")
        .WithDescription("Retrieve TPM attestation and key rotation audit log");

        // ===== Session Management =====

        var sessionGroup = app.MapGroup("/iot/sessions")
            .WithTags("Sessions")
            .WithOpenApi();

        sessionGroup.MapGet("/", (string? deviceId, JwtAuthenticationService authService) =>
        {
            return Results.Ok(authService.GetActiveSessions(deviceId));
        })
        .WithSummary("List active sessions")
        .WithDescription("Get all currently authenticated device sessions");

        sessionGroup.MapDelete("/{sessionId}", (string sessionId, JwtAuthenticationService authService) =>
        {
            return authService.RevokeSession(sessionId)
                ? Results.NoContent()
                : Results.NotFound();
        })
        .WithSummary("Revoke session")
        .WithDescription("Revoke authentication session (logout)");

        // ===== Audit Trail =====

        app.MapGet("/iot/audit/auth", (string deviceId, int count, JwtAuthenticationService authService) =>
        {
            return Results.Ok(authService.GetAuthAuditTrail(deviceId, count));
        })
        .WithTags("Audit")
        .WithSummary("Authentication audit trail")
        .WithDescription("Get authentication and authorization audit log");

        // ===== Security Configuration =====

        app.MapGet("/iot/security/config", () =>
        {
            return Results.Ok(new SecurityConfig
            {
                TpmRequired = false,  // Can be enabled per device
                SasTokenExpiry = "PT1H",  // ISO 8601 duration
                CertificateRequired = true,
                SupportedAlgorithms = new[] { "RSA-2048", "HMAC-SHA256" },
                TpmVersion = "2.0",
                MinTpmFirmware = "7.60",
                EncryptionAtRest = true,
                TLSVersion = "1.3"
            });
        })
        .WithTags("Configuration")
        .WithSummary("Security configuration")
        .WithDescription("Get IoT Hub security policies and requirements");
    }
}

// ===== Request/Response Models =====

public record SasTokenResponse
{
    public string Token { get; init; } = string.Empty;
    public string DeviceId { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public string TokenType { get; init; } = string.Empty;
}

public record TokenValidationRequest
{
    public string Token { get; init; } = string.Empty;
}

public record DeviceRegistrationRequest
{
    public string DeviceId { get; init; } = string.Empty;
    public string CertificatePem { get; init; } = string.Empty;
    public Dictionary<string, string>? Tags { get; init; }
}

// TpmAttestationRequest is defined in Services namespace (TpmSimulationService.cs)

public record SecurityConfig
{
    public bool TpmRequired { get; init; }
    public string SasTokenExpiry { get; init; } = string.Empty;
    public bool CertificateRequired { get; init; }
    public string[] SupportedAlgorithms { get; init; } = Array.Empty<string>();
    public string TpmVersion { get; init; } = string.Empty;
    public string MinTpmFirmware { get; init; } = string.Empty;
    public bool EncryptionAtRest { get; init; }
    public string TLSVersion { get; init; } = string.Empty;
}
