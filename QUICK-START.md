# MedEdge Gateway - Quick Start Guide

> **⚠️ DEMONSTRATION PROJECT**
>
> This is a **technology demonstration** showcasing medical device IoT architecture concepts.
> All data is simulated. Not for clinical or production use.

---

## ⚡ How It Works (90 Seconds)

MedEdge Gateway is a real-time clinical monitoring platform for dialysis machines:

1. **Every 500ms:** Dialysis machines send vital signs (blood flow, pressures, temperature) via **Modbus TCP**
2. **Edge Gateway** translates Modbus → **MQTT** messages (JSON telemetry published to cloud)
3. **Transform Service** converts MQTT → **FHIR Observation** resources (healthcare standard)
4. **AI Engine** analyzes observations for anomalies (detects hypotension, fever, equipment issues)
5. **SignalR WebSocket** broadcasts real-time updates to **Single-Page Blazor Dashboard**
6. **Interactive System Workflow**: Click any component to see detailed status inline (no page navigation)

**Result:** From device measurement to interactive dashboard with detailed views: **< 1 second**

## 🎨 New: Single-Page Interactive Dashboard

The dashboard has been redesigned as a **single-page application** with:

### Interactive System Workflow Diagram
- **4-Layer Architecture Visualization**: Edge → Messaging → Cloud → Presentation
- **Clickable Nodes**: Click any component to see detailed status inline
- **Real-Time Status Indicators**: Animated pulsing dots (green=healthy, orange=warning, red=error)
- **Data Flow Animation**: Particles show active data transmission between layers

### Real-Time Monitoring
- **Statistics Cards**: Total devices, Gateway status, Services health, Data throughput
- **Auto-Refresh**: Every 3 seconds with LIVE UPDATES badge
- **Device Status**: Shows online/offline devices with risk levels
- **Live Vitals Preview**: 6 key metrics (Blood Flow, Pressures, Temperature, Conductivity, Treatment Time)

### Inline Detail Panels
When you click on workflow nodes:
- **Medical Devices**: List of connected devices with status
- **Edge Gateway**: MQTT/SignalR status, message throughput
- **FHIR API**: Version info, observation count, API endpoint
- **Live Vitals**: Real-time vital signs with normal ranges

### No Navigation Menu
- Clean, full-screen experience
- All information accessible via the interactive workflow diagram
- Quick actions for Refresh, Show Vitals, Dashboard Info

For detailed explanation, see **[TECHNICAL-GUIDE.md](TECHNICAL-GUIDE.md)** and **[ARCHITECTURE-DIAGRAMS.md](ARCHITECTURE-DIAGRAMS.md)**.

---

## 🚀 Start the Full System

### Option A: Docker Compose (Recommended for VPS/Demo)

```bash
# Clone repository
git clone https://github.com/bejranonda/MedEdge-Gateway.git
cd MedEdge

# Configure dashboard credentials (optional but recommended)
echo "DASHBOARD_USERNAME=admin" > .env
echo "DASHBOARD_PASSWORD=YourSecurePassword123!" >> .env

# Build and start all services
docker-compose up -d --build

# Wait 60 seconds for services to initialize
# Then open your browser
```

**Access the Dashboard:**
- **http://localhost:5000** (Auto-redirects to System Dashboard)
- You will see the **Single-Page Interactive Dashboard** immediately
- **Click on workflow nodes** to explore system details

**Other Endpoints:**
- 🟢 **FHIR API**: http://localhost:5001/swagger (API documentation)
- 🟢 **IoT Hub Simulator**: http://localhost:8080 (Azure IoT Hub patterns)
- 🟢 **MQTT Broker**: localhost:1883 (Message broker)

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

