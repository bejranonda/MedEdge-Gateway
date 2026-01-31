# MedEdge - Medical Device IoT & Treatment Center Platform

> Production-Grade Medical Device Connectivity with Treatment Center Management
> **Hierarchical Treatment Center Architecture with Real-Time Monitoring**

A production-grade implementation demonstrating:
- **Treatment Center Management** — Hierarchical organization (Zones → Stations → Devices)
- **Azure IoT Hub Patterns** — Device Registry, Twins, Direct Methods, DPS, TPM Attestation
- **Industrial IoT Architecture** — Edge gateway bridging medical devices to cloud infrastructure
- **FHIR R4 Interoperability** — Standards-compliant healthcare data exchange
- **Treatment Session Management** — Full lifecycle tracking with phases and outcomes
- **Device Coordination** — Multi-device synchronized operations via MQTT
- **Analytics & Reporting** — Daily metrics, trends, and performance insights
- **AI-Powered Clinical Intelligence** — Real-time anomaly detection and decision support
- **Single-Page Interactive Dashboard** — Blazor WebAssembly with real-time monitoring
- **Hardware Security** — TPM 2.0 attestation, X.509 certificates, SAS tokens

## 🎯 Project Status

**✅ ALL PHASES COMPLETE (Treatment Center Architecture Implemented)**

**Phase 1: FHIR API Foundation** - ✅ COMPLETE
- ✅ Clean Architecture (9 projects, 3-layer design)
- ✅ FHIR REST API endpoints with Swagger
- ✅ EF Core with SQLite database
- ✅ Treatment Center entities (Zones, Stations, Sessions, Devices)

**Phase 2: Treatment Center Architecture** - ✅ COMPLETE
- ✅ 6 Treatment Zones (52 total stations)
- ✅ Station configuration with device slots
- ✅ Treatment session lifecycle management
- ✅ Device coordination via MQTT
- ✅ Analytics and metrics aggregation

**Phase 3: Industrial Edge Pipeline** - ✅ COMPLETE
- ✅ Device Simulator (Modbus TCP: ports 502-504)
- ✅ Edge Gateway (Modbus → MQTT translation)
- ✅ Polly resilience patterns (circuit breaker, retry)
- ✅ Docker multi-stage builds

**Phase 4: Clinical Intelligence** - ✅ COMPLETE
- ✅ Transform Service (MQTT → FHIR Observations)
- ✅ AI Clinical Engine (clinical thresholds)
- ✅ LOINC code mapping (vital signs)
- ✅ Docker Compose orchestration

**Phase 5: Interactive Dashboard** - ✅ COMPLETE
- ✅ Treatment Center view with zone grid layout
- ✅ Real-time station status indicators
- ✅ SignalR hub for live updates
- ✅ Healthcare-themed responsive design

**Phase 6: Azure IoT Hub Simulator** - ✅ COMPLETE
- ✅ Device Registry & Identity Management
- ✅ Device Twins (Desired/Reported Properties)
- ✅ Direct Methods (Cloud-to-Device Commands)
- ✅ Device Provisioning Service (DPS) Patterns
- ✅ TPM 2.0 Hardware Security Attestation

## 📐 System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│ EDGE LAYER                                                      │
│ Medical Device Simulators (Modbus TCP) → Edge Gateway (.NET 8)  │
└─────────────────────┬───────────────────────────────────────────┘
                      │ MQTT over TLS
┌─────────────────────▼───────────────────────────────────────────┐
│ MESSAGING LAYER                                                 │
│ Eclipse Mosquitto MQTT Broker                                  │
└─────────────────────┬───────────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────────┐
│ CLOUD LAYER                                                     │
│ ┌──────────────────────────────────────────────────────────┐   │
│ │  TREATMENT CENTER SERVICES                              │   │
│ │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │   │
│ │  │  Treatment   │  │   Device     │  │   Analytics  │  │   │
│ │  │   Service    │  │ Coordination │  │   Service    │  │   │
│ │  └──────────────┘  └──────────────┘  └──────────────┘  │   │
│ └──────────────────────────────────────────────────────────┘   │
│ Transform Service → AI Engine → FHIR R4 API                    │
│ Azure IoT Hub Simulator (Device Registry, Twins, Methods)     │
└─────────────────────┬───────────────────────────────────────────┘
                      │ SignalR WebSocket
┌─────────────────────▼───────────────────────────────────────────┐
│ PRESENTATION LAYER                                             │
│ Blazor WebAssembly Dashboard (Treatment Center View)           │
└─────────────────────────────────────────────────────────────────┘
```

## 🏥 Treatment Center Architecture

### Hierarchical Organization

```
Treatment Center
├── Zone A (10 stations) - Dialysis
│   ├── Station A-01 (5 device slots)
│   ├── Station A-02 (5 device slots)
│   └── ...
├── Zone B (10 stations) - Dialysis
├── Zone C (10 stations) - Dialysis
├── Zone D (8 stations) - Dialysis
├── Zone E (6 stations) - ICU
└── Zone F (8 stations) - General
```

**Total: 6 Zones, 52 Stations, 260+ Device Slots**

### Treatment Session Lifecycle

```
Scheduled → In-Progress → Phases (Initiation → Treatment → Completion)
                    ↓
               Interrupted/Completed → Outcomes Recorded
```

## 🔄 How It Works

### Treatment Center Data Flow

```
1️⃣  TREATMENT SCHEDULING
    POST /api/treatments/schedule
    ↓ Assign patient to station
    ↓ Create treatment session with prescription parameters

