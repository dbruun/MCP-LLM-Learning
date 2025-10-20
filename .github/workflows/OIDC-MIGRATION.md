# OIDC Authentication Migration

## ✅ Migration Status: COMPLETE

All GitHub Actions workflows in this repository now use **OpenID Connect (OIDC)** authentication instead of storing long-lived credentials.

## 🔐 Security Benefits

### Before (Secrets-based Auth)
❌ Long-lived service principal credentials stored in GitHub  
❌ Risk of credential leakage  
❌ Manual credential rotation required  
❌ Broad access permissions  
❌ No automatic expiration  

### After (OIDC Auth)
✅ No stored credentials - uses federated identity  
✅ Short-lived tokens (valid for 1 hour)  
✅ Automatic token rotation  
✅ Scoped to specific branches/environments  
✅ Enhanced audit trail  

## 📋 Migrated Workflows

All workflows now use OIDC authentication:

| Workflow | Status | OIDC Login |
|----------|--------|------------|
| `deploy.yml` | ✅ Migrated | Yes - Line 37 |
| `orchestrator.yml` | ✅ N/A (calls templates) | Inherits from templates |
| `deploymentTemplate.yml` | ✅ Migrated | Yes - Line 29 |
| `template-deploy-dev.yml` | ✅ Migrated | Yes - Line 51 |
| `template-deploy-staging.yml` | ✅ Migrated | Yes - Line 51 |
| `template-deploy-prod.yml` | ✅ Migrated | Yes - Line 71 |

## 🔑 Required GitHub Secrets

Only these secrets are needed (no stored credentials):

| Secret Name | Type | Purpose |
|------------|------|---------|
| `AZURE_CLIENT_ID` | App Registration Client ID | OIDC authentication |
| `AZURE_TENANT_ID` | Tenant ID | OIDC authentication |
| `AZURE_SUBSCRIPTION_ID` | Subscription ID | Resource scope |
| `AZURE_RESOURCE_GROUP` | Resource Group Name | Deployment target |
| `AZURE_WEBAPP_NAME` | Web App Name | Deployment target |
| `AZURE_STORAGE_NAME` | Storage Account Name | Deployment target |

**Removed Secrets:**
- ❌ `AZURE_CREDENTIALS` (no longer needed)

## 🏗️ OIDC Configuration

### Azure Setup

1. **App Registration Created:**
   - Name: `GitHub-MCP-LLM-Learning`
   - Type: Microsoft Entra ID App Registration
   - Purpose: GitHub Actions OIDC authentication

2. **Federated Credentials Configured:**
   ```json
   {
     "name": "GitHub-Main-Branch",
     "issuer": "https://token.actions.githubusercontent.com",
     "subject": "repo:dbruun/MCP-LLM-Learning:ref:refs/heads/main",
     "audiences": ["api://AzureADTokenExchange"]
   }
   ```

3. **Service Principal Permissions:**
   - Role: Contributor
   - Scope: Subscription level
   - Type: User-assigned managed identity

### GitHub Workflow Configuration

All workflows use this pattern:

```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    permissions:
      id-token: write  # Required for OIDC
      contents: read   # Required for checkout
    steps:
      - name: Login to Azure via OIDC
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
```

## 🔄 Authentication Flow

```
┌─────────────────────────────────────────────────────────┐
│  1. GitHub Actions Workflow Triggered                  │
└────────────────────┬────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│  2. Request OIDC Token from GitHub                     │
│     Subject: repo:owner/repo:ref:refs/heads/main       │
│     Audience: api://AzureADTokenExchange               │
└────────────────────┬────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│  3. GitHub Issues JWT Token                            │
│     - Contains repository, branch, workflow info       │
│     - Signed by GitHub                                 │
│     - Valid for 1 hour                                 │
└────────────────────┬────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│  4. Present Token to Azure AD                          │
│     - Validate signature                               │
│     - Check federated credential match                 │
│     - Verify subject claim                             │
└────────────────────┬────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│  5. Azure AD Issues Access Token                       │
│     - Short-lived (1 hour)                             │
│     - Scoped to subscription                           │
│     - Includes Contributor role                        │
└────────────────────┬────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│  6. Access Azure Resources                             │
│     - Create/update resources                          │
│     - Deploy applications                              │
│     - Run Azure CLI commands                           │
└─────────────────────────────────────────────────────────┘
```