# Access via public IP
# http://YOUR_SERVER_IP:5000
```

**Result:** When users visit your VPS public IP, they see the interactive System Dashboard immediately.

---

## 📊 What You're Running

When all services are up:

1. **Device Simulator** - Creates realistic dialysis telemetry on Modbus TCP
2. **Edge Gateway** - Polls Modbus and publishes to MQTT
3. **MQTT Broker** - Routes messages between edge and cloud
4. **FHIR API** - RESTful FHIR server with SQLite database
5. **Transform Service** - Converts MQTT telemetry to FHIR Observations
6. **AI Engine** - Detects anomalies in real-time
7. **Dashboard** - Single-page interactive UI with real-time updates

## 🎯 How to Use the Dashboard

### 1. View System Overview
When the dashboard loads, you see:
- **Statistics Cards** (top): Total devices, Gateway status, Services health, Data throughput
- **Interactive Workflow Diagram** (center): 4-layer system architecture
- **Quick Actions** (bottom): Refresh, Show Vitals, Dashboard Info

### 2. Explore the Interactive Workflow
**Click on any component in the workflow diagram:**

| Component | What You'll See |
|-----------|----------------|
| **Medical Devices** | List of connected devices with online status, type, last seen time, and risk level |
| **Edge Gateway** | Status (Operational/Degraded), messages/sec, MQTT connection status, SignalR hub status |
| **MQTT Broker** | Connection status (Connected/Disconnected) |
| **IoT Hub** | Online status and device registry info |
| **FHIR API** | FHIR version (R4), total observations, API endpoint URL |
| **AI Engine** | Monitoring status and anomaly detection info |
| **Dashboard** | Session duration, total updates received, last update time |

### 3. View Live Vitals
Click **"Show Live Vitals"** button to see:
- Blood Flow Rate (mL/min)
- Arterial Pressure (mmHg)
- Venous Pressure (mmHg)
- Temperature (°C)
- Conductivity (mS/cm)
- Treatment Time (hours:minutes)

### 4. Refresh Data
- **Auto-refresh**: Dashboard updates automatically every 3 seconds
- **Manual refresh**: Click "Refresh All" button
- **Status badge**: "🟢 LIVE UPDATES" shows real-time connection

## 🧪 Quick Tests

### 1. Check Services Running

```bash
# FHIR API Health
curl http://localhost:5001/health

# Should return:
# {"status":"healthy"}
```

### 2. List Devices

```bash
curl http://localhost:5001/api/devices | jq '.'
```

### 3. Access Dashboard

```bash
# Dashboard (auto-redirects to System Dashboard)
curl http://localhost:5000

# Or open in browser
# http://localhost:5000
```

### 4. View FHIR Data

```bash
# List patients
curl http://localhost:5001/fhir/Patient | jq '.'
```

## 🔐 Dashboard Credentials

### Setting Credentials

**Option 1: Using .env file**
```bash
echo "DASHBOARD_USERNAME=admin" > .env
echo "DASHBOARD_PASSWORD=YourSecurePassword123!" >> .env
docker-compose up -d --build
```

**Option 2: Using environment variables**
```bash
DASHBOARD_USERNAME=admin DASHBOARD_PASSWORD=YourSecurePassword123! docker-compose up -d --build
```

**Option 3: Default credentials** (if not configured)
- Username: `guest`
- Password: `changeme`

> **⚠️ Security Warning**: Always change default credentials before production deployment!

### Credential Storage

Credentials are embedded in the dashboard at build time via Docker build arguments:
- Stored in `/usr/share/nginx/html/config-env.js`
- Used by the dashboard for authentication
- Not accessible via API endpoints

## 🐛 Troubleshooting

### Dashboard Shows Old Design/Menu

**Problem:** Left navigation menu still appears

**Solution:**
1. Rebuild the container (no cache):
   ```bash
   docker-compose down
   docker-compose build --no-cache dashboard
   docker-compose up -d dashboard
   ```

2. Hard refresh browser:
   - Chrome/Edge: `Ctrl + Shift + R`
   - Firefox: `Ctrl + Shift + R`
   - Or use Incognito/Private mode

### Port Already in Use

```bash
# Find process using port 5001
lsof -i :5001  # macOS/Linux
netstat -ano | findstr :5001  # Windows

