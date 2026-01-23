using MedEdge.IotHubSimulator.Models;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace MedEdge.IotHubSimulator.Services;

/// <summary>
/// TPM 2.0 Simulation Service
/// Demonstrates hardware-backed security for medical devices
///
/// This shows how legacy devices can be retrofitted with TPM modules
/// for secure device identity and Azure IoT Hub provisioning
/// </summary>
public class TpmSimulationService
{
    private readonly ConcurrentDictionary<string, TpmIdentity> _tpmIdentities = new();
    private readonly ILogger<TpmSimulationService> _logger;
    private readonly X509Certificate2? _caCertificate;

    public TpmSimulationService(ILogger<TpmSimulationService> logger)
    {
        _logger = logger;
        _caCertificate = GenerateCaCertificate();
        InitializeSampleDevices();
    }

    private void InitializeSampleDevices()
    {
        // Legacy dialysis machine with TPM retrofit
        var legacyTpm = new TpmIdentity
        {
            DeviceId = "medical-dialysis-legacy-001",
            EndorsementKey = GenerateEndorsementKey(),
            StorageRootKey = GenerateStorageRootKey(),
            AttestationCertificate = GenerateDeviceCertificate("Dialysis Pro+ TPM Module", "1.2"),
            TpmVersion = "2.0",
            Manufacturer = "Infineon SLB9670",  // Common industrial TPM
            FirmwareVersion = "7.81",
            ActivationStatus = TpmActivationStatus.Activated,
            LastAttestation = DateTime.UtcNow.AddDays(-7)
        };

        // Modern dialysis machine with built-in TPM
        var modernTpm = new TpmIdentity
        {
            DeviceId = "medical-dialysis-modern-001",
            EndorsementKey = GenerateEndorsementKey(),
            StorageRootKey = GenerateStorageRootKey(),
            AttestationCertificate = GenerateDeviceCertificate("Dialysis iQ Integrated TPM", "2.1"),
            TpmVersion = "2.0",
            Manufacturer = "MedTech Custom TPM",
            FirmwareVersion = "8.0",
            ActivationStatus = TpmActivationStatus.Activated,
            LastAttestation = DateTime.UtcNow.AddHours(-2)
        };

        _tpmIdentities.TryAdd(legacyTpm.DeviceId, legacyTpm);
        _tpmIdentities.TryAdd(modernTpm.DeviceId, modernTpm);

        _logger.LogInformation("TPM Service initialized with {Count} device identities", _tpmIdentities.Count);
    }

    // ===== TPM 2.0 Key Operations =====

    /// <summary>
    /// Generate Endorsement Key (EK) - unique TPM identifier burned into hardware
    /// Maps to: Azure DPS Individual Enrollment via TPM
    /// </summary>
    public string GenerateEndorsementKey()
    {
        // Simulate hardware-generated EK (256-bit)
        var ekBytes = RandomNumberGenerator.GetBytes(32);
        var ek = Convert.ToBase64String(ekBytes);

        _logger.LogDebug("Generated TPM Endorsement Key: {EK}...", ek[..20]);
        return ek;
    }

    /// <summary>
    /// Generate Storage Root Key (SRK) - parent key for all TPM operations
    /// Maps to: Key storage hierarchy in TPM 2.0
    /// </summary>
    public string GenerateStorageRootKey()
    {
        var srkBytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(srkBytes);
    }

    /// <summary>
    /// Generate Attestation Key (AK) - used for device authentication
    /// Maps to: Azure Device authentication certificates
    /// </summary>
    public (string privateKey, string publicKey) GenerateAttestationKey()
    {
        using var rsa = RSA.Create(2048);
        var privateKey = ExportPem(rsa, includePrivate: true);
        var publicKey = ExportPem(rsa, includePrivate: false);
        return (privateKey, publicKey);
    }

    private static string ExportPem(RSA rsa, bool includePrivate)
    {
        var keyBytes = includePrivate
            ? rsa.ExportRSAPrivateKey()
            : rsa.ExportRSAPublicKey();
        return Convert.ToBase64String(keyBytes);
    }

    // ===== TPM Attestation (AK - Attestation Key) =====

