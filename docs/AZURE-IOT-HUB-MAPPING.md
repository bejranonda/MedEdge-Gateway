# Azure IoT Hub Mapping - MedEdge Simulator

## Overview

This document maps the **MedEdge IoT Hub Simulator** to **Azure IoT Hub** concepts and APIs. The simulator demonstrates Azure IoT Hub patterns for B. Braun medical device connectivity without requiring an Azure subscription.

## Architecture Mapping

| MedEdge Simulator | Azure IoT Hub | Purpose for B. Braun |
|-------------------|---------------|---------------------|
| `/iot/devices` | Device Registry API | Identity management for Dialog+/Dialog iQ machines |
| `/iot/devices/{id}/twin` | Device Twin API | Configuration synchronization |
| `/iot/devices/{id}/methods` | Direct Methods API | Cloud-to-device commands (EmergencyStop, Reboot) |
| `/iot/provisioning` | Device Provisioning Service (DPS) | Zero-touch device provisioning |
| `/iot/auth/tokens/sas` | SAS Token generation | Device authentication |
| `/iot/tpm/attest` | DPS TPM Attestation | Hardware-backed identity |

---

## 1. Device Registry

### Azure IoT Hub
```
GET https://{hub}.azure-devices.net/devices?api-version=2020-09-01
Authorization: SharedAccessSignature sr={hub}&sig={sig}&se={expiry}&skn={policy}
```

### MedEdge Simulator
```bash
curl http://localhost:6000/iot/devices
```

**Sample Response:**
```json
{
  "deviceId": "braun-dialog-plus-001",
  "status": "enabled",
  "connectionState": "connected",
  "tags": {
    "manufacturer": "B. Braun",
    "model": "Dialog+",
    "type": "dialysis",
    "location": "Melsungen-Center-01",
    "firmware": "4.2.1"
  }
}
```

---

## 2. Device Twins

### Azure IoT Hub
```
GET https://{hub}.azure-devices.net/twins/{deviceId}?api-version=2020-09-01
```

### MedEdge Simulator
```bash
curl http://localhost:6000/iot/devices/braun-dialog-plus-001/twin
```

**Twin Structure:**
```json
{
  "deviceId": "braun-dialog-plus-001",
  "etag": "AAAAAAAAAAE=",
  "properties": {
    "desired": {
      "pollingInterval": 500,
      "maxBloodFlow": 450,
      "safetyEnabled": true,
      "$version": 3
    },
    "reported": {
      "lastMaintenance": "2025-12-15",
      "uptimeHours": 12450,
      "pollingInterval": 500,
      "$version": 2
    }
  }
}
```

### Twin Synchronization Pattern

```
┌─────────────────┐         ┌──────────────────┐         ┌─────────────────┐
│   Cloud (Desired)│  PATCH  │ Azure IoT Hub    │  MQTT  │  Device         │
│  {              │ ───────>│  Device Twin     │ <────── │  (Reported)     │
│   "targetBFR":  │         │  {              │         │  {              │
│    450          │         │   desired: {...}│         │   currentBFR:   │
│  }              │         │   reported: {...}│         │    425          │
│ }               │         │  }              │         │  }              │
└─────────────────┘         └──────────────────┘         └─────────────────┘
       │                            │                            ▲
       │                            │                            │
       └───────── Desired Property Update ──────────────────────┘
                    (Device confirms & applies)
```

---

## 3. Direct Methods (Cloud-to-Device Commands)

### Azure IoT Hub
```
POST https://{hub}.azure-devices.net/twins/{deviceId}/methods?api-version=2020-09-01
{
  "methodName": "EmergencyStop",
  "payload": { "reason": "Patient safety" },
  "timeoutInSeconds": 30
}
```

### MedEdge Simulator
```bash
curl -X POST http://localhost:6000/iot/devices/braun-dialog-plus-001/methods \
  -H "Content-Type: application/json" \
  -d '{
    "methodName": "EmergencyStop",
    "payload": { "reason": "Patient safety" },
    "correlationId": "req-123"
  }'
```

**Supported Methods for B. Braun Devices:**

| Method | Parameters | Response | Use Case |
|--------|------------|----------|----------|
| `EmergencyStop` | `{ reason: string }` | `{ result: "stopped", timestamp: "..." }` | Immediate treatment halt |
| `Reboot` | None | `{ result: "rebooting", estimatedDowntime: "00:02:00" }` | Remote device restart |
| `GetDiagnostics` | None | `{ uptime, memoryUsage, cpuUsage, lastError }` | Troubleshooting |
| `FirmwareUpdate` | `{ version: string }` | `{ result: "accepted", version: "..." }` | OTA firmware update |
| `SetParameters` | `{ bloodFlow: number, duration: number }` | `{ result: "applied" }` | Treatment configuration |

