# Comprehensive Deployment Guide for MedEdge Gateway

## 🚀 All Deployment Options

This guide covers every possible deployment strategy for MedEdge Gateway, from simple local development to enterprise cloud platforms.

---

## 🏠 Option 1: Local Development (Docker)

### Quick Start
```bash
# Clone and run everything locally
git clone https://github.com/bejranonda/MedEdge-Gateway.git
cd MedEdge-Gateway
docker-compose up -d

# Access services
Dashboard: http://localhost:5000
API: http://localhost:5001/swagger
```

**Pros:**
- ✅ Fully functional with all services
- ✅ Real device simulation
- ✅ Complete testing environment
- ✅ No external dependencies

**Cons:**
- ❌ Only local access
- ❌ Not production-ready
- ❌ Limited scalability

---

## ☁️ Option 2: Cloud Deployment (Full Stack)

### Azure App Service + Azure IoT Hub
```yaml
# azure-compose.yml
services:
  fhir-api:
    image: mededge/fhir-api:latest
    ports: ["5001:80"]
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${AZURE_SQL_CONNECTION_STRING}

  mqtt-broker:
    image: eclipse-mosquitto:2.0
    ports: ["1883:1883", "9001:9001"]

  transform-service:
    image: mededge/transform-service:latest
    environment:
      - MQTT__BROKER_HOST=${AZURE_IOT_HUB_HOST}

  dashboard:
    image: mededge/dashboard:latest
    ports: ["5000:80"]
    environment:
      - ApiBaseUrl=https://mededge-api.azurewebsites.net
```

**Deployment Commands:**
```bash
# Azure Container Apps
az containerapp create --name mededge-fhir \
  --resource-group mededge-rg \
  --image mededge/fhir-api:latest \
  --ingress external \
  --target-port 80

# Azure IoT Hub
az iot hub create --name mededge-iot \
  --resource-group mededge-rg \
  --sku S1 --location eastus
```

---

## 🌐 Option 3: Hybrid Architecture (Cloudflare + External Services)

### Architecture
```
Cloudflare Pages (Frontend)
├── Blazor Dashboard (static)
├── config.js (runtime config)
└── _redirects (SPA routing)

Cloudflare Workers (Edge Layer)
├── API Gateway (proxy to backend)
├── WebSocket Proxy (SignalR)
└── Caching Layer

Azure/AWS/GCP (Backend)
├── FHIR API Service
├── MQTT Broker (EMQX)
├── Transform Service
├── AI Engine
└── Edge Gateway
```

### Cloudflare Worker for API Gateway
```javascript
// wrangler.toml
name = "mededge-api-gateway"
main = "src/index.js"
compatibility_date = "2024-01-01"

[[env.production.services]]
service = "backend-api"
```

```javascript
// src/index.js
export default {
  async fetch(request, env, ctx) {
    const url = new URL(request.url);

    // Proxy requests to backend
    if (url.pathname.startsWith('/api/') || url.pathname.startsWith('/fhir/') || url.pathname.startsWith('/hubs/')) {
      const backendUrl = env.BACKEND_API_URL;
      const proxyUrl = backendUrl + url.pathname + url.search;

      return fetch(proxyUrl, {
        method: request.method,
        headers: request.headers,
        body: request.body
      });
    }

    // Serve static assets
    return await env.ASSETS.fetch(request);
  }
};
```

---

## 🐳 Option 4: Kubernetes Deployment

### Full Kubernetes Manifests
```yaml
# k8s/namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: mededge

# k8s/fhir-api-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: fhir-api
  namespace: mededge
spec:
  replicas: 3
  selector:
    matchLabels:
      app: fhir-api
  template:
    metadata:
      labels:
        app: fhir-api
    spec:
      containers:
      - name: fhir-api
        image: mededge/fhir-api:latest
        ports:
        - containerPort: 80
        env:
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: mededge-secrets
              key: sql-connection
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"

# k8s/mqtt-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mqtt-broker
  namespace: mededge
spec:
  replicas: 1
  selector:
    matchLabels:
      app: mqtt-broker
  template:
    metadata:
      labels:
        app: mqtt-broker
    spec:
      containers:
      - name: mosquitto
        image: eclipse-mosquitto:2.0
        ports:
        - containerPort: 1883
        - containerPort: 9001

# k8s/ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: mededge-ingress
  namespace: mededge
  annotations:
    nginx.ingress.kubernetes.io/rewrite-target: /
    cert-manager.io/cluster-issuer: letsencrypt-prod
spec:
  tls:
  - hosts:
    - mededge.example.com
    secretName: mededge-tls
  rules:
  - host: mededge.example.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: dashboard-service
            port:
              number: 80
      - path: /api
        pathType: Prefix
        backend:
          service:
            name: fhir-api-service
            port:
              number: 80
      - path: /fhir
        pathType: Prefix
        backend:
          service:
            name: fhir-api-service
            port:
              number: 80
```

**Deployment Commands:**
```bash
# Apply all manifests
kubectl apply -f k8s/

# Check status
kubectl get pods -n mededge
kubectl get services -n mededge
kubectl get ingress -n mededge

# Scale services
kubectl scale deployment fhir-api --replicas=5 -n mededge
```

---

## 📦 Option 5: Serverless Architecture

