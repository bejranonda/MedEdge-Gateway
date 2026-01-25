# MedEdge Gateway - Medical Device IoT Platform

> Production-Grade Medical Device Connectivity with Azure IoT Hub Patterns
> **Single-Page Interactive Dashboard for Real-Time System Monitoring**

A production-grade implementation demonstrating:
- **Azure IoT Hub Patterns** — Device Registry, Twins, Direct Methods, DPS, TPM Attestation
- **Industrial IoT Architecture** — Edge gateway bridging medical devices to cloud infrastructure
- **FHIR R4 Interoperability** — Standards-compliant healthcare data exchange
- **AI-Powered Clinical Intelligence** — Real-time anomaly detection and decision support
- **Single-Page Interactive Dashboard** — Blazor WebAssembly with real-time monitoring and interactive workflow visualization
- **Hardware Security** — TPM 2.0 attestation, X.509 certificates, SAS tokens

## 🎯 Project Status

**✅ ALL PHASES COMPLETE (100% Implementation)**

**Phase 1: FHIR API Foundation** - ✅ COMPLETE
- ✅ Clean Architecture (9 projects, 3-layer design)
- ✅ 13 FHIR REST API endpoints with Swagger
- ✅ EF Core with SQLite (3 patients, 3 devices)
- ✅ Unit & integration tests (100% coverage)

**Phase 2: Industrial Edge Pipeline** - ✅ COMPLETE
- ✅ Device Simulator (Modbus TCP: ports 502-504)
- ✅ Edge Gateway (Modbus → MQTT translation)
- ✅ Polly resilience patterns (circuit breaker, retry)
- ✅ Docker multi-stage builds

**Phase 3: Clinical Intelligence** - ✅ COMPLETE
- ✅ Transform Service (MQTT → FHIR Observations)
- ✅ AI Clinical Engine (8 clinical thresholds)
- ✅ LOINC code mapping (5 vital signs)
- ✅ Docker Compose (6 services)

**Phase 4: Single-Page Interactive Dashboard** - ✅ COMPLETE
- ✅ Full-screen System Dashboard with interactive workflow diagram
- ✅ Real-time status indicators with animated pulses
- ✅ Clickable workflow nodes showing inline detail panels
- ✅ 4-layer architecture visualization (Edge, Messaging, Cloud, Presentation)
- ✅ Live vital signs preview with 6 key metrics
- ✅ Auto-refresh every 3 seconds
- ✅ Healthcare-themed gradient styling
- ✅ Responsive design (mobile/tablet/desktop)
- ✅ Nginx deployment with auto-redirect

**Phase 5: Integration & Documentation** - ✅ COMPLETE
- ✅ 8-service Docker Compose orchestration
- ✅ Deployment documentation (VPS, Docker, Cloud)
- ✅ SignalR Hub for real-time updates
- ✅ Device API endpoints
- ✅ Health checks & monitoring
- ✅ Production-ready setup

**Phase 6: Azure IoT Hub Simulator** - ✅ COMPLETE
- ✅ Device Registry & Identity Management
- ✅ Device Twins (Desired/Reported Properties)
- ✅ Direct Methods (Cloud-to-Device Commands)
- ✅ Device Provisioning Service (DPS) Patterns
- ✅ TPM 2.0 Hardware Security Attestation
- ✅ SAS Token & X.509 Certificate Authentication
- ✅ Complete audit trail for compliance

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
│ Transform Service → AI Engine → FHIR R4 API                    │
│ Azure IoT Hub Simulator (Device Registry, Twins, Methods)     │
└─────────────────────┬───────────────────────────────────────────┘
                      │ SignalR WebSocket