---

## 4. Device Provisioning Service (DPS)

### Azure IoT Hub with TPM
```
POST https://global.azure-devices-provisioning.net/enrollments
{
  "registrationId": "braun-dialog-iq",
  "tpm": {
    "endorsementKey": "<EK from TPM hardware>",
    "storageRootKey": "<SRK from TPM>"
  }
}
```

### MedEdge Simulator
```bash
curl -X POST http://localhost:6000/iot/provisioning \
  -H "Content-Type: application/json" \
  -d '{
    "registrationId": "braun-dialog-iq",
    "templateId": "dialog-iq-v2",
    "payload": {
      "manufacturer": "B. Braun",
      "location": "Melsungen-Center-02"
    }
  }'
```

**Response:**
```json
{
  "deviceId": "braun-dialog-iq-a3f7c12d",
  "assignedHub": "mededge-iot-hub-simulated",
  "deviceKey": "dGVzdC1rZXk=",
  "assignedAt": "2025-01-23T10:30:00Z",
  "status": "assigned"
}
```

---

## 5. TPM 2.0 Attestation

### Azure DPS TPM Attestation Flow

```
┌──────────────┐        ┌──────────────┐        ┌──────────────┐
│  Medical     │        │   Azure DPS  │        │   B. Braun   │
│  Device      │        │   Service    │        │   CA         │
│  (Dialog+)   │        │              │        │              │
└──────┬───────┘        └──────┬───────┘        └──────┬───────┘
       │                       │                       │
       │  1. Get EK from TPM   │                       │
       ├──────────────────────>│                       │
       │                       │                       │
       │  2. Send EK + nonce   │                       │
       ├──────────────────────>│                       │
       │                       │                       │
       │                       │  3. Validate EK cert  │
       │                       ├─────────────────────>│
       │                       │                       │
       │                       │  4. Return validation │
       │                       │<─────────────────────┤
       │                       │                       │
       │  5. Return IoT Hub    │                       │
       │     assignment        │                       │
       │<──────────────────────┤                       │
       │                       │                       │
```

### MedEdge Simulator
```bash
curl -X POST http://localhost:6000/iot/tpm/attest \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "braun-dialog-iq-001",
    "endorsementKey": "base64-encoded-EK-from-TPM-hardware",
    "storageRootKey": "base64-encoded-SRK-from-TPM-hardware",
    "pcrValues": {
      "0": "bios-measurement-hash",
      "1": "bootloader-measurement-hash",
      "2": "kernel-measurement-hash",
      "7": "secure-boot-policy-hash"
    },
    "nonce": "random-challenge"
  }'
```

**Response:**
```json
{
  "deviceId": "braun-dialog-iq-001",
  "isValid": true,
  "attestationCertificate": "-----BEGIN CERTIFICATE-----\n...",
  "validationDetails": {
    "isValid": true,
    "validatedMeasurements": {
      "PCR_0": "BIOS measurement: valid",
      "PCR_1": "Bootloader measurement: valid",
      "PCR_2": "OS kernel measurement: valid",
      "PCR_7": "Secure boot policy: valid"
    }
  },
  "timestamp": "2025-01-23T10:30:00Z"
}
```

---

## 6. Authentication Methods

### SAS Token Authentication

**Azure IoT Hub SAS Token Format:**
```
SharedAccessSignature sr={hub}&sig={signature}&se={expiry}&skn={keyName}
```

**MedEdge Simulator:**
```bash
curl -X POST http://localhost:6000/iot/auth/tokens/sas?deviceId=braun-dialog-plus-001&expiry=01:00:00
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "deviceId": "braun-dialog-plus-001",
  "expiresAt": "2025-01-23T11:30:00Z",
  "tokenType": "Sas"
}
```

### X.509 Certificate Authentication

**Azure IoT Hub Certificate Enrollment:**
```bash
# Upload certificate to Azure IoT Hub
az iot hub device-identity create \
  --hub-name {hub} \
  --device-id {device-id} \
  --auth-method x509_certificate \
  --primary-thumbprint {thumbprint}
```

**MedEdge Simulator:**
```bash
curl -X POST http://localhost:6000/iot/auth/certificates/validate \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "braun-dialog-plus-001",
    "certificatePem": "-----BEGIN CERTIFICATE-----\n..."
  }'
```

