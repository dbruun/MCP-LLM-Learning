# GitHub Workflows Documentation

This directory contains GitHub Actions workflows for automated building, testing, and deployment of the MCP-LLM-Learning application to Azure.

## 📋 Table of Contents

- [Overview](#overview)
- [Workflows](#workflows)
- [Setup & Configuration](#setup--configuration)
- [Usage](#usage)
- [Architecture](#architecture)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## 🎯 Overview

The workflows in this repository provide a complete CI/CD pipeline with support for:
- **Multi-environment deployments** (Development, Staging, Production)
- **Azure OIDC authentication** (no stored credentials)
- **Automated tagging** with PCM compliance metadata
- **Reusable workflow templates** for consistency
- **Build artifacts** with version management
- **Automated resource provisioning** in Azure

## 📂 Workflows

### 1. `deploy.yml` - Main Deployment Pipeline

**Trigger:**
- Push to `main` branch
- Manual dispatch (`workflow_dispatch`)

**Purpose:**
Main deployment pipeline that builds the application, creates Azure resources with compliance tags, and deploys to production.

**Key Features:**
- ✅ Builds and publishes the .NET application
- ✅ Creates/updates Azure Storage Account with 50+ compliance tags
- ✅ Validates PCM project numbers (RITM format)
- ✅ Applies committer email and deployment metadata
- ✅ Calls reusable deployment template

**Inputs:**
- `project_number`: PCM Project Number (default: `RITM1185178`)

**Required Secrets:**
- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `AZURE_RESOURCE_GROUP`
- `AZURE_STORAGE_NAME`

**Example Compliance Tags Applied:**
```yaml
pcm-application_name_sysid: placeholder
pcm-built_by: user@example.com
pcm-business_service_sysid: SAVNS
pcm-project_number: RITM1185178
pcm-sensitive_data_cjis: yes
pcm-sensitive_data_pii: yes
deployed-by: github-username
deployment-timestamp: 1729468800
github-run-id: 123456789
```

**Workflow Structure:**
```yaml
Jobs:
  1. build-and-deploy
     - Checkout code
     - Setup .NET 8.0
     - Restore & Build
     - Publish MCPCopilotUI
     - Login to Azure (OIDC)
     - Create/Update Storage with tags
  
  2. deploy-prod
     - Calls deploymentTemplate.yml
```

---

### 2. `orchestrator.yml` - Multi-Environment Orchestrator

**Trigger:**
- Called by other workflows (`workflow_call`)

**Purpose:**
Reusable orchestrator workflow that routes deployments to the appropriate environment-specific template based on input parameters.

**Key Features:**
- ✅ Single source of truth for build & test logic
- ✅ Environment-based routing (dev/staging/prod)
- ✅ Version management (automatic or manual)
- ✅ Artifact caching and management
- ✅ Optional test execution
- ✅ NuGet package caching
- ✅ Output deployment URL and status

**Inputs:**
```yaml
environment:        # Required: 'dev', 'staging', or 'prod'
project_number:     # Optional: PCM RITM ticket (default: RITM1185178)
skip_tests:         # Optional: Skip test execution (default: false)
version:            # Optional: Application version (default: auto-generated)
```

**Secrets:**
```yaml
azure_client_id
azure_tenant_id
azure_subscription_id
azure_resource_group
azure_webapp_name
azure_storage_name
```

**Outputs:**
```yaml
deployment_url:     # URL of deployed application
deployment_status:  # Status of deployment
```

**Workflow Structure:**
```yaml
Jobs:
  1. build-and-test
     - Checkout & setup
     - Generate version (1.0.{run_number})
     - Cache NuGet packages
     - Restore, build, test
     - Publish application
     - Upload artifacts
  
  2. route-deployment
     - Set environment-specific outputs
  
  3. deploy-dev (conditional)
     - Calls template-deploy-dev.yml
  
  4. deploy-staging (conditional)
     - Calls template-deploy-staging.yml
  
  5. deploy-production (conditional)
     - Calls template-deploy-prod.yml
```

**Environment URLs:**
- **Development**: `https://dev-mcpcopilot.azurewebsites.net`
- **Staging**: `https://staging-mcpcopilot.azurewebsites.net`
- **Production**: `https://mcpcopilot.azurewebsites.net`

---

### 3. `deploymentTemplate.yml` - Simple Reusable Template

**Trigger:**
- Called by other workflows (`workflow_call`)

**Purpose:**
Basic reusable template demonstrating workflow composition.

**Inputs:**
- `environment`: Target environment (required)
- `app_name`: Application name (required)

**Secrets:**
- `azure_client_id`: Azure AD app registration client ID (required)
- `azure_tenant_id`: Azure AD tenant ID (required)
- `azure_subscription_id`: Azure subscription ID (required)

**Current Implementation:**
```yaml
- Checkout code
- Login to Azure via OIDC
- Echo deployment message
```

**Usage Example:**
```yaml
uses: ./.github/workflows/deploymentTemplate.yml
with:
  environment: production
  app_name: my-app
secrets:
  azure_client_id: ${{ secrets.AZURE_CLIENT_ID }}
  azure_tenant_id: ${{ secrets.AZURE_TENANT_ID }}
  azure_subscription_id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
```

---

### 4. Environment-Specific Templates

**Files:**
- `template-deploy-dev.yml`
- `template-deploy-staging.yml`
- `template-deploy-prod.yml`

**Purpose:**
Environment-specific deployment logic with appropriate configurations, approval gates, and resource settings.

**Common Structure:**
```yaml
Inputs:
  - artifact_name
  - version
  - project_number

Secrets:
  - azure_client_id
  - azure_tenant_id
  - azure_subscription_id
  - azure_resource_group
  - azure_webapp_name
  - azure_storage_name

Jobs:
  1. Download artifact
  2. Azure login (OIDC)
  3. Deploy to environment-specific Web App
  4. Apply environment-specific tags
  5. Run smoke tests
```

---

## ⚙️ Setup & Configuration

### 1. Azure Setup

#### Create Azure AD App Registration (for OIDC)

```bash
# Create app registration
az ad app create --display-name "GitHub-MCP-LLM-Learning"

# Get the app ID
APP_ID=$(az ad app list --display-name "GitHub-MCP-LLM-Learning" --query [0].appId -o tsv)

# Create service principal
az ad sp create --id $APP_ID

# Get service principal object ID
SP_OBJECT_ID=$(az ad sp show --id $APP_ID --query id -o tsv)

# Assign Contributor role to subscription
az role assignment create \
  --role Contributor \
  --subscription <SUBSCRIPTION_ID> \
  --assignee-object-id $SP_OBJECT_ID \
  --assignee-principal-type ServicePrincipal
```

#### Configure Federated Credentials

```bash
# For main branch
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "GitHub-Main-Branch",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:dbruun/MCP-LLM-Learning:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# For pull requests
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "GitHub-Pull-Requests",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:dbruun/MCP-LLM-Learning:pull_request",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

### 2. GitHub Secrets Setup

Navigate to **Settings → Secrets and variables → Actions** and add:

| Secret Name | Description | Example |
|------------|-------------|---------|
| `AZURE_CLIENT_ID` | App registration client ID | `a1b2c3d4-e5f6-...` |
| `AZURE_TENANT_ID` | Azure AD tenant ID | `12345678-abcd-...` |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID | `87654321-dcba-...` |
| `AZURE_RESOURCE_GROUP` | Resource group name | `rg-mcpcopilot-prod` |
| `AZURE_WEBAPP_NAME` | Web app name | `mcpcopilot-prod` |
| `AZURE_STORAGE_NAME` | Storage account name | `mcpcopilotprod` |

### 3. Azure Resources

Create the required Azure resources:

```bash
# Set variables
RESOURCE_GROUP="rg-mcpcopilot-prod"
LOCATION="eastus"
WEBAPP_NAME="mcpcopilot-prod"
STORAGE_NAME="mcpcopilotprod"
APP_SERVICE_PLAN="asp-mcpcopilot-prod"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create App Service Plan
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux

# Create Web App
az webapp create \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "DOTNETCORE:8.0"

# Create Storage Account
az storage account create \
  --name $STORAGE_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS
```

---

## 🚀 Usage

### Deploy to Production (Automatic)

Push to the `main` branch:

```bash
git add .
git commit -m "Deploy to production"
git push origin main
```

### Deploy with Custom Project Number

Use manual workflow dispatch:

1. Go to **Actions** tab
2. Select **"Check Build and Deploy to Azure"**
3. Click **"Run workflow"**
4. Enter project number (e.g., `RITM1234567`)
5. Click **"Run workflow"**

### Deploy to Specific Environment

Call the orchestrator from another workflow:

```yaml
jobs:
  deploy-staging:
    uses: ./.github/workflows/orchestrator.yml
    with:
      environment: staging
      project_number: RITM1234567
      skip_tests: false
    secrets:
      azure_client_id: ${{ secrets.AZURE_CLIENT_ID }}
      azure_tenant_id: ${{ secrets.AZURE_TENANT_ID }}
      azure_subscription_id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
      azure_resource_group: ${{ secrets.AZURE_RESOURCE_GROUP_STAGING }}
      azure_webapp_name: ${{ secrets.AZURE_WEBAPP_NAME_STAGING }}
      azure_storage_name: ${{ secrets.AZURE_STORAGE_NAME_STAGING }}
```

---

## 🏗️ Architecture

### Workflow Dependency Graph

```
deploy.yml (Main Branch Push)
  │
  ├─► build-and-deploy job
  │   ├─► Build .NET app
  │   ├─► Login to Azure (OIDC)
  │   └─► Create Storage + Tags
  │
  └─► deploy-prod job
      └─► deploymentTemplate.yml
          └─► Deploy to production

orchestrator.yml (Reusable)
  │
  ├─► build-and-test job
  │   ├─► Build & Test
  │   └─► Upload artifacts
  │
  ├─► route-deployment job
  │   └─► Set environment URLs
  │
  ├─► deploy-dev job (conditional)
  │   └─► template-deploy-dev.yml
  │
  ├─► deploy-staging job (conditional)
  │   └─► template-deploy-staging.yml
  │
  └─► deploy-production job (conditional)
      └─► template-deploy-prod.yml
```

### Authentication Flow

```
GitHub Actions
    │
    ├─► Request OIDC token
    │   └─► Subject: repo:owner/repo:ref:refs/heads/main
    │
    ├─► Exchange token with Azure AD
    │   ├─► Federated credential validation
    │   └─► Issue Azure AD token
    │
    └─► Access Azure resources
        └─► Contributor role on subscription
```

---

## 🎯 Best Practices

### Security

✅ **Use OIDC authentication** (no stored credentials)  
✅ **Federated credentials** for specific branches/PRs  
✅ **Least privilege** - assign minimal required roles  
✅ **Secrets rotation** - regularly rotate service principals  
✅ **Environment protection** - enable approvals for production

### Workflow Design

✅ **Reusable workflows** - DRY principle  
✅ **Environment variables** - centralize configuration  
✅ **Conditional execution** - skip unnecessary jobs  
✅ **Artifact caching** - speed up builds  
✅ **Job outputs** - pass data between jobs

### Tagging Strategy

✅ **Compliance tags** - PCM requirements  
✅ **Metadata tags** - deployment tracking  
✅ **Version tags** - application versions  
✅ **Environment tags** - env identification

### Version Management

```bash
# Automatic versioning
version: 1.0.${{ github.run_number }}

# Semantic versioning
version: 2.1.3

# Date-based versioning
version: 2024.10.20.${{ github.run_number }}
```

---

## 🔧 Troubleshooting

### Common Issues

#### 1. OIDC Authentication Failure

**Error:**
```
Error: Login failed with Error: Unable to get OIDC token
```

**Solution:**
- Verify federated credential subject matches: `repo:owner/repo:ref:refs/heads/main`
- Check `id-token: write` permission is set
- Ensure app registration has correct audience: `api://AzureADTokenExchange`

#### 2. Storage Account Already Exists

**Error:**
```
The storage account named 'xxx' is already taken
```

**Solution:**
- Storage names must be globally unique
- Use a unique naming convention: `{company}{app}{env}{random}`
- Example: `contosomcp01prod`

#### 3. Project Number Validation Failure

**Error:**
```
Error: Project number must start with 'RITM'
```

**Solution:**
- Ensure project number format: `RITM1234567`
- Check workflow input or default value
- Validate regex: `^RITM`

#### 4. Build Artifacts Not Found

**Error:**
```
Error: Unable to find artifact 'mcpcopilotui-prod-v1.0.123'
```

**Solution:**
- Verify artifact upload in build job
- Check artifact retention (default: 30 days)
- Ensure artifact name matches between upload/download

#### 5. Port Already in Use

**Error:**
```
IOException: Failed to bind to address http://127.0.0.1:5172
```

**Solution:**
```bash
# Find process using port
netstat -ano | findstr :5172

# Kill process
taskkill /PID <process_id> /F

# Or use different port in launchSettings.json
```

### Debug Mode

Enable debug logging in workflows:

```yaml
env:
  ACTIONS_STEP_DEBUG: true
  ACTIONS_RUNNER_DEBUG: true
```

### Manual Rollback

```bash
# List deployments
az webapp deployment list --name $WEBAPP_NAME --resource-group $RESOURCE_GROUP

# Rollback to previous deployment
az webapp deployment slot swap \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --slot staging \
  --target-slot production
```

---

## 📊 Monitoring & Logging

### View Workflow Runs

1. Go to **Actions** tab in GitHub
2. Select workflow run
3. Click on job to view logs
4. Download artifacts if needed

### Azure Application Insights

```bash
# Enable Application Insights
az monitor app-insights component create \
  --app mcpcopilot-insights \
  --location eastus \
  --resource-group $RESOURCE_GROUP

# Link to Web App
az webapp config appsettings set \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=..."
```

---

## 🔄 Workflow Maintenance

### Update Workflow Dependencies

```yaml
# Update actions versions periodically
- uses: actions/checkout@v4          # Check for v5
- uses: actions/setup-dotnet@v3      # Check for v4
- uses: azure/login@v2               # Check for v3
```

### Archive Old Workflow Runs

GitHub automatically archives runs after 90 days. To manually delete:

```bash
# Using GitHub CLI
gh run list --limit 100 | \
  awk '{print $7}' | \
  xargs -I {} gh run delete {}
```

---

## 📚 Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure OIDC Setup Guide](https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure)
- [Reusable Workflows](https://docs.github.com/en/actions/using-workflows/reusing-workflows)
- [Azure CLI Reference](https://learn.microsoft.com/en-us/cli/azure/)
- [Workflow Syntax](https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions)

---

## 🤝 Contributing

When adding new workflows:

1. Follow the naming convention: `{purpose}-{environment}.yml`
2. Add comprehensive comments
3. Include error handling
4. Document required secrets
5. Update this README
6. Test in a feature branch first

---

**Last Updated:** October 20, 2025  
**Maintained By:** GitHub Actions Team
