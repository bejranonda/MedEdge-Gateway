# B. Braun Interview Demo Script

## Preparation

```bash
# Start the IoT Hub Simulator
cd D:\Git\Werapol\MedEdge
docker-compose up iot-hub-simulator

# Wait for startup, then open Swagger UI
# http://localhost:6000
```

---

## Demo Script (15 minutes)

### 1. Opening - Context (1 minute)

> "Good morning. I've prepared the MedEdge project to demonstrate how I would approach
> B. Braun's medical device connectivity challenge. The project already shows legacy
> dialysis machine integration - today I'll add the Azure IoT Hub patterns."

```
Open: http://localhost:6000
```

> "This is the IoT Hub Simulator - it demonstrates Azure IoT Hub patterns locally,
> without requiring an Azure subscription. Let me walk through the architecture."

---

### 2. Device Registry - B. Braun Machines (2 minutes)

```
Click: GET /iot/devices -> Try it out -> Execute
```

> "I've pre-populated the simulator with B. Braun dialysis machines:
> - **Dialog+**: Legacy model, needs TPM retrofit
> - **Dialog iQ**: Modern model with built-in TPM 2.0
>
> This shows how we'd manage the 300 dialysis centers globally - each device
> has a unique identity, location tags, and firmware tracking."

**Key Point:** Registry as single source of truth for global fleet

---

### 3. Device Twins - Configuration Management (3 minutes)

```
# Get current twin
Click: GET /iot/devices/braun-dialog-plus-001/twin -> Execute

# Explain the structure
# - desired: What we want from cloud
# - reported: What device confirms it has
```

> "Device Twins are the key to remote configuration management. The cloud sends
> **desired properties** (like max blood flow rate), and the device confirms with
> **reported properties** when it's applied. This is critical for medical devices
> where we need to verify configuration changes were actually applied."

```
# Update desired properties
Click: PATCH /iot/devices/braun-dialog-plus-001/twin/desired
Request body:
{
  "maxBloodFlow": 475,
  "pollingInterval": 250,
  "safetyEnabled": true
}
```

> "For B. Braun, this means we can update treatment parameters across 300 centers
> without sending technicians on-site. But because it's medical, we always verify
> the device confirmed the change via the reported properties."

**Key Point:** Bidirectional sync ensures safety-critical configurations are verified

---

### 4. Direct Methods - Remote Intervention (3 minutes)

```
# Read-only: Get diagnostics
Click: POST /iot/devices/braun-dialog-iq-001/methods
Request body:
{
  "methodName": "GetDiagnostics",
  "correlationId": "demo-001"
}
```

> "Direct Methods enable real-time control. Let me get diagnostics from a Dialog iQ
> in Melsungen - we can see uptime, memory usage, CPU, and last error."

```
# Critical operation: Emergency Stop
Click: POST /iot/devices/braun-dialog-plus-001/methods
Request body:
{
  "methodName": "EmergencyStop",
  "payload": {
    "reason": "Patient safety",
    "requestedBy": "Dr. Mueller"
  },
  "correlationId": "demo-emergency"
}
```

> "This is the most important capability for patient safety: Emergency Stop.
> If a clinician in Berlin needs to halt treatment in a Munich center remotely,
> this command does it immediately. The response includes timestamp for audit trail."

**Key Point:** Emergency intervention capability saves lives

---

### 5. TPM 2.0 - Hardware Security (4 minutes)

```
# Show TPM identities
Click: GET /iot/tpm/identities -> Execute
```

> "Security is critical for medical devices. I've implemented TPM 2.0 attestation
> - the same standard Azure DPS uses. Each device has a unique Endorsement Key
> burned into hardware. This prevents device spoofing."

> "Notice the difference: Dialog+ has a **TPM retrofit** (Infineon module),
> while Dialog iQ has **integrated TPM** (B. Braun custom). Both achieve the
> same security standard."

```
# Perform TPM attestation
Click: POST /iot/tpm/attest
Request body:
{
  "deviceId": "braun-dialog-iq-001",
  "endorsementKey": "dGVzdC1la2V5LWZyb20tdHBtLWhhcmR3YXJl",
  "storageRootKey": "dGVzdC1zcmtrZXktZnJvbS10cG0taGFyZHdhcmU=",
  "pcrValues": {
    "0": "bios-measurement-hash",
    "1": "bootloader-measurement-hash",
    "2": "kernel-measurement-hash",
    "7": "secure-boot-policy-hash"
  },
  "nonce": "challenge-12345"
}
```

> "TPM attestation validates three things:
> 1. **Device Identity**: The EK matches the registered device
> 2. **Boot Integrity**: PCR values prove the device booted securely
> 3. **Anti-Replay**: The nonce prevents replay attacks
>
> This is how B. Braun can ensure only authentic dialysis machines connect
> to the cloud - no counterfeit devices."

**Key Point:** Hardware-backed identity prevents device spoofing

---

### 6. Authentication - SAS Tokens & Certificates (2 minutes)

```
# Generate SAS token
Click: POST /iot/auth/tokens/sas
Query params: deviceId=braun-dialog-plus-001&expiry=01:00:00
```

> "Devices authenticate using SAS tokens (Shared Access Signature). This token
> gives the device permission to connect for 1 hour. After that, it must renew."