**Response:**
```json
{
  "isValid": true,
  "deviceId": "braun-dialog-plus-001",
  "issuer": "CN=B. Braun Avitum Device CA, O=B. Braun, C=DE",
  "subject": "CN=braun-dialog-plus-001, O=B. Braun Avitum",
  "serialNumber": "A3F7C12D",
  "validFrom": "2024-12-24T00:00:00Z",
  "validTo": "2026-01-23T00:00:00Z",
  "validationChecks": {
    "ChainTrust": true,
    "Revocation": true,
    "Expiration": true,
    "DeviceMatch": true,
    "TpmBound": true
  }
}
```

---

## 7. Telemetry Ingestion

### Azure IoT Hub
```
POST https://{hub}.azure-devices.net/devices/{deviceId}/messages/events?api-version=2020-09-01
Authorization: SharedAccessSignature ...
Content-Type: application/json

{
  "bloodFlowRate": 350,
  "arterialPressure": 120,
  "venousPressure": 85,
  "temperature": 37.1,
  "timestamp": "2025-01-23T10:30:00Z"
}
```

### MedEdge Simulator
```bash
curl -X POST http://localhost:6000/iot/devices/braun-dialog-plus-001/telemetry \
  -H "Content-Type: application/json" \
  -d '{
    "bloodFlowRate": 350,
    "arterialPressure": 120,
    "venousPressure": 85,
    "temperature": 37.1,
    "timestamp": "2025-01-23T10:30:00Z"
  }'
```

---

## 8. Security Architecture

### Azure IoT Hub Security Features

| Feature | Azure IoT Hub | MedEdge Simulator |
|---------|---------------|-------------------|
| **TLS Encryption** | TLS 1.2+ | TLS 1.3 |
| **SAS Tokens** | SharedAccessSignature | JWT-based SAS |
| **X.509 Certificates** | CA-validated | Simulated CA validation |
| **TPM Attestation** | DPS TPM enrollment | Full TPM 2.0 simulation |
| **Device Twins** | Encrypted at rest | In-memory |
| **Audit Logging** | Azure Monitor | In-memory audit trail |

### Security Layer Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        B. Braun Cloud (Azure)                   │
├─────────────────────────────────────────────────────────────────┤
│  Azure IoT Hub                                                  │
│  ├─ Device Registry (Identity)                                  │
│  ├─ Device Twins (Configuration)                                │
│  ├─ Direct Methods (Commands)                                   │
│  └─ Event Hub (Telemetry) ────────> Azure Data Explorer         │
└────────────────┬────────────────────────────────────────────────┘
                 │ TLS 1.3 + Mutual Auth
┌────────────────▼────────────────────────────────────────────────┐
│                      Security Layer                             │
│  ├─ SAS Token Validation (HMAC-SHA256)                         │
│  ├─ X.509 Certificate Chain Validation                         │
│  └─ TPM 2.0 Hardware Attestation                               │
└────────────────┬────────────────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────────────────┐
│                   Device Gateway (Edge)                         │
│  ├─ Dialysis Machine (Dialog+)                                  │
│  ├─ Dialysis Machine (Dialog iQ)                                │
│  └─ Water Filtration System                                    │
└─────────────────────────────────────────────────────────────────┘
```

---

## 9. API Reference Summary

### Base URL
```
MedEdge Simulator: http://localhost:6000
Azure IoT Hub: https://{hub-name}.azure-devices.net
```

### Endpoint Mapping

| MedEdge Endpoint | Azure IoT Hub Endpoint | Method |
|------------------|------------------------|--------|
| `GET /iot/devices` | `/devices?api-version=2020-09-01` | List devices |
| `GET /iot/devices/{id}` | `/devices/{id}?api-version=2020-09-01` | Get device |
| `POST /iot/devices` | `/devices/{id}?api-version=2020-09-01` (PUT) | Create device |
| `GET /iot/devices/{id}/twin` | `/twins/{id}?api-version=2020-09-01` | Get twin |
| `PATCH /iot/devices/{id}/twin/desired` | `/twins/{id}?api-version=2020-09-01` (PATCH) | Update desired |
| `POST /iot/devices/{id}/methods` | `/twins/{id}/methods?api-version=2020-09-01` | Invoke method |
| `POST /iot/devices/{id}/telemetry` | `/devices/{id}/messages/events` | Send telemetry |
| `POST /iot/provisioning` | DPS enrollment API | Provision device |
| `POST /iot/tpm/attest` | DPS TPM attestation | TPM attestation |

---

## 10. Interview Demo Script

### Demo Flow (15 minutes)

**1. Device Registry (2 min)**
```bash
# Show pre-loaded B. Braun devices
curl http://localhost:6000/iot/devices | jq
```
> "I've pre-populated the simulator with B. Braun Dialog+ and Dialog iQ dialysis machines.
> This demonstrates how we'd register 300 dialysis centers worldwide."

**2. Device Twins (3 min)**
```bash
# Show current twin configuration
curl http://localhost:6000/iot/devices/braun-dialog-plus-001/twin | jq