    /// <summary>
    /// Perform TPM 2.0 Attestation
    /// Validates: Device identity, integrity measurements, boot state
    /// Maps to: Azure DPS TPM Attestation flow
    /// </summary>
    public async Task<TpmAttestationResult> PerformAttestationAsync(string deviceId, TpmAttestationRequest request)
    {
        _logger.LogInformation("Performing TPM attestation for device {DeviceId}", deviceId);

        // Simulate TPM operation delay
        await Task.Delay(TimeSpan.FromMilliseconds(50));

        var identity = _tpmIdentities.GetOrAdd(deviceId, _ => new TpmIdentity
        {
            DeviceId = deviceId,
            EndorsementKey = request.EndorsementKey ?? GenerateEndorsementKey(),
            StorageRootKey = request.StorageRootKey ?? GenerateStorageRootKey(),
            TpmVersion = "2.0",
            ActivationStatus = TpmActivationStatus.Pending
        });

        // Validate attestation request
        var validationResult = ValidateAttestation(identity, request);

        if (validationResult.IsValid)
        {
            identity.ActivationStatus = TpmActivationStatus.Activated;
            identity.LastAttestation = DateTime.UtcNow;

            _logger.LogInformation("TPM attestation successful for {DeviceId}", deviceId);
        }
        else
        {
            identity.ActivationStatus = TpmActivationStatus.Failed;
            _logger.LogWarning("TPM attestation failed for {DeviceId}: {Reason}",
                deviceId, validationResult.FailureReason);
        }

        return new TpmAttestationResult
        {
            DeviceId = deviceId,
            IsValid = validationResult.IsValid,
            AttestationCertificate = identity.AttestationCertificate,
            ValidationDetails = validationResult,
            Timestamp = DateTime.UtcNow
        };
    }

    private TpmValidationResult ValidateAttestation(TpmIdentity identity, TpmAttestationRequest request)
    {
        // Check EK matches (in production, verify against manufacturer registry)
        if (!string.IsNullOrEmpty(request.EndorsementKey) && request.EndorsementKey != identity.EndorsementKey)
        {
            return new TpmValidationResult
            {
                IsValid = false,
                FailureReason = "Endorsement Key mismatch"
            };
        }

        // Validate nonce (prevent replay attacks)
        if (string.IsNullOrEmpty(request.Nonce))
        {
            return new TpmValidationResult
            {
                IsValid = false,
                FailureReason = "Missing nonce - possible replay attack"
            };
        }

        // Simulate PCR (Platform Configuration Register) validation
        // PCRs measure boot state - critical for medical device integrity
        if (request.PcrValues == null || request.PcrValues.Count < 4)
        {
            return new TpmValidationResult
            {
                IsValid = false,
                FailureReason = "Insufficient PCR values - integrity measurement incomplete"
            };
        }

        return new TpmValidationResult
        {
            IsValid = true,
            ValidatedMeasurements = new Dictionary<string, string>
            {
                ["PCR_0"] = "BIOS measurement: valid",
                ["PCR_1"] = "Bootloader measurement: valid",
                ["PCR_2"] = "OS kernel measurement: valid",
                ["PCR_7"] = "Secure boot policy: valid"
            }
        };
    }

    // ===== Device Certificates (X.509) =====

    /// <summary>
    /// Generate X.509 device certificate signed by simulated B. Braun CA
    /// Maps to: Azure IoT Hub X.509 certificate authentication
    /// </summary>
    private string? GenerateDeviceCertificate(string deviceName, string firmwareVersion)
    {
        if (_caCertificate == null) return null;

        // Simulate certificate generation
        var certData = $"""
        -----BEGIN CERTIFICATE-----
        MIIDXTCCAkWgAwIBAgIJAJC1HiIAZAiIMA0GCSqGSIb3DQEBCwUAMEUxCzAJBgNV
        BAYTAkRFMRMwEQYDVQQIDApIZXNzZW4tQmF5MRUwEwYDVQQKDAxCLiBCcmF1biBB
        dml0dW0wHhcNMjUwMTAxMDAwMDAwWhcNMzUwMTAxMDAwMDAwWjBpMRMwEQYDVQQI
        DApNZWRpY2FsMTowOAYDVQQDDDFFbmRvcnNlbWVudCBLZXk6IHtTQ0hFTUFfSUR9
        {Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))}
        {Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))}
        -----END CERTIFICATE-----
        """;

        return certData;
    }