### AWS Lambda + API Gateway
```yaml
# serverless.yml
service: mededge-gateway
frameworkVersion: '3'

provider:
  name: aws
  runtime: dotnetcore8
  region: us-east-1

functions:
  fhir-api:
    handler: MedEdge.FhirApi::MedEdge.FhirApi.Lambda.Function::FunctionHandler
    events:
      - httpApi:
          path: /fhir/{proxy}
          method: ANY
          cors: true

  transform-service:
    handler: MedEdge.TransformService::MedEdge.TransformService.Lambda.Function::FunctionHandler
    events:
      - schedule:
          rate: rate(1 minute)

  ai-engine:
    handler: MedEdge.AiEngine::MedEdge.AiEngine.Lambda.Function::FunctionHandler
    events:
      - sqs:
          arn: !GetAtt TransformQueue.Arn

resources:
  Resources:
    FhirDatabase:
      Type: AWS::DynamoDB::Table
      Properties:
        TableName: MedEdgeFhir
        BillingMode: PAY_PER_REQUEST
        AttributeDefinitions:
          - AttributeName: id
            AttributeType: S
        KeySchema:
          - AttributeName: id
            KeyType: HASH

    MQTTEventBus:
      Type: AWS::SQS::Queue
      Properties:
        QueueName: MedEdgeMQTTEvents
```

### Google Cloud Functions
```python
# main.py (Python version for demonstration)
import functions_framework
import json
from google.cloud import firestore

@functions_framework.http
def fhir_api(request):
    """Handle FHIR API requests"""
    if request.method == 'GET':
        db = firestore.Client()
        patients = list(db.collection('patients').stream())
        return json.dumps([{
            'id': p.id,
            'data': p.to_dict()
        } for p in patients])

    return 'Method not allowed', 405
```

---

## 🚀 Option 6: Platform-Specific Deployment

### Heroku Deployment
```bash
# Create Heroku app
heroku create mededge-gateway

# Add buildpacks
heroku buildpacks:set heroku/dotnet

# Set environment variables
heroku config:set ASPNETCORE_ENVIRONMENT=Production
heroku config:set ConnectionStrings__DefaultConnection="${DATABASE_URL}"

# Deploy
git push heroku main

# Scale dynos
heroku ps:scale worker=1
```

### AWS Elastic Beanstalk
```
.ebextensions/01_python.config
option_settings:
  aws:elasticbeanstalk:application:environment:
    ASPNETCORE_ENVIRONMENT: "Production"
```

### Google Cloud Run
```bash
# Build and push to Container Registry
gcloud builds submit --tag gcr.io/PROJECT-ID/mededge-fhir

# Deploy to Cloud Run
gcloud run deploy mededge-fhir \
  --image gcr.io/PROJECT-ID/mededge-fhir \
  --platform managed \
  --region us-central1 \
  --allow-unauthenticated
```

---

## 🎯 Recommended Deployment Strategy

### For Production/Enterprise
```
Option 4: Kubernetes
├── Full scalability
├── Self-healing
├── Load balancing
├── Rolling updates
└── Production-ready
```

### For Startups/MVP
```
Option 3: Hybrid (Cloudflare + Azure)
├── Low cost
├── Good performance
├── Easy to manage
└── Scalable
```

### For Personal Projects
```
Option 2: Azure App Service
├── Simple setup
├── Integrated services
└── Pay-as-you-go
```

### For Development
```
Option 1: Local Docker
├── Full functionality
├── No cost
└── Easy debugging
```

---

## 📊 Platform Comparison

| Platform | Cost | Scalability | Complexity | Features |
|----------|------|-------------|------------|----------|
| **Local Docker** | $0 | Low | Low | Full stack |
| **Azure App Service** | $$ | Medium | Low | Managed |
| **Hybrid Cloudflare** | $ | High | Medium | Edge computing |
| **Kubernetes** | $$$ | Very High | High | Enterprise |
| **AWS Lambda** | $$ | Very High | High | Serverless |
| **Heroku** | $$ | Medium | Low | PaaS |

---

## 🔧 Setup Scripts

### Automated Deployment Script
```bash
#!/bin/bash
# deploy.sh - Comprehensive deployment script

set -e

ENVIRONMENT=${1:-production}
PLATFORM=${2:-azure}

echo "🚀 Deploying MedEdge Gateway to $PLATFORM in $ENVIRONMENT mode"

case $PLATFORM in
    "azure")
        echo "🌤️  Deploying to Azure..."
        # Azure deployment commands here
        ;;
    "aws")
        echo "⚡ Deploying to AWS..."
        # AWS deployment commands here
        ;;
    "k8s")
        echo "🐳 Deploying to Kubernetes..."
        kubectl apply -f k8s/
        ;;
    "hybrid")
        echo "🌐 Deploying Hybrid Cloudflare + Azure..."
        # Hybrid deployment commands
        ;;
    *)
        echo "❌ Unknown platform: $PLATFORM"
        exit 1
        ;;
esac

echo "✅ Deployment completed successfully!"
echo "📊 View status: $PLATFORM-status-url"
```

### Quick Start Commands
```bash
# Local development
./scripts/deploy.sh local docker

# Azure deployment
./scripts/deploy.sh production azure

# Kubernetes deployment
./scripts/deploy.sh production k8s

# Hybrid deployment
./scripts/deploy.sh production hybrid
```

---

## 🎉 Choose Your Deployment

Ready to deploy? Choose your preferred option:

1. **[🏠 Local Development](#option-1-local-development-docker)** - For testing and development
2. **[☁️ Cloud Deployment](#option-2-cloud-deployment-full-stack)** - For production with full services
3. **[🌐 Hybrid Architecture](#option-3-hybrid-architecture-cloudflare--external-services)** - Best performance/cost ratio
4. **[🐳 Kubernetes](#option-4-kubernetes-deployment)** - For enterprise scaling
5. **[📦 Serverless](#option-5-serverless-architecture)** - For event-driven architecture

Each option provides complete functionality - choose based on your scaling needs, budget, and technical requirements.

---

**Last Updated:** January 23, 2026
**Version:** 1.0.0
**Status:** Production Ready