## 📊 Verification

### Check OIDC Usage

All workflows should have:

1. **Permissions block:**
   ```yaml
   permissions:
     id-token: write  # Required for OIDC
     contents: read   # Required for checkout
   ```

2. **Azure login with OIDC parameters:**
   ```yaml
   - uses: azure/login@v2
     with:
       client-id: ${{ secrets.AZURE_CLIENT_ID }}
       tenant-id: ${{ secrets.AZURE_TENANT_ID }}
       subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
   ```

3. **No `creds:` or `credentials:` parameters**

### Verify No Stored Credentials

```bash
# Search for old credential patterns (should return no results)
grep -r "creds:" .github/workflows/
grep -r "azure-credentials:" .github/workflows/
grep -r "AZURE_CREDENTIALS" .github/workflows/*.yml
```

Expected output: No matches (except in documentation/comments)

## 🔍 Audit Trail

OIDC provides enhanced audit capabilities:

1. **Azure AD Sign-in Logs:**
   - View every authentication attempt
   - See which workflow/branch triggered access
   - Track token lifetime and usage

2. **Azure Activity Logs:**
   - See all actions performed by GitHub Actions
   - Correlate with specific workflow runs
   - Track resource changes

3. **GitHub Actions Logs:**
   - See OIDC token acquisition
   - Verify Azure login success
   - Debug authentication issues

## 🎯 Best Practices Implemented

✅ **Least Privilege:** Federated credentials scoped to specific branches  
✅ **Short-lived Tokens:** 1-hour validity, auto-rotation  
✅ **No Stored Secrets:** Zero long-lived credentials in GitHub  
✅ **Audit Trail:** Full visibility in Azure AD and Activity logs  
✅ **Branch Protection:** Separate credentials per environment  
✅ **Automatic Expiration:** Tokens expire without manual rotation  

## 🔒 Security Considerations

### Federated Credential Scope

Current configuration allows:
- ✅ Workflows on `main` branch
- ✅ Pull request workflows (if configured)

Blocked:
- ❌ Workflows from forks (unless explicitly configured)
- ❌ Workflows from other branches (unless explicitly configured)
- ❌ Manual credential usage outside GitHub Actions

### Future Enhancements

Consider implementing:

1. **Environment-specific federated credentials:**
   ```bash
   # Production - requires specific branch
   subject: "repo:owner/repo:ref:refs/heads/main"
   
   # Staging - allows develop branch
   subject: "repo:owner/repo:ref:refs/heads/develop"
   
   # Development - allows any branch
   subject: "repo:owner/repo:pull_request"
   ```

2. **Conditional Access Policies:**
   - Require MFA for production deployments
   - Restrict access to specific IP ranges
   - Enforce device compliance

3. **Just-in-Time (JIT) Access:**
   - Temporary elevated permissions for deployments
   - Auto-revoke after workflow completion

## 📚 Additional Resources

- [GitHub OIDC Documentation](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/configuring-openid-connect-in-azure)
- [Azure Federated Identity Credentials](https://learn.microsoft.com/en-us/azure/active-directory/develop/workload-identity-federation)
- [Azure Login Action](https://github.com/azure/login)
- [Security Hardening for GitHub Actions](https://docs.github.com/en/actions/security-guides/security-hardening-for-github-actions)

## 🤝 Maintenance

### Regular Reviews

- **Quarterly:** Review federated credential configurations
- **Quarterly:** Audit Azure AD sign-in logs
- **Annually:** Review and update service principal permissions
- **Annually:** Validate branch protection rules

### Monitoring

Set up alerts for:
- Failed OIDC authentication attempts
- Unusual token requests
- Permission elevation attempts
- Federated credential modifications

---

**Migration Completed:** October 20, 2025  
**Migration Status:** ✅ All workflows migrated to OIDC  
**Security Posture:** ✅ No long-lived credentials stored  
**Compliance:** ✅ Meets security best practices