```
# Validate X.509 certificate
Click: POST /iot/auth/certificates/validate
Request body:
{
  "deviceId": "braun-dialog-plus-001",
  "certificatePem": "-----BEGIN CERTIFICATE-----\n..."
}
```

> "For higher security, we can use X.509 certificates. The simulator validates
> the certificate chain to B. Braun's CA, checks revocation status, and verifies
> the certificate is bound to the TPM. This is medical-grade security."

**Key Point:** Multiple authentication methods for different security tiers

---

### 7. Audit Trail - Compliance (2 minutes)

```
# View authentication audit
Click: GET /iot/audit/auth
Query params: deviceId=braun-dialog-plus-001&count=10
```

> "Every authentication, attestation, and method invocation is logged.
> This is mandatory for healthcare compliance - we need to know who accessed
> which device, when, and what they did."

**Key Point:** Full audit trail meets healthcare regulatory requirements

---

### 8. Architecture Summary (remaining time)

```
Open: http://localhost:6000
Show the full endpoint list
```

> "To summarize, the simulator demonstrates the complete Azure IoT Hub pattern:

> **For B. Braun's Challenge:**
> - **Legacy Retrofit**: Dialog+ gets TPM module + Edge Gateway
> - **Modern Integration**: Dialog iQ has native TPM + Azure SDK
> - **Global Scale**: 300 centers, 5000+ devices
> - **Security**: Hardware-backed identity, TLS 1.3, audit trails
> - **Compliance**: Full logging, verified configuration changes

> **My Approach:**
> 1. Assess each device type (legacy vs modern)
> 2. Design retrofit path for older machines
> 3. Use Azure DPS with TPM for zero-touch provisioning
> 4. Implement Device Twins for remote configuration
> 5. Enable Direct Methods for emergency intervention
> 6. Maintain full audit trail for compliance

> This architecture gives B. Braun connectivity to Azure while maintaining
> the security and reliability required for life-critical medical devices."

---

## Quick Reference - Demo Commands

```bash
# Terminal commands for demo
BASE_URL="http://localhost:6000"

# 1. List all devices
curl $BASE_URL/iot/devices | jq

# 2. Get device twin
curl $BASE_URL/iot/devices/braun-dialog-plus-001/twin | jq

# 3. Update desired properties
curl -X PATCH $BASE_URL/iot/devices/braun-dialog-plus-001/twin/desired \
  -H "Content-Type: application/json" \
  -d '{"maxBloodFlow": 475, "pollingInterval": 250}' | jq

# 4. Invoke direct method
curl -X POST $BASE_URL/iot/devices/braun-dialog-iq-001/methods \
  -H "Content-Type: application/json" \
  -d '{"methodName": "GetDiagnostics"}' | jq

# 5. Emergency stop
curl -X POST $BASE_URL/iot/devices/braun-dialog-plus-001/methods \
  -H "Content-Type: application/json" \
  -d '{"methodName": "EmergencyStop", "payload": {"reason": "Patient safety"}}' | jq

# 6. TPM attestation
curl -X POST $BASE_URL/iot/tpm/attest \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "braun-dialog-iq-001",
    "endorsementKey": "dGVzdC1law==",
    "storageRootKey": "dGVzdC1zcms=",
    "pcrValues": {"0": "bios", "1": "boot", "2": "kernel", "7": "secure"},
    "nonce": "challenge-123"
  }' | jq

# 7. Generate SAS token
curl -X POST "$BASE_URL/iot/auth/tokens/sas?deviceId=braun-dialog-plus-001&expiry=01:00:00" | jq

# 8. Audit trail
curl "$BASE_URL/iot/audit/auth?deviceId=braun-dialog-plus-001&count=10" | jq
```

---

## Potential Questions & Answers

**Q: How would this scale to 5000 devices?**
> "Azure IoT Hub handles millions of devices. The simulator shows the patterns -
> in production, we'd use Azure IoT Hub's auto-scale, Event Hub for telemetry
> ingestion, and Stream Analytics for real-time processing."

**Q: What about network reliability in dialysis centers?**
> "The Edge Gateway implements store-and-forward with offline buffering. Devices
> continue operating locally, and sync to Azure when connectivity resumes. The
> MedEdge project already demonstrates this with Polly resilience patterns."

**Q: How do you handle firmware updates?**
> "Two approaches: For Dialog iQ, we use the FirmwareUpdate direct method which
> pulls from Azure Blob Storage. For Dialog+, the Edge Gateway can stage updates
> locally and apply during maintenance windows - no firmware modification needed."

**Q: What about data sovereignty?**
> "Azure has Germany regions (Frankfurt) which keeps medical data within EU.
> The architecture is cloud-agnostic - we could use AWS or GCP if required,
> but Azure IoT Hub's DPS and TPM support make it the best fit for B. Braun."

**Q: How do you prove the device is authentic?**
> "TPM 2.0 attestation with EK (Endorsement Key) validation. The simulator shows
> PCR (Platform Configuration Register) validation which proves the device booted
> securely. In production, B. Braun's CA would sign device certificates, binding
> identity to the TPM."

---

## Summary Checklist

- [ ] Start docker-compose before interview
- [ ] Test all endpoints work
- [ ] Have Swagger UI open and ready
- [ ] Have terminal ready for curl commands
- [ ] Know the key talking points
- [ ] Prepare for questions about scale, security, and compliance
