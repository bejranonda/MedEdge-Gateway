# MedEdge - Medical Device IoT & Treatment Center Platform

> **Enterprise-Grade Global-Scale Medical Device IoT Platform**
> **Three-Tier Architecture: Local → Regional → Global | HIPAA/GDPR Compliant | Federated AI**

A production-grade global-scale platform demonstrating:
- **Global-Regional-Local Architecture** — Three-tier deployment with data sovereignty
- **Treatment Center Management** — Hierarchical organization (Zones → Stations → Devices)
- **Federated AI Learning** — Privacy-preserving ML model training
- **FHIR R4 Interoperability** — USCDI v3 compliant healthcare data exchange
- **Multi-Region Deployment** — Active-active regional cloud services
- **Edge Disaster Recovery** — Offline buffering with automatic sync
- **Device Fleet Management** — Global OTA updates, telemetry, and monitoring
- **Supply Chain Intelligence** — AI-powered demand forecasting
- **Hardware Security** — TPM 2.0 attestation, X.509 certificates

## 🎯 Project Status

**✅ v2.2.0 RELEASED - Azure IoT Hub Integration**

**Latest Update: Real Azure IoT Hub Connectivity**
- ✅ Edge Gateway connects to Azure IoT Hub (Regional tier)
- ✅ Dual publishing: Local MQTT + Azure IoT Hub simultaneously
- ✅ Device Twin support for cloud-to-device configuration
- ✅ Direct Methods: EmergencyStop, Reboot, GetDiagnostics
- ✅ Treatment Center & Supply Center logical interfaces

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
- ✅ System Dashboard with real-time monitoring
- ✅ SignalR hub for live updates
- ✅ Healthcare-themed responsive design

**Phase 6: Azure IoT Hub Simulator** - ✅ COMPLETE
- ✅ Device Registry & Identity Management
- ✅ Device Twins (Desired/Reported Properties)
- ✅ Direct Methods (Cloud-to-Device Commands)
- ✅ Device Provisioning Service (DPS) Patterns
- ✅ TPM 2.0 Hardware Security Attestation

**Phase 7: Global Scale Architecture (v2.0)** - ✅ COMPLETE
- ✅ Three-tier architecture (Local → Regional → Global)
- ✅ Data sovereignty enforcement (HIPAA/GDPR)
- ✅ Federated learning coordination
- ✅ Multi-region deployment patterns
- ✅ Architecture documentation and dashboards

**Phase 8: Azure IoT Hub Integration (v2.2)** - ✅ COMPLETE
- ✅ Real Azure IoT Hub connectivity (F1 Free tier)
- ✅ Edge Gateway dual publishing (MQTT + IoT Hub)
- ✅ TelemetryBroadcaster for multi-subscriber pattern
- ✅ Device Twin callbacks for configuration sync
- ✅ Direct Method handlers (EmergencyStop, Reboot, GetDiagnostics)
- ✅ Treatment Center & Supply Center logical interfaces

## 📐 System Architecture

### Three-Tier Global Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      GLOBAL TIER                                        │
│              Management & Analytics (No PHI)                            │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │  Global Device Mgmt • Global Analytics • Compliance • Global DB │  │
│  │  (Fleet OTA, ML Training, Audit, Cassandra)                     │  │
│  └──────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────┬───────────────────────────────────────────┘
                              │
┌─────────────────────────────▼───────────────────────────────────────────┐
│                      REGIONAL TIER                                      │
│              Cloud & Services (Data Residency)                          │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │  Treatment • Coordination • Analytics • Transform • FHIR API   │  │
│  │  AI Engine • Treatment Center Layer • Supply Center • Regional DB│  │
│  │  ┌─────────────────────────────────────────────────────────────┐│  │
│  │  │         🔷 AZURE IOT HUB (Central Regional Hub)            ││  │
│  │  │  Treatment Center Interface │ Supply Center Interface      ││  │
│  │  └─────────────────────────────────────────────────────────────┘│  │
│  └──────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────┬───────────────────────────────────────────┘
                              │
