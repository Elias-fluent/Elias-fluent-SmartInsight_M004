# SmartInsight CI/CD Pipeline

This document describes the Continuous Integration and Continuous Deployment (CI/CD) pipeline for the SmartInsight project.

## Pipeline Overview

The SmartInsight CI/CD pipeline is implemented using GitHub Actions and consists of the following stages:

1. **Build and Test**: Compiles the application and runs all tests
2. **Code Analysis**: Performs static code analysis with SonarCloud
3. **Security Scan**: Executes OWASP Dependency Check for vulnerabilities
4. **Docker Build**: Creates and pushes Docker images to GitHub Container Registry
5. **Deployment**: Deploys to development or production environments based on branch

## Workflow Files

The CI/CD pipeline is defined in two main workflow files:

- **[ci-cd.yml](.github/workflows/ci-cd.yml)**: Main CI/CD pipeline triggered on push to main/develop
- **[pr-validation.yml](.github/workflows/pr-validation.yml)**: PR validation workflow for GitFlow enforcement

## GitFlow Workflow

The project follows GitFlow branching model:

1. **main**: Production-ready code
2. **develop**: Integration branch for features
3. **feature/**: Feature branches (branched from develop)
4. **bugfix/**: Bug fix branches (branched from develop)
5. **release/**: Release branches (branched from develop)
6. **hotfix/**: Hotfix branches (branched from main)

### Pull Request Rules

- All code changes must be made through Pull Requests
- PRs must follow the semantic naming convention (e.g., `feat: add user authentication`)
- PRs require passing build, tests, and code quality checks
- PRs must maintain minimum 80% code coverage
- PRs must have at least one approval before merging

## Deployment Strategy

The CI/CD pipeline automatically deploys to environments based on branch:

- **develop branch**: Auto-deployment to Development environment
- **main branch**: Deployment to Production environment (requires approval)

## Docker Infrastructure

The application is containerized using Docker with a multi-stage build process:

1. **Build stage**: Compiles .NET application
2. **UI Build stage**: Builds React SPA
3. **Runtime stage**: Final production image

Docker Compose is used to define the complete infrastructure including:

- **API**: SmartInsight API service
- **PostgreSQL**: Primary database
- **Qdrant**: Vector database for knowledge graph
- **Ollama**: Local LLM inference
- **Seq**: Centralized logging and monitoring

## Environment Variables

The pipeline uses the following environment variables/secrets:

- **GITHUB_TOKEN**: Automatically provided by GitHub Actions
- **SONAR_TOKEN**: SonarCloud authentication
- **DEV_SSH_PRIVATE_KEY**: SSH key for development server
- **DEV_SERVER_USER**: Username for development server
- **DEV_SERVER_HOST**: Hostname for development server
- **PROD_SSH_PRIVATE_KEY**: SSH key for production server
- **PROD_SERVER_USER**: Username for production server
- **PROD_SERVER_HOST**: Hostname for production server

## Local Development

To run the full infrastructure locally:

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f api

# Stop all services
docker-compose down
```

## Security Considerations

- All secrets are stored in GitHub Secrets and not exposed in code
- OWASP Dependency Check is run on every build
- Production deployments require manual approval
- Docker images are regularly scanned for vulnerabilities

## Monitoring and Logging

- **Seq**: Web UI available at http://localhost:8081
- All application logs are collected and searchable through Seq
- Errors and warnings trigger notifications to the development team

## Continuous Improvement

The CI/CD pipeline is continuously improved through:

1. Regular review of build times and optimization where necessary
2. Updates to security scanning tools and libraries
3. Expansion of test coverage
4. Automation of manual processes

# CI/CD Pipeline Configuration

This document provides instructions for configuring the CI/CD pipeline for the SmartInsight project.

## Required Secrets

The following secrets need to be configured in your GitHub repository:

### GitHub Secrets

1. **SONAR_TOKEN**
   - Go to [SonarCloud](https://sonarcloud.io/)
   - Log in with your GitHub account
   - Navigate to My Account > Security
   - Generate a new token with the necessary permissions
   - Add this token as a repository secret named `SONAR_TOKEN`

2. **CODECOV_TOKEN**
   - Go to [Codecov](https://codecov.io/)
   - Log in with your GitHub account
   - Find your project and navigate to Settings
   - Copy the upload token
   - Add this token as a repository secret named `CODECOV_TOKEN`

3. **Deployment Secrets** (for production/development environments)
   - `DEV_SSH_PRIVATE_KEY` - SSH private key for development server
   - `DEV_SERVER_USER` - Username for development server
   - `DEV_SERVER_HOST` - Hostname for development server
   - `PROD_SSH_PRIVATE_KEY` - SSH private key for production server
   - `PROD_SERVER_USER` - Username for production server
   - `PROD_SERVER_HOST` - Hostname for production server

## SonarCloud Configuration

1. Create a new project in SonarCloud
2. Set the organization to `elias-fluent`
3. Set the project key to `SmartInsight`
4. The GitHub Actions workflow is configured to automatically run SonarCloud scans

## Pipeline Overview

The CI/CD pipeline includes the following jobs:

1. **Build and Test** - Builds the solution and runs tests
2. **Code Analysis** - Runs SonarCloud code analysis
3. **Security Scan** - Performs dependency scanning for vulnerabilities
4. **Docker Build** - Builds and pushes Docker images
5. **Deploy to Development** - Deploys to the development environment
6. **Deploy to Production** - Deploys to the production environment (requires manual approval)

## Troubleshooting

If the SonarCloud scan fails with "Failed to query JRE metadata", ensure that:
1. The SONAR_TOKEN secret is correctly configured in your GitHub repository
2. The organization and project key in the workflow file match your SonarCloud configuration
3. The SonarCloud project is properly set up with the correct visibility settings 