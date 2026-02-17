# MedEdge Gateway - Live Demo Walkthrough

> **⚠️ DEMONSTRATION ONLY**
>
> This walkthrough demonstrates the capabilities of the MedEdge platform as a **technology showcase**.
> All device data is simulated. This system is not intended for clinical or production use.

---

**Duration:** 10 minutes
**Audience:** Technical Evaluators, IT Professionals, Stakeholders
**Prerequisites:** Docker Compose deployed and running

---

## Pre-Demo Setup (5 minutes before)

### 1. Verify Deployment

```bash
# Check all services running
docker-compose ps

# Expected: All 7 containers with status "Up"

# Verify API is responsive
curl http://localhost:5001/health
# Response: {"status":"healthy"}
```

### 2. Open Required Windows

**Window 1: Dashboard**
```bash
open http://localhost:5000  # macOS
start http://localhost:5000  # Windows
xdg-open http://localhost:5000  # Linux
```

**Window 2: API Swagger Documentation**
```bash
open http://localhost:5001/swagger
```

**Window 3: MQTT Monitor (Optional)**
```bash
docker run -it --rm efrecon/mqtt-client sub -h localhost -t "bbraun/dialysis/#"
```

---

## Demo Timeline

### [0:00 - 1:00] Welcome & Overview

**Presenter Says:**
> "Welcome to MedEdge Gateway - a next-generation clinical connectivity platform for medical devices. This demo showcases real-time monitoring, AI-powered anomaly detection, and clinical decision support."

**Show on Screen:**
- Dashboard home page
- Four metric cards visible
- System status "Operational" 🟢

**Narrative Points:**
- Real-time telemetry from 3 dialysis machines
- FHIR R4 interoperability standard
- Hybrid AI detection (Statistical + LLM)
- Enterprise-ready architecture

---

### [1:00 - 3:30] Fleet Status Overview

**Presenter Says:**
> "First, let's look at the fleet status. We're monitoring three Dialog+ and Dialog iQ dialysis machines. Each device shows its current operational state."

**Actions:**
1. Click on "Fleet Status" in navigation
2. Show 3 device cards:
   - Device-001 (Dialog+) 🟢 Online
   - Device-002 (Dialog iQ) 🟢 Online
   - Device-003 (Dialog+) 🟢 Online

**Point Out:**
- Green status indicators (all online)
- Current patient assignments
- Last telemetry timestamp (recent)
- Active alarm count (none currently)

**Narrative:**
> "All three devices are currently online and operating normally. The platform automatically polls each device's Modbus TCP registers every 500 milliseconds, capturing vital signs and equipment status."

---

### [3:30 - 6:00] Live Vital Signs Monitoring

**Presenter Says:**
> "Now let's dive into the real-time vital signs monitoring. Here we can see live telemetry streaming from the devices via SignalR WebSocket."

**Actions:**
1. Click on "Live Vitals" in navigation
2. Select "Device-001" from dropdown
3. Click "Connect"

**Show:**
```
Real-time dashboard displays:
┌─────────────────────────────┐
│ Blood Flow Rate: 320 mL/min  │ ✓ Normal (Target: 200-400)
├─────────────────────────────┤
│ Arterial Pressure: 120 mmHg  │ ✓ Normal (Target: 50-200)
├─────────────────────────────┤
│ Venous Pressure: 80 mmHg     │ ✓ Normal (Target: 50-200)
├─────────────────────────────┤
│ Temperature: 36.5°C          │ ✓ Normal (Target: 35-38°C)
├─────────────────────────────┤
│ Conductivity: 14.0 mS/cm     │ ✓ Normal (Target: 13.5-14.5)
├─────────────────────────────┤
│ Treatment Time: 0h 45m 20s   │ Progress: 18.8%
└─────────────────────────────┘
```

**Narrative Points:**
> "Notice the color-coded indicators. Green means within normal range. Each vital includes the target range for reference. The system is updating every 500 milliseconds - watch how smoothly the values change."

**Observation:**
- Charts show real-time waveforms
- Values update live via SignalR
- All metrics in green (normal operation)

---

### [6:00 - 8:30] AI Anomaly Detection & Clinical Alerts

**Presenter Says:**
> "Now for the interesting part - let's simulate a critical clinical event and see how the AI responds."