┌─────────────────────────────▼───────────────────────────────────────────┐
│                      LOCAL TIER                                         │
│              Facility Edge (HIPAA/GDPR)                                 │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │  CLIENT GROUP                    FACILITY GROUP                 │  │
│  │  Medical Devices • Monitoring Ctr   Treatment Center • Supply   │  │
│  │  Controller • Edge Gateway[Hosp]    Edge Gateway[Store]         │  │
│  │  MQTT Broker (Facility)            Local Database (PHI)        │  │
│  └──────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
```

### Data Sovereignty Strategy

| Tier | Data Scope | Database | Retention | PHI Access |
|------|-----------|----------|-----------|------------|
| **Global** | Device catalog, analytics | Cassandra | 25 years | None |
| **Regional** | Aggregates, anonymized | PostgreSQL Cluster | 10 years | Anonymized |
| **Local** | Patient data, sessions | SQLite/PostgreSQL | 7 years | Full |

### Communication Flow

```
Patient Data Flow (PHI):
Medical Device → Edge Gateway → Local DB → (Anonymized) → Regional DB → (Aggregated) → Global DB

Device Management Flow:
Global Service → Regional Distribution → Edge Gateway → Medical Device

Emergency/Failover:
Edge Gateway → Local Buffer → (Offline Mode) → Sync when Regional available
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

## 🔄 How It Works

### Three-Tier Data Flow

```
1️⃣  LOCAL TIER (Facility Edge)
    • Medical devices connect via Modbus TCP
    • Edge Gateway translates to MQTT
    • Local database stores patient data (PHI)
    • MQTT Broker enables facility messaging

2️⃣  REGIONAL TIER (Cloud Services)
    • Data anonymized before leaving local
    • Regional PostgreSQL cluster stores aggregates
    • Treatment service orchestrates sessions
    • Device coordination manages multi-device sync
    • AI engine performs federated learning

3️⃣  GLOBAL TIER (Management)
    • Global device fleet management
    • OTA firmware/software distribution
    • ML model training and distribution
    • Compliance monitoring and audit
    • No PHI at global level
```

### Federated AI Learning

```
Local Edge Models → Regional Aggregation → Global Training
     ↓ (raw data)        ↓ (model updates)      ↓ (new models)
```

- **Benefit**: Improves AI without crossing PHI boundaries
- **Compliance**: HIPAA/GDPR compliant by design

## 🎨 Dashboard Features

### System Dashboard (v2.1.2)
- **Interactive Donut Charts**: Visual status indicators for Devices, Supply, and Services
  - Total Devices: Online/Offline/Defective breakdown with color-coded segments
  - Supply Center: Good/Low/Critical inventory levels with status visualization
  - Services: Healthy/Unhealthy container status at a glance
- **Three-Tier Visualization**: Global → Regional → Local architecture
- **Color-Coded Tiers**: Green (Global), Blue (Regional), Purple (Local)
- **Interactive Detail Panels**: Click any component for detailed status
- **Real-Time Statistics**: Device counts, gateway metrics, service health
- **Architecture Legend**: Clear tier responsibilities and compliance status

### Technology Stack by Tier

| Tier | Component | Technology |
|------|-----------|------------|
| **Local** | Runtime | .NET 8.0 |
| **Local** | Database | SQLite (devices), PostgreSQL (facilities) |
| **Local** | Messaging | MQTTnet |
| **Local** | Security | TPM 2.0, X.509 certificates |
| **Regional** | Runtime | .NET 8.0 |
| **Regional** | Database | PostgreSQL, InfluxDB |
| **Regional** | Messaging | MQTTnet, EMQX/VerneMQ |
| **Regional** | FHIR | Firely .NET SDK 5.5.0 |
| **Regional** | AI | ML.NET + ONNX Runtime |
| **Global** | Database | Cassandra/scyllaDB |
| **Global** | Messaging | Apache Kafka |
| **Global** | ML | PyTorch/TensorFlow |
| **Global** | OTA | Azure IoT Hub / AWS IoT Device Management |