# Kill process
kill -9 <PID>  # macOS/Linux
taskkill /PID <PID> /F  # Windows
```

### Dashboard Won't Load

```bash
# Check dashboard service
docker-compose logs dashboard

# Verify port is accessible
curl http://localhost:5000

# Restart dashboard
docker-compose restart dashboard
```

### No Data Showing

```bash
# Check if other services are running
docker-compose ps

# Check FHIR API
curl http://localhost:5001/health

# Check logs
docker-compose logs -f fhir-api
docker-compose logs -f transform-service
```

## 📈 Data Flow Visualization

```
Time: 0s  - Services start
Time: 5s  - Device Simulator generates telemetry
          - Edge Gateway polls Modbus (every 500ms)
          - MQTT receives messages
          - Transform Service maps to FHIR Observations
          - FHIR API persists to database
          - AI Engine analyzes for anomalies
          - Dashboard auto-refreshes every 3 seconds

Time: 10s - Dashboard shows real-time statistics
            - Click workflow nodes to see details
            - Auto-refresh updates all metrics
            - Live indicators show system health
```

## 🎯 Dashboard Navigation

Since this is a **single-page application**, there's no navigation menu. All information is accessible via:

1. **Interactive Workflow Diagram** - Click nodes to see details
2. **Quick Action Buttons**:
   - Refresh All
   - Show Live Vitals
   - Dashboard Info
3. **Auto-Refresh** - Updates every 3 seconds automatically

## 📚 Documentation

- **[README.md](README.md)** - Project overview and single-page dashboard features
- **[ARCHITECTURE.md](docs/ARCHITECTURE.md)** - System design details
- **[FHIR-MAPPING.md](docs/FHIR-MAPPING.md)** - FHIR resource mapping
- **[IMPLEMENTATION.md](IMPLEMENTATION.md)** - Implementation summary
- **[DEPLOYMENT.md](DEPLOYMENT.md)** - Production deployment guide

## 🔑 API Endpoints (Swagger Available)

When FHIR API is running, visit: **http://localhost:5001/swagger**

### Main Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/fhir/Patient` | List all patients |
| GET | `/fhir/Patient/{id}` | Get patient by ID |
| GET | `/fhir/Device` | List all devices |
| GET | `/fhir/Device/{id}` | Get device by ID |
| POST | `/fhir/Observation` | Create new observation |
| GET | `/fhir/Observation` | List observations |
| GET | `/fhir/Observation?patient={id}` | Get observations for patient |
| GET | `/fhir/Observation?device={id}` | Get observations from device |
| GET | `/health` | Health check |

## 🔐 Security Notes

- All services communicate over internal Docker network
- MQTT TLS can be enabled in configuration
- No hardcoded credentials in code
- Dashboard credentials set via environment variables
- Logs don't contain sensitive data
- Database is local SQLite (not production-ready)

## 🎯 Next Steps

1. **For Testing:** Click workflow nodes to explore system details
2. **For Development:** Explore code in `src/` directory
3. **For Learning:** Read `docs/ARCHITECTURE.md` for system design
4. **For Production:** Set strong credentials and enable TLS

## 🎉 Success Indicators

When everything is working:

✅ Dashboard loads at http://localhost:5000
✅ Interactive workflow diagram displays with status indicators
✅ Clicking nodes shows inline detail panels
✅ Statistics auto-refresh every 3 seconds
✅ FHIR API responds at http://localhost:5001/swagger
✅ `GET /health` returns healthy status
✅ Live updates badge shows "🟢 LIVE UPDATES"

---

**For complete implementation details, see [IMPLEMENTATION.md](IMPLEMENTATION.md)**
**For VPS deployment, see [DEPLOYMENT.md](DEPLOYMENT.md)**