**Actions:**
1. Open Swagger in second window (http://localhost:5001/swagger)
2. Find endpoint: `POST /api/devices/{deviceId}/anomaly/hypotension`
3. Enter DeviceId: "Device-001"
4. Execute request

**What Happens:**
- Within 1-2 seconds, vital sign for Arterial Pressure drops
- **RED ALERT** appears prominently at top of Live Vitals page

**Alert Display:**
```
⚠️ CLINICAL ALERTS
├─ SEVERITY: CRITICAL
├─ FINDING: Hypotension detected - Arterial Pressure < 80 mmHg
├─ RECOMMENDATION:
│   1. Verify arterial needle position
│   2. Check access pressure limits
│   3. Evaluate volume status
│   4. Consider reducing ultrafiltration
└─ Detected: 2026-01-16 12:34:56 UTC
```

**Narrative:**
> "The AI detected hypotension - a critical condition where blood pressure drops too low. Within milliseconds, the system:
> 1. Statistical detector identified the threshold violation
> 2. AI engine analyzed the clinical context
> 3. Generated evidence-based recommendations
> 4. Alerted all connected clinicians

This is exactly the kind of early warning that can prevent serious complications."

**Point Out:**
- Response time (< 2 seconds)
- Clinical accuracy of threshold
- Actionable recommendations
- Timestamp for audit trail

---

### [8:30 - 9:00] Data Export & FHIR Inspector

**Presenter Says:**
> "Let's look at how we can access the underlying clinical data. The FHIR Inspector lets us query and export observations in the industry standard FHIR format."

**Actions:**
1. Click on "FHIR Inspector" in navigation
2. Select Resource Type: "Observation"
3. Click "Search"
4. Show results table with recent observations
5. Click "View" on one row to expand JSON

**Show JSON:**
```json
{
  "resourceType": "Observation",
  "id": "obs-ap-001",
  "status": "final",
  "code": {
    "coding": [{
      "system": "http://loinc.org",
      "code": "75992-9",
      "display": "Arterial Pressure"
    }]
  },
  "subject": { "reference": "Patient/P001" },
  "device": { "reference": "Device/Device-001" },
  "effectiveDateTime": "2026-01-16T12:34:56Z",
  "value": {
    "value": 75,
    "unit": "mmHg",
    "system": "http://unitsofmeasure.org",
    "code": "mm[Hg]"
  }
}
```

**Narrative:**
> "This is FHIR R4 format - the healthcare industry standard for interoperability. All data can be exported as FHIR Bundles for integration with EHR systems, research databases, or regulatory compliance. This enables seamless integration with any FHIR-compliant system."

---

### [9:00 - 9:30] Recovery & System Resilience

**Presenter Says:**
> "Now let's show the emergency stop capability - critical for patient safety."

**Actions:**
1. Go back to "Fleet Status"
2. Find Device-001 card
3. Click "Emergency Stop" button
4. Confirm in dialog

**What Happens:**
- Device card changes to RED 🔴
- Status shows "Offline"
- All telemetry stops

**Narrative:**
> "Emergency stop is immediately broadcast to the device via MQTT. In a real scenario, this would halt the dialysis treatment. The command is logged for compliance and the event is timestamped for the medical record."

**Show Recovery:**
- System is ready for restart
- Device can be brought back online
- All data is persisted

---

### [9:30 - 10:00] Wrap-up & Architecture Overview

**Presenter Says:**
> "Let's take a final look at the architecture that makes all this possible."

**Show Architecture Diagram:**
```
┌─────────────────────────────────────────────────────────┐
│ CLINICAL DASHBOARD (Blazor WASM)                        │
│ ↓ SignalR WebSocket                                     │
├─────────────────────────────────────────────────────────┤
│ FHIR API (ASP.NET Core 8.0)                             │
│ ↓ REST API | ↑ MQTT | ↓ Database                        │
├─────────────────────────────────────────────────────────┤
│ Transform Service → FHIR Mapper → SQLite Database       │
│ AI Engine → Statistical Detection → Clinical Alerts    │
├─────────────────────────────────────────────────────────┤
│ MQTT Broker (Mosquitto)                                 │
│ ↓                                                        │
│ Edge Gateway (Modbus ↔ MQTT)                           │
│ ↓                                                        │
│ Device Simulator (Modbus TCP Servers)                   │
└─────────────────────────────────────────────────────────┘
```

**Key Talking Points:**

1. **Edge Computing**
   - Devices → Modbus TCP (industrial protocol)
   - Gateway → Protocol translation
   - Resilience patterns (Polly)

2. **Real-Time Integration**
   - MQTT pub/sub architecture
   - SignalR WebSocket for dashboard
   - Sub-second latency

3. **Clinical Intelligence**
   - FHIR R4 compliance
   - LOINC codes for semantics
   - AI-powered recommendations

4. **Enterprise Ready**
   - Docker containerization
   - Kubernetes-ready
   - Complete audit trail
   - TLS/SSL support

**Final Statement:**
> "MedEdge Gateway demonstrates how modern cloud-native architecture can bring enterprise integration and AI-powered clinical intelligence to medical device connectivity. It bridges the gap between legacy industrial equipment and cutting-edge clinical analytics platforms."

---

## Post-Demo Q&A

### Common Questions & Answers

**Q: How real-time is the data?**
> A: 500-millisecond polling interval from devices, with SignalR WebSocket updates pushing to the dashboard in real-time. Full round-trip is typically < 1 second from device to clinical alert.

**Q: What if the MQTT broker goes down?**
> A: The Edge Gateway has offline buffering - it stores messages locally and replays them once the broker comes back online. No data loss.

**Q: Can it integrate with our EHR?**
> A: Yes - all data is FHIR R4 compliant. The Bulk Data API endpoint allows export to any FHIR-compatible system. We also support HL7 v2 translators.

**Q: What about security?**
> A: All components support TLS 1.2+. Authentication can use OAuth 2.0 or SMART on FHIR. Audit logging captures all clinical data modifications.

**Q: How many devices can it scale to?**
> A: Docker Compose is for demo/small deployments. On Kubernetes with load balancing, we can scale to hundreds of devices per cluster, thousands globally with multi-region deployment.

**Q: What's the infrastructure cost?**
> A: Demo runs on 4GB RAM. Production typically needs 8-16GB. Cloud deployment on AKS/EKS: ~$200-500/month for modest load.

---

## Technical Deep Dive (Optional)

If audience wants technical details, show:

### 1. Device Polling Sequence

```bash
# Simulate real-time polling
Watch MQTT broker:
docker logs -f medEdge-mosquitto | grep "bbraun/dialysis"

# Shows: bbraun/dialysis/Device-001/telemetry
# Output: {"deviceId":"Device-001","timestamp":"...","measurements":{...}}
```

### 2. FHIR Transformation Pipeline

```bash
# Query FHIR data
curl http://localhost:5001/fhir/Observation?patient=P001 | jq '.'

# Shows complete FHIR Bundle with all observations
```

### 3. AI Detection Logic

Show code from `StatisticalAnomalyDetector.cs`:
```csharp
if (bloodFlow < 150) return RiskLevel.Critical;
if (arterialPressure < 80) return RiskLevel.Critical;  // ← Hypotension
if (temperature > 38.5) return RiskLevel.Warning;
```

### 4. SignalR Hub Communication

Show TelemetryHub group broadcasting:
```csharp
await Clients.Group(deviceId).SendAsync("VitalSignUpdate", observation);
```

---

## Demo Troubleshooting

### Issue: Dashboard shows "Disconnected"

**Solution:**
```bash
# Verify API is running
curl http://localhost:5001/health

# Restart API
docker-compose restart fhir-api
```

### Issue: No telemetry updates

**Solution:**
```bash
# Check simulator
docker-compose logs simulator

# Restart all services
docker-compose restart
```

### Issue: Alerts don't appear

**Solution:**
```bash
# Check transform service
docker-compose logs transform-service

# Verify AI engine
docker-compose logs ai-engine
```

---

## Follow-Up Resources

**Provide Links:**
1. GitHub Repository: https://github.com/bejranonda/MedEdge-Gateway
2. Full Deployment Guide: [DEPLOYMENT.md](DEPLOYMENT.md)
3. Architecture Details: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
4. FHIR Mapping: [docs/FHIR-MAPPING.md](docs/FHIR-MAPPING.md)
5. Implementation Status: [PROJECT-STATUS.md](PROJECT-STATUS.md)

**Contact Information:**
- Email: [Your Contact]
- Issues: [GitHub Issues]
- Documentation: See `docs/` directory

---

## Demo Feedback Form

Post-demo survey questions:
1. How clear was the clinical workflow?
2. How realistic are the vital signs?
3. Would you use this for patient monitoring?
4. What features would you add?
5. Integration with your EHR?

---

**Demo Version:** 1.0
**Last Updated:** 2026-01-16
**Status:** Technology Demonstration
**Purpose:** Portfolio Demo - Showcasing Medical IoT Concepts