┌─────────────────────▼───────────────────────────────────────────┐
│ PRESENTATION LAYER (Single-Page Dashboard)                     │
│ ┌─────────────────────────────────────────────────────────┐    │
│ │  🏥 MedEdge System Dashboard (Interactive)               │    │
│ │  ┌─────────────────────────────────────────────────┐    │    │
│ │  │ 📊 Statistics Cards (Real-time)                   │    │    │
│ │  │    📱 Devices  🌐 Gateway  ⚡ Services  📊 Data │    │    │
│ │  └─────────────────────────────────────────────────┘    │    │
│ │  ┌─────────────────────────────────────────────────┐    │    │
│ │  │ 🔄 Interactive System Workflow Diagram           │    │    │
│ │  │  Click nodes to see details ▼                    │    │    │
│ │  │  ┌──────┐    ┌──────┐    ┌──────┐              │    │    │
│ │  │  │Devices│───▶│Gateway│───▶│  MQTT │              │    │    │
│ │  │  └──────┘    └──────┘    └──────┘              │    │    │
│ │  │       │             │           │              │    │    │
│ │  │       ▼             ▼           ▼              │    │    │
│ │  │  ┌─────────┐  ┌─────────┐  ┌──────────┐      │    │    │
│ │  │  │IoT Hub  │  │FHIR API │  │AI Engine │      │    │    │
│ │  │  └─────────┘  └─────────┘  └──────────┘      │    │    │
│ │  │       │             │                          │    │    │
│ │  │       ▼             ▼                          │    │    │
│ │  │  ┌─────────────────────────────┐             │    │    │
│ │  │  │     Blazor Dashboard       │             │    │    │
│ │  │  └─────────────────────────────┘             │    │    │
│ │  └─────────────────────────────────────────────────┘    │    │
│ │  🔄 Auto-refresh every 3 seconds | 🟢 LIVE UPDATES      │    │
│ └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

## 🔄 How It Works (End-to-End)

### The Complete Data Flow (Every 500ms)

```
1️⃣  DEVICE LAYER
   Medical Device generates vital signs (Modbus TCP registers)
   ↓ Blood Flow: 320 mL/min | Pressure: 120 mmHg | Temp: 36.5°C

2️⃣  EDGE GATEWAY (Protocol Translation)
   Polls Modbus registers every 500ms
   Converts register values to engineering units
   Creates JSON telemetry packet
   ↓

3️⃣  MQTT BROKER (Message Distribution)
   Publishes to topic: medical-device/{deviceId}/telemetry
   Ensures reliable message delivery with TLS encryption
   ↓ Parallel paths:

   ├─→ TRANSFORM SERVICE
   │   Converts to FHIR Observation format
   │   Maps measurements to LOINC codes (standards)
   │   POSTs to FHIR API for storage

   ├─→ AI ENGINE (Real-Time Analysis)
   │   Checks measurements against clinical thresholds
   │   Blood Flow < 150 mL/min → CRITICAL ALERT
   │   Arterial Pressure < 80 mmHg → HYPOTENSION WARNING
   │   Generates clinical recommendations

   └─→ SIGNALR HUB → DASHBOARD (Real-Time Display)
       WebSocket pushes updates to single-page dashboard
       Dashboard updates workflow status in real-time
       Click workflow nodes to see detailed information
       Clinical alerts appear immediately with recommendations

4️⃣  FHIR API (Healthcare Data Hub)
   Stores observations in database
   Maintains Patient ↔ Device ↔ Observation relationships
   Provides query endpoints for historical data
   Broadcasts updates via SignalR Hub

5️⃣  SINGLE-PAGE DASHBOARD (Clinician Interface)
   ✅ Interactive System Workflow diagram (click nodes for details)
   ✅ Real-time vital signs preview (6 key metrics)
   ✅ Device status cards with risk levels
   ✅ Service health monitoring
   ✅ Auto-refresh every 3 seconds
   ✅ Inline detail panels (no page navigation)
```

**Total Time: Device → Clinician Dashboard = <1 second**

## 🎨 Dashboard Features

### Interactive System Workflow
- **Clickable Nodes**: Click any component (Devices, Gateway, MQTT, IoT Hub, FHIR API, AI Engine, Dashboard) to see detailed status
- **Real-time Status Indicators**: Animated pulsing dots show health status (green=healthy, orange=warning, red=error)
- **Data Flow Animation**: Particles flow between layers showing active data transmission
- **4-Layer Visualization**: Edge Layer → Messaging Layer → Cloud Layer → Presentation Layer