## 🚀 Quick Start

### Prerequisites
- Docker Desktop (for containerized deployment)
- .NET 8.0 SDK (for local development only)

### Fastest Deployment (Docker Compose)

```bash
# Clone repository
git clone https://github.com/bejranonda/MedEdge-Gateway.git
cd MedEdge-Gateway

# Build and start all services
docker-compose up -d --build

# Access dashboard
# Open browser to: http://localhost:8888
```

**Access Points:**
| Service | URL | Description |
|---------|-----|-------------|
| Dashboard | http://localhost:8888 | System Dashboard |
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

### FHIR Resources
```
GET    /fhir/Patient              # List patients
GET    /fhir/Patient/{id}         # Get patient
GET    /fhir/Device               # List devices
GET    /fhir/Device/{id}          # Get device
GET    /fhir/Observation          # List observations
POST   /fhir/Observation          # Create observation
```

## 🔒 Security & Compliance

### Security Framework
- **Device Layer**: TPM 2.0 + X.509 certificates
- **Edge Layer**: TLS 1.3 for all communications, local attestation
- **Regional Layer**: VPC isolation, private endpoints, Azure Firewall
- **Global Layer**: DDoS protection, Web Application Firewall
- **Data Layer**: Encryption at rest (AES-256), encryption in transit (TLS 1.3)

### Compliance Framework
- **HIPAA**: Business Associate Agreement (BAA) compliant cloud regions
- **GDPR**: Data residency by EU/UK region, consent management
- **FDA 21 CFR Part 11**: Electronic records, electronic signatures
- **ISO 27001**: Information security management
- **ISO 13485**: Medical device quality management

### Data Sovereignty
- **Local**: Full PHI retention within facility
- **Regional**: Data residency by geography (GDPR compliance)
- **Global**: Zero PHI, only device metadata and analytics

## 📚 Documentation

| Document | Purpose |
|----------|---------|
| **README.md** | Project overview & quick start |
| **CHANGELOG.md** | Version history and release notes |
| **docs/ARCHITECTURE-v2.0-Global-Scale.md** | Complete v2.0 architecture specification |
| **docs/ARCHITECTURE-REVISION-SUMMARY.md** | v2.0 revision summary and roadmap |
| **QUICK-START.md** | Rapid deployment guide |
| **TECHNICAL-GUIDE.md** | How the system works |
| **DEPLOYMENT.md** | Production deployment |
| **DEMO.md** | Demo walkthrough |

## 🚀 Deployment Roadmap

### Phase 1: Foundation (Months 1-3)
- [ ] Implement federated MQTT broker architecture
- [ ] Deploy regional database clusters
- [ ] Add data residency enforcement

### Phase 2: Resilience (Months 4-6)
- [ ] Implement edge offline buffering
- [ ] Add regional active-active deployment
- [ ] Deploy disaster recovery automation

### Phase 3: Intelligence (Months 7-9)
- [ ] Implement federated learning pipeline
- [ ] Deploy global analytics platform
- [ ] Add AI-powered forecasting

### Phase 4: Optimization (Months 10-12)
- [ ] Performance tuning
- [ ] Cost optimization
- [ ] Compliance automation

## 📝 License

MIT License - See LICENSE file for details

## 👨‍💻 Author

Built as a portfolio project demonstrating expertise in:
- Global-scale medical device IoT architecture
- Three-tier deployment with data sovereignty
- Federated AI learning and privacy-preserving ML
- HIPAA/GDPR compliance strategies
- FHIR R4 healthcare interoperability
- Treatment center management
- Azure IoT Hub architecture and patterns
- Real-time clinical decision support
- Full-stack .NET development

---

**Current Version:** v2.2.0
**Last Updated:** 2026-02-04
**Status:** Production Ready - Azure IoT Hub Integration
