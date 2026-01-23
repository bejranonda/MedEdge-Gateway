using MedEdge.IotHubSimulator.Models;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace MedEdge.IotHubSimulator.Services;

/// <summary>
/// JWT Authentication Service for Medical Device Identity
/// Demonstrates: Device authentication, token issuance, certificate-based auth
///
/// For B. Braun: Shows how dialysis machines authenticate to Azure IoT Hub
/// using SAS tokens (Shared Access Signature) or X.509 certificates
/// </summary>
public class JwtAuthenticationService
{
    private readonly ILogger<JwtAuthenticationService> _logger;
    private readonly string _signingKey;
    private readonly TpmSimulationService _tpmService;
    private readonly ConcurrentDictionary<string, DeviceSession> _activeSessions = new();

    public JwtAuthenticationService(
        ILogger<JwtAuthenticationService> logger,
        TpmSimulationService tpmService)
    {
        _logger = logger;
        _tpmService = tpmService;
        _signingKey = GenerateSigningKey();
    }

    // ===== JWT Token Issuance =====

    /// <summary>
    /// Generate SAS Token (Shared Access Signature) for device authentication
    /// Maps to: Azure IoT Hub SAS Token authentication
    /// </summary>
    public string GenerateDeviceSasToken(string deviceId, TimeSpan expiry)
    {
        var expires = DateTime.UtcNow.Add(expiry);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, deviceId),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new(JwtRegisteredClaimNames.Exp, new DateTimeOffset(expires).ToUnixTimeSeconds().ToString()),
            new("iothub", "mededge-simulated"),
            new("device_id", deviceId),
            new("aud", "mededge.azure-devices.net")  // Simulated Azure IoT Hub
        };

        var token = GenerateJwtToken(claims, expires);
        _logger.LogInformation("Generated SAS token for {DeviceId}, expires at {Expires}", deviceId, expires);

        return token;
    }

    /// <summary>
    /// Generate token for user/service authentication
    /// Maps to: Azure RBAC service principal authentication
    /// </summary>
    public string GenerateServiceToken(string serviceId, string[] scopes, TimeSpan expiry)
    {
        var expires = DateTime.UtcNow.Add(expiry);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, serviceId),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new(JwtRegisteredClaimNames.Exp, new DateTimeOffset(expires).ToUnixTimeSeconds().ToString()),
            new("scope", string.Join(" ", scopes)),
            new("role", "service")
        };

        return GenerateJwtToken(claims, expires);
    }

    private string GenerateJwtToken(List<Claim> claims, DateTime expires)
    {
        var key = new SymmetricSecurityKey(Convert.FromBase64String(_signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "mededge-iot-hub",
            audience: "mededge.azure-devices.net",
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ===== Token Validation =====

    /// <summary>
    /// Validate SAS token from device
    /// Maps to: Azure IoT Hub token validation
    /// </summary>
    public TokenValidationResult ValidateToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Convert.FromBase64String(_signingKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "mededge-iot-hub",
                ValidateAudience = true,
                ValidAudience = "mededge.azure-devices.net",
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);

            var deviceId = principal.FindFirst("device_id")?.Value;
            var sessionId = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value ?? Guid.NewGuid().ToString();

            // Track active session
            if (!string.IsNullOrEmpty(deviceId))
            {
                _activeSessions.TryAdd(sessionId, new DeviceSession
                {
                    SessionId = sessionId,
                    DeviceId = deviceId,
                    AuthenticatedAt = DateTime.UtcNow,
                    ExpiresAt = validatedToken.ValidTo
                });
            }

            _logger.LogInformation("Token validated for device {DeviceId}", deviceId ?? "unknown");

            return new TokenValidationResult
            {
                IsValid = true,
                DeviceId = deviceId,
                Principal = principal,
                ExpiresAt = validatedToken.ValidTo
            };
        }
        catch (SecurityTokenExpiredException)
        {
            return new TokenValidationResult
            {
                IsValid = false,
                Error = "Token expired",
                ErrorCode = "TOKEN_EXPIRED"
            };
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return new TokenValidationResult
            {
                IsValid = false,
                Error = "Invalid signature",
                ErrorCode = "INVALID_SIGNATURE"
            };
        }
        catch (Exception ex)
        {
            return new TokenValidationResult
            {
                IsValid = false,
                Error = ex.Message,
                ErrorCode = "VALIDATION_ERROR"
            };
        }
    }

    // ===== X.509 Certificate Authentication =====

    /// <summary>
    /// Validate X.509 certificate for device authentication
    /// Maps to: Azure IoT Hub X.509 certificate authentication
    /// </summary>
    public async Task<CertificateValidationResult> ValidateCertificateAsync(string deviceId, string certificatePem)
    {
        _logger.LogInformation("Validating X.509 certificate for device {DeviceId}", deviceId);

        // Simulate certificate validation delay
        await Task.Delay(TimeSpan.FromMilliseconds(100));

        // Parse certificate
        if (!certificatePem.Contains("BEGIN CERTIFICATE"))
        {
            return new CertificateValidationResult
            {
                IsValid = false,
                Error = "Invalid certificate format",
                ErrorCode = "INVALID_FORMAT"
            };
        }

        // In production, we would:
        // 1. Verify certificate chain to B. Braun CA
        // 2. Check certificate revocation (CRL/OCSP)
        // 3. Validate certificate expiration
        // 4. Verify device ID matches certificate CN/SAN

        // Simulate certificate validation
        var tpmIdentity = _tpmService.GetTpmIdentity(deviceId);

        var result = new CertificateValidationResult
        {
            IsValid = true,
            DeviceId = deviceId,
            Issuer = "CN=B. Braun Avitum Device CA, O=B. Braun, C=DE",
            Subject = $"CN={deviceId}, O=B. Braun Avitum",
            SerialNumber = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
            ValidFrom = DateTime.UtcNow.AddDays(-30),
            ValidTo = DateTime.UtcNow.AddYears(1),
            ValidationChecks = new Dictionary<string, bool>
            {
                ["ChainTrust"] = true,
                ["Revocation"] = true,  // Not revoked
                ["Expiration"] = true,  // Not expired
                ["DeviceMatch"] = tpmIdentity != null,
                ["TpmBound"] = tpmIdentity != null  // Certificate bound to TPM
            }
        };

        if (tpmIdentity == null)
        {
            result.IsValid = false;
            result.Error = "Certificate not bound to TPM identity";
            result.ErrorCode = "TPM_MISMATCH";
        }

        _logger.LogInformation("Certificate validation for {DeviceId}: {Result}",
            deviceId, result.IsValid ? "Valid" : "Invalid");

        return result;
    }

    // ===== Device Registration with Certificate =====

    /// <summary>
    /// Register device with X.509 certificate
    /// Maps to: Azure IoT Hub X.509 device enrollment
    /// </summary>
    public async Task<DeviceRegistrationResult> RegisterDeviceWithCertificateAsync(
        string deviceId,
        string certificatePem,
        Dictionary<string, string>? tags = null)
    {
        _logger.LogInformation("Registering device {DeviceId} with X.509 certificate", deviceId);

        // Validate certificate first
        var certValidation = await ValidateCertificateAsync(deviceId, certificatePem);
        if (!certValidation.IsValid)
        {
            return new DeviceRegistrationResult
            {
                Success = false,
                DeviceId = deviceId,
                Error = certValidation.Error,
                ErrorCode = certValidation.ErrorCode
            };
        }

        // Check TPM attestation
        var tpmIdentity = _tpmService.GetTpmIdentity(deviceId);
        if (tpmIdentity == null)
        {
            tpmIdentity = _tpmService.RegisterDevice(deviceId, "X.509 Registered Device");
        }

        // Generate SAS token for immediate use
        var sasToken = GenerateDeviceSasToken(deviceId, TimeSpan.FromDays(30));

        return new DeviceRegistrationResult
        {
            Success = true,
            DeviceId = deviceId,
            SasToken = sasToken,
            CertificateThumbprint = certValidation.Thumbprint,
            AssignedHub = "mededge-iot-hub",
            RegisteredAt = DateTime.UtcNow,
            Tags = tags ?? new Dictionary<string, string>()
        };
    }

    // ===== Session Management =====

    public IEnumerable<DeviceSession> GetActiveSessions(string? deviceId = null)
    {
        var sessions = _activeSessions.Values.Where(s => s.ExpiresAt > DateTime.UtcNow);

        if (!string.IsNullOrEmpty(deviceId))
        {
            sessions = sessions.Where(s => s.DeviceId == deviceId);
        }

        return sessions.OrderByDescending(s => s.AuthenticatedAt);
    }

    public bool RevokeSession(string sessionId)
    {
        return _activeSessions.TryRemove(sessionId, out _);
    }

    // ===== Audit Trail =====

    public IEnumerable<AuthAuditEntry> GetAuthAuditTrail(string deviceId, int count = 50)
    {
        var entries = new List<AuthAuditEntry>();

        for (int i = 0; i < Math.Min(count, 20); i++)
        {
            entries.Add(new AuthAuditEntry
            {
                Timestamp = DateTime.UtcNow.AddHours(-i * 2),
                DeviceId = deviceId,
                AuthMethod = i % 2 == 0 ? "SAS Token" : "X.509 Certificate",
                Result = i > 15 ? "Failed" : "Success",
                SourceIp = $"192.168.1.{100 + i % 50}",
                UserAgent = "AzureDeviceSDK/1.0",
                Details = i > 15 ? "Token expired" : "Authentication successful"
            });
        }

        return entries.OrderByDescending(e => e.Timestamp);
    }

    private string GenerateSigningKey()
    {
        // Generate a 256-bit signing key
        var keyBytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(keyBytes);
    }
}

// ===== Supporting Models =====

public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public string? DeviceId { get; set; }
    public ClaimsPrincipal? Principal { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? Error { get; set; }
    public string? ErrorCode { get; set; }
}

public class CertificateValidationResult
{
    public bool IsValid { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string? Issuer { get; set; }
    public string? Subject { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public string? Thumbprint => Convert.ToHexString(RandomNumberGenerator.GetBytes(20));
    public Dictionary<string, bool>? ValidationChecks { get; set; }
    public string? Error { get; set; }
    public string? ErrorCode { get; set; }
}

public class DeviceRegistrationResult
{
    public bool Success { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string? SasToken { get; set; }
    public string? CertificateThumbprint { get; set; }
    public string? AssignedHub { get; set; }
    public DateTime RegisteredAt { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
    public string? Error { get; set; }
    public string? ErrorCode { get; set; }
}

public class DeviceSession
{
    public string SessionId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public DateTime AuthenticatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string SourceIp { get; set; } = "192.168.1.1";
    public string AuthMethod { get; set; } = "SAS Token";
}

public class AuthAuditEntry
{
    public DateTime Timestamp { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string AuthMethod { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string SourceIp { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string? Details { get; set; }
}
