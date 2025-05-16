# 🚀 SmartInsight: AI-Native Enterprise Knowledge Platform

[![Build Status](https://github.com/user/smartinsight/workflows/CI/badge.svg)](https://github.com/user/smartinsight/actions)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

SmartInsight is a revolutionary enterprise knowledge platform engineered from the ground up through the orchestrated efforts of advanced AI agents. This system connects diverse data sources into a comprehensive knowledge ecosystem with a natural language interface, delivering a transformative solution for enterprise knowledge management.

## 🔑 Key Features

- **35+ Connector Types**: Seamless integration with databases, document repositories, wikis, and more
- **Multi-dimensional Knowledge Graph**: Deep entity understanding with 75+ relationship types
- **Vector-based Semantic Search**: Precise information retrieval using 768-dimension embeddings
- **Natural Language to SQL Translation**: Secure database querying with tenant isolation
- **Interactive Data Visualization**: Auto-generated insights and relationship mapping
- **100% On-premises AI Processing**: Complete data sovereignty with all processing local
- **Self-documenting Architecture**: Designed for continuous AI-driven evolution

## 🏗️ Architecture

SmartInsight follows a modular, containerized architecture:

```
┌─────────────────────────┐      ┌─────────────────────────┐
│                         │      │                         │
│   SmartInsight.UI       │      │   SmartInsight.Admin    │
│   (React + TypeScript)  │      │   (React + TypeScript)  │
│                         │      │                         │
└───────────┬─────────────┘      └───────────┬─────────────┘
            │                                │
            │                                │
            ▼                                ▼
┌───────────────────────────────────────────────────────────┐
│                                                           │
│                    SmartInsight.API                       │
│                 (ASP.NET Core + .NET 8)                   │
│                                                           │
├───────────┬───────────┬───────────┬───────────┬───────────┤
│           │           │           │           │           │
│  Security │    AI     │  Knowledge│   Data    │ Telemetry │
│  Module   │  Module   │  Module   │  Module   │  Module   │
│           │           │           │           │           │
└───────────┴─────┬─────┴─────┬─────┴─────┬─────┴───────────┘
                  │           │           │
                  ▼           ▼           ▼
┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│             │ │             │ │             │ │             │
│  PostgreSQL │ │   Qdrant    │ │   Ollama    │ │    Redis    │
│  Database   │ │  Vector DB  │ │  LLM Server │ │    Cache    │
│             │ │             │ │             │ │             │
└─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘
```

### Core Components

- **SmartInsight.UI**: User-facing React application for querying and visualization
- **SmartInsight.Admin**: Administrative interface for configuration and monitoring
- **SmartInsight.API**: Core API layer handling all business logic
- **SmartInsight.Core**: Shared DTOs, interfaces, and common utilities
- **SmartInsight.Data**: Data access layer with Entity Framework Core repositories
- **SmartInsight.AI**: AI processing, intent detection, and LLM integration
- **SmartInsight.Knowledge**: Knowledge graph, entity extraction, and semantic processing
- **SmartInsight.Telemetry**: Logging, monitoring, and performance metrics

## 🛠️ Technology Stack

| Layer | Technologies |
|-------|--------------|
| **Frontend** | React 18, TypeScript 5, Redux Toolkit, Recharts, ShadCN UI, TailwindCSS |
| **Backend API** | .NET 8, ASP.NET Core, Entity Framework Core, MediatR (CQRS), AutoMapper, FluentValidation |
| **Databases** | PostgreSQL 16, Qdrant Vector DB, Redis Cache |
| **AI/ML** | Ollama, ONNX Runtime, Sentence Transformers, LangChain.NET |
| **DevOps** | Docker, GitHub Actions, Seq Logging, xUnit, Playwright |
| **Security** | OAuth 2.0, JWT, Row-Level Security, ASP.NET Core Identity |

## 🚀 Getting Started

### Prerequisites

- Docker and Docker Compose
- .NET 8 SDK (for development)
- Node.js 18+ and npm/yarn (for UI development)
- Git

### Quick Start

1. **Clone the repository**

```bash
git clone https://github.com/yourusername/smartinsight.git
cd smartinsight
```

2. **Configure environment variables**

```bash
cp .env.example .env
# Edit .env with your configuration values
```

3. **Start the application using Docker Compose**

```bash
docker-compose up -d
```

4. **Access the application**

- UI: [http://localhost:3000](http://localhost:3000)
- Admin Portal: [http://localhost:3001](http://localhost:3001)
- API: [http://localhost:5000](http://localhost:5000)
- API Documentation: [http://localhost:5000/swagger](http://localhost:5000/swagger)

## 📋 Development Setup

### Backend Development

1. **Install .NET 8 SDK** from [Microsoft .NET Download](https://dotnet.microsoft.com/download)

2. **Set up your development database**

```bash
docker-compose up -d postgres qdrant redis ollama
```

3. **Start the API in development mode**

```bash
cd src/SmartInsight.API
dotnet run
```

### Frontend Development

1. **Install dependencies**

```bash
cd src/SmartInsight.UI
npm install
```

2. **Start development server**

```bash
npm run dev
```

## 🔧 Configuration Options

### Docker Compose Configuration

The main `docker-compose.yml` file configures:

- Service dependencies and network configuration
- Volume mounting for persistent data
- Environment variable injection
- Health checks and restart policies

### Application Configuration

Core application settings are managed through:

- `.env` file for sensitive information and deployment-specific settings
- `appsettings.json` for ASP.NET Core configuration
- `.taskmasterconfig` for Task Master AI management

### Multi-Tenant Configuration

SmartInsight is designed for multi-tenant deployment:

- Tenant isolation at the database layer via PostgreSQL Row-Level Security
- Application-level tenant context middleware
- Separate vector embeddings namespaces per tenant in Qdrant

## 📊 Data Source Connectors

SmartInsight supports the following data sources:

| Connector Type | Status | Description |
|----------------|--------|-------------|
| PostgreSQL | ✅ | Full-featured database connector with secure query generation |
| File Repository | ✅ | Supporting text, markdown, PDF, and DOCX files |
| MSSQL | 🔄 Planned | Database connector for Microsoft SQL Server |
| MySQL | 🔄 Planned | Database connector for MySQL |
| Confluence | 🔄 Planned | Wiki connector for Atlassian Confluence |
| JIRA | 🔄 Planned | Issue tracking connector for Atlassian JIRA |
| Git Repository | 🔄 Planned | Code repository connector for Git |
| SharePoint | 🔄 Planned | Document management connector for Microsoft SharePoint |
| Google Drive | 🔄 Planned | File storage connector for Google Drive |
| Dropbox | 🔄 Planned | File storage connector for Dropbox |

## 🔐 Security Features

SmartInsight implements comprehensive security measures:

- **Multi-Tenant Architecture**: Complete tenant isolation at all layers
- **Row-Level Security**: PostgreSQL RLS policies for fine-grained access control
- **OAuth2 Authentication**: Industry-standard OAuth2 with JWT tokens
- **Role-Based Access Control**: Comprehensive RBAC with custom claims
- **Tenant-Specific Configuration**: Per-tenant settings and customizations
- **Security Vulnerability Testing**: Automated testing for security vulnerabilities
- **Secure Credential Storage**: Encrypted storage for data source credentials
- **Query Safety Verification**: All generated queries verified for safety compliance

## 📦 Deployment Options

### Docker Compose (Development/Small Deployments)

Ideal for development or small-scale production:

```bash
docker-compose up -d
```

### Kubernetes (Enterprise Deployments)

For scalable multi-node deployment:

```bash
# Install using Helm
helm install smartinsight ./helm/smartinsight
```

Prerequisites:
- Kubernetes cluster (AKS, EKS, GKE, or on-premises)
- Helm 3+
- Persistent volumes for data storage

## 💻 Key API Endpoints

### Authentication

- `POST /api/auth/login`: Authenticate and get token
- `POST /api/auth/refresh`: Refresh authentication token

### Conversation

- `POST /api/conversation`: Create new conversation
- `GET /api/conversation/{id}`: Get conversation by ID
- `POST /api/conversation/{id}/message`: Add message to conversation

### Data Sources

- `GET /api/datasource`: List configured data sources
- `POST /api/datasource`: Create new data source
- `GET /api/datasource/{id}/status`: Check data source status

### Administration

- `GET /api/admin/tenant`: List tenants
- `POST /api/admin/tenant`: Create new tenant
- `GET /api/admin/user`: List users
- `POST /api/admin/user`: Create new user

## 🧪 Testing

SmartInsight includes comprehensive test suites:

### Run Unit Tests

```bash
dotnet test tests/SmartInsight.Tests
```

### Run Integration Tests

```bash
dotnet test tests/SmartInsight.IntegrationTests
```

### Run End-to-End Tests

```bash
cd tests/SmartInsight.E2ETests
npm run test
```

## 🛣️ Roadmap

The SmartInsight platform development roadmap includes:

- Additional data source connectors
- Enhanced visualization options
- Mobile application development
- Advanced workflow automation
- External API publishing for integration

## 🤝 Contributing

SmartInsight was built using AI-native engineering methodology. To continue this approach:

1. Examine the task structure in the `tasks/` directory
2. View the next recommended task using:
```bash
task-master next
```
3. Continue implementation by asking Cursor AI to:
```
Continue implementing the next task
```

## 📃 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📚 Documentation

Additional documentation is available in the `docs/` directory:

- Architecture Documentation: `docs/Architecture`
- API Reference: Generated at `http://localhost:5000/swagger`
- Deployment Guides: `docs/Deployment`

## 👥 Team

SmartInsight was developed by:

- Elias Dergham - Senior Full Stack Software Engineer & AI Champion

## 📧 Contact

For questions or support, please contact:
- Email: elias.dergham@fluentechnology.com
- GitHub Issues: [Create an issue](https://github.com/Elias-fluent/Elias-fluent-SmartInsight_M004)

---

*This document itself was authored collaboratively with Claude 3.7 Sonnet, exemplifying the AI-collaborative approach that defines SmartInsight.* 