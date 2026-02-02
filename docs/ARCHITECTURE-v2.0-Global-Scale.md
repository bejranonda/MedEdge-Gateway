# MedEdge Global Scale Architecture v2.0

## Executive Summary

This document presents a comprehensive architectural revision for a **global-scale medical device IoT platform** based on industry best practices, regulatory compliance requirements (HIPAA, GDPR), and modern distributed systems patterns.

## Research Sources

Based on research from industry leaders and academic sources:
- [AWS Cloud Platform for Medical Device Data](https://www.cardinalpeak.com/product-development-case-studies/scalable-aws-data-platform-for-global-medical-diagnostics-leader)
- [HIPAA and GDPR Compliance in IoT Healthcare Systems](https://www.researchgate.net/publication/379129933_HIPAA_and_GDPR_Compliance_in_IoT_Healthcare_Systems)
- [A global federated real-world data and analytics platform](https://pmc.ncbi.nlm.nih.gov/articles/PMC10182857/)
- [Building a Unified Healthcare Data Platform: Architecture](https://medium.com/doctolib/building-a-unified-healthcare-data-platform-architecture-2bed2aaaf437)
- [Data Sovereignty in a Multi-Cloud World](https://www.betsol.com/blog/data-sovereignty-in-a-multi-cloud-world/)
- [Edge Computing vs Cloud Computing](https://www.emma.ms/blog/edge-computing-vs-cloud-computing)
- [Trustworthy AI-based Federated Learning Architecture](https://www.sciencedirect.com/science/article/pii/S0925231224001863)
- [HECS4MQTT: Multi-Layer Security Framework for Healthcare](https://www.mdpi.com/1999-5903/17/7/298)

## Revised Three-Tier Architecture

### Level 1: LOCAL (Facility Edge)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        LOCAL - FACILITY EDGE                             │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  ┌─────────────────────┐    ┌──────────────────────┐                    │
│  │   CLIENT GROUP      │    │   FACILITY GROUP     │                    │
│  ├─────────────────────┤    ├──────────────────────┤                    │
│  │ • Medical Devices   │    │ • Treatment Center   │                    │
│  │ • Monitoring Ctr    │    │   (Hospital/Clinic)  │                    │
│  │                     │    │                      │                    │
│  │ [Edge Gateway] ─────┼────┼────► [Edge Gateway]  │                    │
│  │      Hospital       │    │        Store         │                    │
│  │         │           │    │         │            │                    │
│  │         ▼           │    │         ▼            │                    │
│  │   [MQTT Broker]     │    │   [Local Database]   │                    │
│  │   (Facility)        │    │   (SQLite/PostgreSQL)│                    │
│  └─────────────────────┘    └──────────────────────┘                    │
│                                                                           │
│  Database: LOCAL FACILITY DATABASE                                        │
│  - Patient data (in-facility only)                                       │
│  - Treatment sessions                                                     │
│  - Device configurations                                                  │
│  - Supply inventory (local)                                              │
│  - Offline buffering for disaster recovery                               │
└─────────────────────────────────────────────────────────────────────────┘
```

**Local Services:**
- **Edge Gateway (Hospital)**: Modbus → MQTT translation, local processing
- **Edge Gateway (Store)**: API gateway for treatment center supplies
- **Local MQTT Broker**: Facility-level message routing (Eclipse Mosquitto)
- **Local Database**: SQLite for edge devices, PostgreSQL for facilities
- **Device Controller**: Modbus RTU/TCP controller for medical devices
- **Monitoring Center UI**: Real-time facility dashboard

**Data Sovereignty:** All patient data remains within facility boundaries per HIPAA/GDPR

---

### Level 2: REGIONAL (Cloud Services)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     REGIONAL - CLOUD & SERVICES                          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │              CLOUD & SERVICES (Regional)                          │   │
│  │  ┌────────────────────────────────────────────────────────────┐  │   │
│  │  │  Treatment Service • Device Coordination • Analytics       │  │   │
│  │  │  Transform Service • AI Clinical Engine                    │  │   │
│  │  └────────────────────────────────────────────────────────────┘  │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                           │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │              TREATMENT CENTER LAYER                               │   │
│  │  • Zone Management (A-F: 6 zones)                                 │   │
│  │  • Station Coordination (52 stations)                             │   │
│  │  • Multi-device Synchronization                                  │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                           │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │              SUPPLY CENTER                                        │   │
│  │  • Regional Distribution                                         │   │
│  │  • AI-Powered Demand Forecasting                                 │   │
│  │  • Vendor Integration                                            │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                           │
│  Database: REGIONAL DATABASE (Clustered)                                  │
│  - Multi-region PostgreSQL cluster                                       │
│  - Read replicas for high availability                                   │
│  - Time-series data (InfluxDB/TimescaleDB)                               │
│  - Device registry (regional)                                            │
│  - Aggregated/anonymized analytics                                       │
│  - Federated learning model aggregation                                  │
└─────────────────────────────────────────────────────────────────────────┘
```

**Regional Services:**
- **Treatment Service**: Session lifecycle, patient flow management
- **Device Coordination Service**: Multi-device orchestration at stations
- **Analytics Service**: Regional treatment metrics, performance analytics
- **Transform Service**: MQTT → FHIR R4 transformation with LOINC mapping
- **AI Clinical Engine**: Anomaly detection, federated learning coordinator
- **FHIR API Server**: USCDI v3 compliant healthcare data exchange
- **IoT Hub (Regional)**: Device registry, twins, DPS, TPM attestation
- **Supply Chain Service**: Regional inventory, AI forecasting

**Data Sovereignty:** Data residency enforced by region (e.g., EU data stays in EU)

---

### Level 3: GLOBAL (Management & Analytics)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      GLOBAL - MANAGEMENT & ANALYTICS                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │              CLOUD & SERVICES (Global)                             │   │
│  │  ┌────────────────────────────────────────────────────────────┐  │   │
│  │  │  Global Device Management • Firmware OTA                   │  │   │
│  │  │  Global Analytics • Model Training                         │  │   │
│  │  │  Compliance Monitoring • Audit Log                         │  │   │
│  │  └────────────────────────────────────────────────────────────┘  │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                           │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │              GLOBAL FEDERATED MESSAGING                           │   │
│  │  • Cross-region event replication                                │   │
│  │  • Global message routing                                        │   │
│  │  • Disaster recovery coordination                                │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                           │
│  Database: GLOBAL DATABASE (Distributed)                                  │
│  - Distributed database (Cassandra/scyllaDB)                             │
│  - Global device catalog (no PHI)                                        │
│  - Firmware/software versions                                            │
│  - Anonymized global analytics                                          │
│  - Audit logs (immutable)                                               │
│  - ML model artifacts                                                    │
└─────────────────────────────────────────────────────────────────────────┘
```

**Global Services:**
- **Global Device Management**: Fleet management, software/firmware OTA
- **Global Analytics**: Cross-regional insights, population health
- **ML Model Training**: Centralized model training, federated learning
- **Compliance Service**: Global audit, regulatory reporting
- **Disaster Recovery**: Multi-region failover coordination

**Data Sovereignty:** NO PHI at global level; only anonymized aggregates and device metadata

---

## Database Architecture

### Distributed Data Strategy

| Tier | Database Type | Data Scope | Retention | Replication |
|------|--------------|------------|-----------|-------------|
| **Local** | SQLite/PostgreSQL | Facility PHI | 7 years | None |
| **Regional** | PostgreSQL Cluster | Regional aggregates | 10 years | Multi-AZ, read replicas |
| **Global** | Cassandra/scyllaDB | Device catalog, analytics | 25 years | Multi-region |

### Data Flows

```
Patient Data Flow (PHI):
Medical Device → Edge Gateway → Local DB → (Anonymized) → Regional DB → (Aggregated) → Global DB

Device Management Flow:
Global Service → Regional Distribution → Edge Gateway → Medical Device

Emergency/Failover:
Edge Gateway → Local Buffer → (Offline Mode) → Sync when Regional available
```

---

## Key Improvements Over Original Design

### 1. **Data Sovereignty by Design**
- **Local**: Full PHI retention within facility
- **Regional**: Data residency by geography (GDPR compliance)
- **Global**: Zero PHI, only device metadata and analytics

### 2. **Federated Learning for AI**
- Local edge models train on facility data
- Regional coordinator aggregates model updates (not raw data)
- Global service trains improved models, pushes updates
- **Benefit**: Improves AI while maintaining privacy

### 3. **Active-Active Multi-Region Deployment**
- Regional services deploy in active-active pairs
- Global load balancing directs traffic to nearest region
- Automatic failover on regional outage
- **Benefit**: 99.99%+ availability target

### 4. **Disaster Recovery at Edge**
- Edge gateways include offline buffering
- Local database continues during regional outage
- Automatic sync when connectivity restored
- **Benefit**: Medical devices continue operating during cloud outage

### 5. **Federated Messaging Architecture**
- Local MQTT brokers federate to regional clusters
- Regional MQTT clusters federate globally
- Topic hierarchy: `local/{facility}/`, `regional/{region}/`, `global/`
- **Benefit**: Scalable message routing across regions

### 6. **Supply Chain Resilience**
- Local inventory at each treatment center
- Regional distribution centers with AI forecasting
- Global vendor coordination
- **Benefit**: Critical supplies always available

---

## Technology Stack

### Local (Facility Edge)
| Component | Technology | Purpose |
|-----------|------------|---------|
| Runtime | .NET 8.0 | Cross-platform edge execution |
| Database | SQLite (devices), PostgreSQL (facilities) | Local data persistence |
| Messaging | MQTTnet | Device protocol |
| Dashboard | Blazor WebAssembly | Local monitoring UI |
| Security | TPM 2.0, X.509 certificates | Device identity |

### Regional (Cloud Services)
| Component | Technology | Purpose |
|-----------|------------|---------|
| Runtime | .NET 8.0 | Service execution |
| Database | PostgreSQL (primary), InfluxDB (time-series) | Regional data store |
| Messaging | MQTTnet, EMQX/VerneMQ | Federated MQTT broker |
| API | ASP.NET Core Minimal APIs | RESTful endpoints |
| FHIR | Firely .NET SDK 5.5.0 | Healthcare interoperability |
| AI | ML.NET + ONNX Runtime | Federated learning coordinator |
| Cache | Redis | Session caching |

### Global (Management)
| Component | Technology | Purpose |
|-----------|------------|---------|
| Database | Cassandra/scyllaDB | Distributed data store |
| Messaging | Apache Kafka | Global event streaming |
| ML | PyTorch/TensorFlow | Model training |
| CDN | Cloudflare/Azure CDN | Asset distribution |
| OTA | Azure IoT Hub / AWS IoT Device Management | Firmware updates |

---

## Security Architecture

### Defense in Depth

1. **Device Layer**: TPM 2.0 + X.509 certificates
2. **Edge Layer**: TLS 1.3 for all communications, local attestation
3. **Regional Layer**: VPC isolation, private endpoints, Azure Firewall
4. **Global Layer**: DDoS protection, Web Application Firewall
5. **Data Layer**: Encryption at rest (AES-256), encryption in transit (TLS 1.3)

### Compliance Framework

- **HIPAA**: Business Associate Agreement (BAA) compliant cloud regions
- **GDPR**: Data residency by EU/UK region, consent management
- **FDA 21 CFR Part 11**: Electronic records, electronic signatures
- **ISO 27001**: Information security management
- **ISO 13485**: Medical device quality management

### Audit & Monitoring

- Immutable audit logs (blockchain-backed)
- Real-time security monitoring
- Automated compliance reporting
- Penetration testing (quarterly)

---

## Deployment Architecture

### Multi-Region Pattern

```
                Global Load Balancer
                        ↓
        ┌───────────────┼───────────────┐
        ↓               ↓               ↓
   Region A (US)    Region B (EU)   Region C (APAC)
   [Active-Active]  [Active-Active]  [Active-Active]
        ↓               ↓               ↓
    Facilities     Facilities       Facilities
       (10+)          (10+)           (10+)
```

### Scaling Strategy

| Service Type | Scaling Mode | Auto-Scale Trigger |
|--------------|--------------|-------------------|
| API Services | Horizontal | CPU > 70%, Request queue |
| MQTT Brokers | Cluster | Connection count |
| Analytics Workers | Horizontal | Queue depth |
| Database | Read replicas | Read latency |

---

## Communication Protocols

### Device to Cloud
- **Protocol**: MQTT 5.0 over TLS 1.3
- **QoS**: QoS 1 for critical alerts, QoS 0 for telemetry
- **Topics**: `devices/{region}/{facility}/{deviceId}/telemetry`

### Cloud to Cloud
- **Protocol**: Apache Kafka for events, REST for APIs
- **Pattern**: Event-driven architecture with CQRS

### Edge to Facility
- **Protocol**: Modbus RTU/TCP, MQTT
- **Fallback**: Local buffering during outage

---

## Monitoring & Observability

### Metrics Collection
- **Local**: Device vitals, gateway health, local queue depth
- **Regional**: Service metrics, API performance, database latency
- **Global**: Cross-region latency, aggregate health, compliance status

### Logging Strategy
- **Structured Logging**: JSON format with correlation IDs
- **Log Aggregation**: ELK Stack (local), Splunk (regional/global)
- **Retention**: 90 days hot, 7 years cold

### Alerting
- **P1**: Patient safety alerts (immediate)
- **P2**: Service down (5 minutes)
- **P3**: Performance degradation (15 minutes)

---

## Migration Path

### Phase 1: Foundation (Months 1-3)
- Implement federated MQTT broker architecture
- Deploy regional database clusters
- Add data residency enforcement

### Phase 2: Resilience (Months 4-6)
- Implement edge offline buffering
- Add regional active-active deployment
- Deploy disaster recovery automation

### Phase 3: Intelligence (Months 7-9)
- Implement federated learning pipeline
- Deploy global analytics platform
- Add AI-powered forecasting

### Phase 4: Optimization (Months 10-12)
- Performance tuning
- Cost optimization
- Compliance automation

---

## Cost Optimization

### Reserved Instances
- 1-year reserved for regional baseline load
- 3-year reserved for global stable services

### Spot Instances
- Use for batch analytics workers
- Non-critical background processing

### Data Transfer
- Regional data aggregation before global sync
- Compress and deduplicate telemetry data
- Use CDN for firmware distribution

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Availability | 99.99% | Uptime monitoring |
| Latency (p99) | < 100ms | Application insights |
| Device Onboarding | < 5 min | Process automation |
| Recovery Time | < 15 min | Disaster recovery drills |
| Compliance Pass Rate | 100% | Automated audits |

---

## Conclusion

This architecture provides a **production-ready, globally scalable medical device IoT platform** that:
- Maintains regulatory compliance across regions
- Enables real-time patient monitoring with sub-second latency
- Scales to millions of devices across continents
- Provides disaster resilience at every layer
- Leverages AI for predictive maintenance and supply optimization

The three-tier model (Local → Regional → Global) ensures data sovereignty while enabling global intelligence through federated learning and anonymized analytics.

---

**Document Version**: 2.0
**Last Updated**: 2026-02-02
**Status**: Proposed Architecture