2️⃣  DEVICE COORDINATION
    POST /api/coordination/station/{id}/start-all
    ↓ MQTT commands to all devices at station
    ↓ Synchronized device startup

3️⃣  TREATMENT MONITORING
    Real-time vital signs via MQTT
    ↓ Treatment phases tracked
    ↓ Observations recorded to FHIR

4️⃣  CLINICAL DECISION SUPPORT
    AI Engine monitors measurements
    ↓ Threshold alerts
    ↓ Clinical recommendations

5️⃣  SESSION COMPLETION
    POST /api/treatments/{id}/complete
    ↓ Record outcomes (vitals, complications, patient status)
    ↓ Update station availability

6️⃣  ANALYTICS & REPORTING
    Daily metrics aggregation
    ↓ Station performance trends
    ↓ Area comparison reports
```

## 🎨 Dashboard Features

### Treatment Center View
- **Zone Grid Layout**: Visual representation of all 6 zones
- **Station Status Indicators**: Color-coded dots (available, occupied, maintenance, cleaning, offline)
- **Real-Time Updates**: SignalR pushes status changes instantly
- **Station Detail View**: Patient info, treatment progress, device status

### System Dashboard
- **Interactive Workflow**: Click nodes for detailed status
- **Real-Time Statistics**: Device counts, gateway metrics, service health
- **Live Vitals Preview**: Blood flow, pressures, temperature
- **Auto-Refresh**: 3-second update cycle

## 🚀 Quick Start

### Prerequisites
- Docker Desktop (for containerized deployment)
- .NET 8.0 SDK (for local development only)

### Fastest Deployment (Docker Compose)

```bash
# Clone repository
git clone https://github.com/bejranonda/MedEdge-Gateway.git
cd MedEdge

# Build and start all services
docker-compose up -d --build

# Access dashboard
# Open browser to: http://localhost:8888
```

**Access Points:**
| Service | URL | Description |
|---------|-----|-------------|
| Dashboard | http://localhost:8888 | Treatment Center UI |
| FHIR API | http://localhost:5001/swagger | REST API docs |
| IoT Hub Simulator | http://localhost:8080 | Azure IoT patterns |
| MQTT Broker | localhost:1883 | Message broker |

## 📊 API Endpoints

### Treatment Management
```
POST   /api/treatments/schedule           # Schedule treatment
GET    /api/treatments                    # List all sessions
GET    /api/treatments/active             # Active sessions
PUT    /api/treatments/{id}/start         # Start treatment
PUT    /api/treatments/{id}/phase         # Update phase
PUT    /api/treatments/{id}/interrupt     # Interrupt treatment
POST   /api/treatments/{id}/complete      # Complete treatment
```

### Device Coordination
```
POST   /api/coordination/station/{id}/start-all       # Start all devices
POST   /api/coordination/station/{id}/stop-all        # Stop all devices
POST   /api/coordination/station/{id}/emergency-stop  # Emergency stop
GET    /api/coordination/groups                       # Device groups
POST   /api/coordination/groups                       # Create device group
```

### Analytics
```
GET    /api/analytics/summary              # Latest metrics
GET    /api/analytics/trends               # Treatment trends
GET    /api/analytics/station-performance  # Station performance
GET    /api/analytics/area-comparison      # Area comparison
```

### Treatment Center
```
GET    /api/areas                          # List all zones
GET    /api/areas/{id}                     # Get zone details
GET    /api/stations                       # List all stations
GET    /api/stations/{id}                   # Get station details
GET    /api/stations/available             # Available stations
```

### FHIR Resources
```
GET    /fhir/Patient              # List patients
GET    /fhir/Patient/{id}         # Get patient
GET    /fhir/Device               # List devices
GET    /fhir/Device/{id}          # Get device
GET    /fhir/Observation          # List observations
POST   /fhir/Observation          # Create observation
```

## 🛠 Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| **Runtime** | .NET | 8.0 |
| **API** | ASP.NET Core | 8.0 |
| **FHIR SDK** | Firely .NET SDK | 5.5.0 |
| **Database** | SQLite | - |
| **ORM** | Entity Framework Core | 8.0 |
| **Dashboard** | Blazor WebAssembly | .NET 8 |
| **UI Framework** | MudBlazor | Latest |
| **Real-time** | SignalR | .NET 8 |
| **Messaging** | Eclipse Mosquitto MQTT | 2.0 |

## 📚 Documentation

| Document | Purpose |
|----------|---------|
| **README.md** | Project overview & quick start |
| **QUICK-START.md** | Rapid deployment guide |
| **TECHNICAL-GUIDE.md** | How the system works |
| **DEPLOYMENT.md** | Production deployment |
| **DEMO.md** | Demo walkthrough |

## 🔒 Security

- TLS 1.3 for all communications
- TPM 2.0 hardware attestation for device identity
- X.509 certificate validation
- SAS token authentication
- Audit logging for all operations
- Input validation on all API endpoints
- Environment-based configuration

## 📝 License

MIT License - See LICENSE file for details

## 👨‍💻 Author

Built as a portfolio project demonstrating expertise in:
- Treatment center management architecture
- Azure IoT Hub architecture and patterns
- FHIR R4 healthcare interoperability
- Industrial IoT architecture
- Real-time clinical decision support
- Full-stack .NET development

---

**Current Version:** v1.4.0
**Last Updated:** 2026-01-31
**Status:** Production Ready - Treatment Center Architecture Implemented