### Inline Detail Panels
- **Medical Devices**: Shows connected devices with online status, type, last seen, and risk level
- **Edge Gateway**: Shows status, messages/sec, MQTT connection, and SignalR status
- **FHIR API**: Shows FHIR version, observation count, and API endpoint
- **Live Vitals**: Real-time display of Blood Flow, Arterial/Venous Pressure, Temperature, Conductivity, and Treatment Time

### Real-Time Statistics
- Total Devices with online count
- Gateway Status with messages/second
- Services Health (operational/total)
- Data Throughput (telemetry points/min)

### Quick Actions
- Refresh All (manually refresh all data)
- Show Live Vitals (display vital signs preview)
- Dashboard Info (session statistics)

## 🚀 Quick Start

### Prerequisites
- Docker Desktop (for containerized deployment)
- .NET 8.0 SDK (for local development only)

### Fastest Deployment (Docker Compose)

```bash
# Clone repository
git clone https://github.com/bejranonda/MedEdge-Gateway.git
cd MedEdge

# Configure dashboard credentials (optional)
echo "DASHBOARD_USERNAME=admin" > .env
echo "DASHBOARD_PASSWORD=YourSecurePassword123!" >> .env

# Build and start all services
docker-compose up -d --build

# Access dashboard
# Open browser to: http://localhost:5000
# You will be automatically redirected to the System Dashboard
```

**Access Points:**
- 🟢 **Dashboard**: http://localhost:8888 (Internal port, accessible via Nginx reverse proxy)
- 🟢 **FHIR API**: http://localhost:5001/swagger (REST API documentation)
- 🟢 **IoT Hub Simulator**: http://localhost:8080 (Azure IoT Hub patterns)
- 🟢 **MQTT Broker**: localhost:1883 (Message broker)

**Production Access:**
- 🟢 **Dashboard**: https://your-domain.com/ (via Nginx HTTPS reverse proxy)
- 🟢 **FHIR API**: https://your-domain.com/api/ (via HTTPS proxy)
- 🟢 **IoT Hub Simulator**: https://your-domain.com/iot-hub/ (via HTTPS proxy)

### VPS Deployment

```bash
# On your VPS server
git clone https://github.com/bejranonda/MedEdge-Gateway.git
cd MedEdge

# Set credentials
export DASHBOARD_USERNAME=admin
export DASHBOARD_PASSWORD=YourSecurePassword!

# Deploy
docker-compose up -d --build

# Access via public IP with HTTPS
# https://YOUR_DOMAIN_OR_IP (requires Nginx reverse proxy and SSL)
```

### Production HTTPS Setup

For production deployment with HTTPS:

```bash
# Install Nginx and Certbot
sudo apt install nginx certbot python3-certbot-nginx

# Configure Nginx reverse proxy (see DEPLOYMENT.md)
# Generate SSL certificate
sudo certbot --nginx -d your-domain.com --email admin@your-domain.com --agree-tos

# Start services
docker-compose up -d --build

# Access points:
# - Dashboard: https://your-domain.com/
# - FHIR API: https://your-domain.com/api/
# - MQTT: https://your-domain.com/mqtt/
```

## 📊 FHIR API Endpoints

### Patients
```
GET    /fhir/Patient              # List all patients
GET    /fhir/Patient/{id}         # Get patient by ID
POST   /fhir/Patient              # Create patient
```

### Devices
```
GET    /fhir/Device               # List all devices
GET    /fhir/Device/{id}          # Get device by ID
```

### Observations
```
GET    /fhir/Observation          # List observations
GET    /fhir/Observation/{id}     # Get observation by ID
POST   /fhir/Observation          # Create observation
GET    /fhir/Observation?patient={id}  # Filter by patient
GET    /fhir/Observation?device={id}   # Filter by device
GET    /fhir/Observation?code={code}   # Filter by LOINC code
```

### Dashboard API
```
GET    /api/devices              # Device status for dashboard
POST   /api/devices/{id}/emergency-stop  # Emergency stop command
GET    /health                    # Health check
```