    private X509Certificate2 GenerateCaCertificate()
    {
        // In production, this would be B. Braun's private CA
        // For simulation, we create a self-signed CA
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=B. Braun Avitum Device CA, O=B. Braun, C=DE",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.CrlSign | X509KeyUsageFlags.KeyCertSign,
                critical: true));

        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(10));

        return certificate;
    }

    // ===== TPM Identity Management =====

    public TpmIdentity? GetTpmIdentity(string deviceId)
    {
        return _tpmIdentities.TryGetValue(deviceId, out var identity) ? identity : null;
    }

    public IEnumerable<TpmIdentity> GetAllIdentities()
    {
        return _tpmIdentities.Values;
    }

    public TpmIdentity RegisterDevice(string deviceId, string manufacturer = "Generic TPM")
    {
        var identity = new TpmIdentity
        {
            DeviceId = deviceId,
            EndorsementKey = GenerateEndorsementKey(),
            StorageRootKey = GenerateStorageRootKey(),
            AttestationCertificate = GenerateDeviceCertificate(manufacturer, "1.0"),
            TpmVersion = "2.0",
            Manufacturer = manufacturer,
            FirmwareVersion = "1.0",
            ActivationStatus = TpmActivationStatus.Pending
        };

        _tpmIdentities.TryAdd(deviceId, identity);
        _logger.LogInformation("Registered TPM identity for {DeviceId} with {Manufacturer}", deviceId, manufacturer);

        return identity;
    }

    // ===== Audit Trail for Medical Devices =====

    public IEnumerable<TpmAuditEntry> GetAuditTrail(string deviceId, int count = 50)
    {
        // Return simulated audit entries
        var entries = new List<TpmAuditEntry>();

        for (int i = 0; i < Math.Min(count, 10); i++)
        {
            entries.Add(new TpmAuditEntry
            {
                Timestamp = DateTime.UtcNow.AddHours(-i * 6),
                DeviceId = deviceId,
                Event = i % 3 == 0 ? "Attestation" : i % 2 == 0 ? "Key Rotation" : "Integrity Check",
                Result = "Success",
                PerformedBy = "TPM-2.0-Hardware",
                Details = i % 3 == 0 ? "EK validated, PCR values within threshold" : "SRK rotated successfully"
            });
        }

        return entries.OrderByDescending(e => e.Timestamp);
    }
}

// ===== Supporting Models =====

public class TpmIdentity
{
    public string DeviceId { get; set; } = string.Empty;
    public string EndorsementKey { get; set; } = string.Empty;
    public string StorageRootKey { get; set; } = string.Empty;
    public string? AttestationCertificate { get; set; }
    public string TpmVersion { get; set; } = "2.0";
    public string Manufacturer { get; set; } = "Unknown";
    public string FirmwareVersion { get; set; } = "1.0";
    public TpmActivationStatus ActivationStatus { get; set; } = TpmActivationStatus.Pending;
    public DateTime LastAttestation { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Properties { get; set; } = new();
}

public enum TpmActivationStatus
{
    Pending,
    Activated,
    Failed,
    Revoked
}

public class TpmAttestationRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string? EndorsementKey { get; set; }
    public string? StorageRootKey { get; set; }
    public string Nonce { get; set; } = Guid.NewGuid().ToString();
    public Dictionary<int, string>? PcrValues { get; set; }  // Platform Configuration Registers
}

public class TpmAttestationResult
{
    public string DeviceId { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string? AttestationCertificate { get; set; }
    public TpmValidationResult? ValidationDetails { get; set; }
    public DateTime Timestamp { get; set; }
}

public class TpmValidationResult
{
    public bool IsValid { get; set; }
    public string? FailureReason { get; set; }
    public Dictionary<string, string>? ValidatedMeasurements { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public class TpmAuditEntry
{
    public DateTime Timestamp { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public string? Details { get; set; }
}
