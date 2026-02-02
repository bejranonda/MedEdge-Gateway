# Architecture Revision Summary v2.0

## Overview

The MedEdge Gateway architecture has been revised from a single-region deployment model to a **global-scale three-tier architecture** (Local → Regional → Global) based on industry best practices for medical device IoT platforms.

## What Changed

### 1. Architecture Document
- **New Document**: `docs/ARCHITECTURE-v2.0-Global-Scale.md`
- Complete architectural specification for global deployment
- Database strategy with distributed data sovereignty
- Security and compliance framework
- Technology stack for each tier

### 2. Dashboard Updates
- **Visual Changes**: Three-tier workflow diagram (Global → Regional → Local)
- **Color Coding**:
  - 🟢 Green: Global (Management & Analytics)
  - 🔵 Blue: Regional (Cloud Services)
  - 🟣 Purple: Local (Facility Edge)
- **New Detail Panels**: All v2.0 architecture components now clickable
- **Header**: Updated to v2.0 with new subtitle

### 3. Data Sovereignty Strategy

| Tier | Data Scope | Database | Retention | PHI Access |
|------|-----------|----------|-----------|------------|
| **Local** | Patient data, treatment sessions | SQLite/PostgreSQL | 7 years | Full |
| **Regional** | Aggregates, anonymized data | PostgreSQL Cluster | 10 years | Anonymized |
| **Global** | Device catalog, analytics | Cassandra | 25 years | None |

## Research-Based Improvements

Based on research from these sources:
- [AWS Cloud Platform for Medical Device Data](https://www.cardinalpeak.com/product-development-case-studies/scalable-aws-data-platform-for-global-medical-diagnostics-leader)
- [HIPAA and GDPR Compliance in IoT Healthcare Systems](https://www.researchgate.net/publication/379129933_HIPAA_and_GDPR_Compliance_in_IoT_Healthcare_Systems)
- [A global federated real-world data and analytics platform](https://pmc.ncbi.nlm.nih.gov/articles/PMC10182857/)
- [Building a Unified Healthcare Data Platform: Architecture](https://medium.com/doctolib/building-a-unified-healthcare-data-platform-architecture-2bed2aaaf437)
- [Data Sovereignty in a Multi-Cloud World](https://www.betsol.com/blog/data-sovereignty-in-a-multi-cloud-world/)
- [Trustworthy AI-based Federated Learning Architecture](https://www.sciencedirect.com/science/article/pii/S0925231224001863)
- [HECS4MQTT: Multi-Layer Security Framework for Healthcare](https://www.mdpi.com/1999-5903/17/7/298)

### Key Improvements

#### 1. Federated Learning for AI
```
Local Edge Models → Regional Aggregation → Global Training
     ↓ (raw data)        ↓ (model updates)      ↓ (new models)
```
- **Benefit**: Improves AI without crossing PHI boundaries
- **Compliance**: HIPAA/GDPR compliant by design

#### 2. Active-Active Multi-Region Deployment
- Regional services deploy in active-active pairs
- Global load balancing directs traffic to nearest region
- Automatic failover on regional outage
- **Target**: 99.99%+ availability

#### 3. Disaster Recovery at Edge
- Edge gateways include offline buffering
- Local database continues during regional outage
- Automatic sync when connectivity restored
- **Benefit**: Medical devices continue operating during cloud outage

#### 4. Supply Chain Resilience
- Local inventory at each treatment center
- Regional distribution centers with AI forecasting
- Global vendor coordination
- **Benefit**: Critical supplies always available

## Implementation Roadmap

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

## Technology Stack Summary

### Local (Facility Edge)
| Component | Technology |
|-----------|------------|
| Runtime | .NET 8.0 |
| Database | SQLite (devices), PostgreSQL (facilities) |
| Messaging | MQTTnet |
| Dashboard | Blazor WebAssembly |
| Security | TPM 2.0, X.509 certificates |

### Regional (Cloud Services)
| Component | Technology |
|-----------|------------|
| Runtime | .NET 8.0 |
| Database | PostgreSQL, InfluxDB |
| Messaging | MQTTnet, EMQX/VerneMQ |
| API | ASP.NET Core Minimal APIs |
| FHIR | Firely .NET SDK 5.5.0 |
| AI | ML.NET + ONNX Runtime |
| Cache | Redis |

### Global (Management)
| Component | Technology |
|-----------|------------|
| Database | Cassandra/scyllaDB |
| Messaging | Apache Kafka |
| ML | PyTorch/TensorFlow |
| CDN | Cloudflare/Azure CDN |
| OTA | Azure IoT Hub / AWS IoT Device Management |

## Files Modified

```
docs/ARCHITECTURE-v2.0-Global-Scale.md  (NEW)
docs/ARCHITECTURE-REVISION-SUMMARY.md   (NEW)
src/Web/MedEdge.Dashboard/Pages/SystemDashboard.razor (MODIFIED)
```

## Next Steps

1. **Review**: Review the architecture document and provide feedback
2. **Prioritize**: Decide which phase components to implement first
3. **Prototype**: Create proof-of-concept for federated MQTT
4. **Deploy**: Begin regional deployment strategy

## Success Metrics

| Metric | Target | Current |
|--------|--------|---------|
| Availability | 99.99% | ~99% |
| Latency (p99) | < 100ms | ~200ms |
| Regions | 3+ | 1 |
| Disaster Recovery | < 15 min | Manual |
| Compliance | 100% | ~90% |

---

**Document Version**: 2.0
**Last Updated**: 2026-02-02
**Status**: Architecture Revision Complete