## 🛠 Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| **Runtime** | .NET | 8.0 |
| **API** | ASP.NET Core | 8.0 |
| **FHIR SDK** | Firely .NET SDK | 5.5.0 |
| **Database** | SQLite / PostgreSQL | - |
| **ORM** | Entity Framework Core | 8.0 |
| **Dashboard** | Blazor WebAssembly | .NET 8 |
| **UI Framework** | MudBlazor | Latest |
| **Real-time** | SignalR | .NET 8 |
| **Messaging** | Eclipse Mosquitto MQTT | 2.0 |
| **Security** | JWT, X.509, TPM 2.0 | - |

## 📚 Documentation Structure

### For Beginners (New to .NET/C#)
| Document | Purpose | Time |
|----------|---------|------|
| **[LEARNING-GUIDE.md](LEARNING-GUIDE.md)** | 8-week .NET/C# learning path | 4-8 weeks |

### For Everyone
| Document | Purpose | Pages |
|----------|---------|-------|
| **README.md** | Project overview & quick start | This file |
| **QUICK-START.md** | Rapid deployment guide | Updated |
| **TECHNICAL-GUIDE.md** | How the system works | 100+ |
| **DEPLOYMENT.md** | Production deployment (VPS, Docker) | Updated |
| **DEMO.md** | 10-minute demo walkthrough | 60+ |
| **docs/ARCHITECTURE.md** | System design details | 100+ |
| **docs/FHIR-MAPPING.md** | FHIR resource mapping | 80+ |
| **IMPLEMENTATION.md** | Implementation summary | 100+ |

**Choose Your Path:**
- **New to .NET?** Start with [LEARNING-GUIDE.md](LEARNING-GUIDE.md)
- **Want to understand how it works?** Start with [TECHNICAL-GUIDE.md](TECHNICAL-GUIDE.md)
- **Want to deploy it?** Start with [QUICK-START.md](QUICK-START.md) or [DEPLOYMENT.md](DEPLOYMENT.md)
- **Want to see it in action?** Start with [DEMO.md](DEMO.md)

## 🔒 Security

- TLS 1.3 for all communications
- TPM 2.0 hardware attestation for device identity
- X.509 certificate validation
- SAS token authentication
- Audit logging for all operations
- Input validation on all API endpoints
- Environment-based configuration (no secrets in code)
- Dashboard credential protection via build arguments

## 📝 License

MIT License - See LICENSE file for details

## 👨‍💻 Author

Built as a portfolio project demonstrating expertise in:
- Azure IoT Hub architecture and patterns
- FHIR R4 healthcare interoperability
- Industrial IoT architecture
- Hardware security (TPM, certificates)
- Real-time clinical decision support
- Full-stack .NET development
- Interactive single-page applications

## 🤝 Contributing

This project is under active development.

---

**Current Phase:** 6/6 Complete ✅  
**Current Version:** v1.2.0-beta  
**Last Updated:** 2026-01-25  
**Status:** Production Ready - All services operational with real-time monitoring  

## 🎉 Latest Release (v1.2.0-beta)

### What's New
- ✅ **Fixed CORS Blocking**: Dashboard now properly communicates with backend API
- ✅ **Resolved API Stability Issues**: Defensive data handling prevents 500 errors
- ✅ **Dashboard Build Fixed**: Compilation errors resolved, builds successfully
- ✅ **Optimized Layout**: Compact header design with version badge
- ✅ **Single-Page Design**: All system information accessible without page navigation
- ✅ **Real-Time Updates**: Live device status, gateway metrics, and service health
- ✅ **Interactive Workflow**: Click nodes to see detailed component information
- ✅ **HTTPS Production Ready**: Nginx reverse proxy with Let's Encrypt SSL support
- ✅ **Port Configuration Updated**: Dashboard runs on port 8888 to avoid conflicts

### Bug Fixes
- Fixed SystemDashboard.razor brace mismatch (RZ1006)
- Updated FhirApi CORS policy for localhost cross-origin requests
- Made FhirDeviceEntity properties nullable for SQLite compatibility
- Added comprehensive error handling to api/devices endpoint
- Removed excessive top padding from dashboard container
- Resolved Nginx port conflicts with dashboard service