# Update desired properties (simulate cloud configuration change)
curl -X PATCH http://localhost:6000/iot/devices/braun-dialog-plus-001/twin/desired \
  -H "Content-Type: application/json" \
  -d '{"maxBloodFlow": 475, "pollingInterval": 250}'
```
> "Device Twins allow bidirectional synchronization. The cloud sends desired configuration,
> and the device confirms when applied - critical for safety-critical medical devices."

**3. Direct Methods (3 min)**
```bash
# Invoke GetDiagnostics (read-only operation)
curl -X POST http://localhost:6000/iot/devices/braun-dialog-iq-001/methods \
  -H "Content-Type: application/json" \
  -d '{"methodName": "GetDiagnostics"}' | jq

# Invoke EmergencyStop (critical safety operation)
curl -X POST http://localhost:6000/iot/devices/braun-dialog-plus-001/methods \
  -H "Content-Type: application/json" \
  -d '{"methodName": "EmergencyStop", "payload": {"reason": "Patient safety"}}' | jq
```
> "Direct Methods enable real-time control. For B. Braun, this means emergency intervention
> capability from any location - a key requirement for remote dialysis center management."

**4. TPM Attestation (4 min)**
```bash
# Show TPM identities
curl http://localhost:6000/iot/tpm/identities | jq

# Perform TPM attestation
curl -X POST http://localhost:6000/iot/tpm/attest \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "braun-dialog-iq-001",
    "endorsementKey": "dGVzdC1lay==",
    "storageRootKey": "dGVzdC1zcms==",
    "pcrValues": {
      "0": "bios-hash",
      "1": "bootloader-hash",
      "2": "kernel-hash",
      "7": "secure-boot-hash"
    },
    "nonce": "random-challenge"
  }' | jq
```
> "TPM 2.0 attestation ensures hardware-backed identity. For B. Braun, this prevents
> device spoofing and ensures only authentic medical devices can connect to the cloud."

**5. Authentication & Audit (3 min)**
```bash
# Generate SAS token
curl -X POST "http://localhost:6000/iot/auth/tokens/sas?deviceId=braun-dialog-plus-001&expiry=01:00:00" | jq

# View audit trail
curl "http://localhost:6000/iot/audit/auth?deviceId=braun-dialog-plus-001&count=10" | jq
```
> "All authentication is logged for compliance. Medical device regulations require
> full audit trails for any device configuration changes or emergency interventions."

---

## 11. Key Takeaways for B. Braun Interview

### Demonstrated Knowledge

✅ **Azure IoT Hub Architecture**
- Device Registry, Twins, Methods, DPS patterns
- No Azure subscription required to demonstrate

✅ **Medical Device Security**
- TPM 2.0 hardware attestation
- X.509 certificate validation
- SAS token authentication

✅ **Legacy System Integration**
- Dialog+ (older devices) retrofit pattern
- Dialog iQ (modern devices) native TPM support
- Protocol translation (Modbus → FHIR → Azure)

✅ **Healthcare Compliance**
- Full audit trail for all operations
- Device identity verification
- Secure communication (TLS 1.3)

### Talking Points

**"For B. Braun's 300 dialysis centers:"**

> "I would use Azure IoT Hub's Device Provisioning Service with TPM attestation.
> Each dialysis machine gets a hardware-backed identity, preventing device spoofing.
> Device Twins would enable remote configuration management without on-site visits.
> And Direct Methods provide emergency intervention capability from Melsungen HQ."

**"For the legacy Dialog+ machines:"**

> "The retrofit pattern uses the Edge Gateway to translate Modbus to Azure IoT Hub
> protocols. We add a TPM module for hardware security, then use DPS for zero-touch
> provisioning. This extends Azure IoT capabilities to older equipment without
> firmware modifications."

**"For security and compliance:"**

> "All communications use TLS 1.3 with mutual authentication. TPM attestation
> ensures device identity, X.509 certificates validate against B. Braun's CA,
> and every operation is logged to Azure Monitor for healthcare compliance auditing."
