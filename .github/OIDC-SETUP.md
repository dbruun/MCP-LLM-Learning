# GitHub Actions OIDC Authentication Setup Guide

This guide walks through converting your GitHub Actions workflows from client secret authentication to OpenID Connect (OIDC) for enhanced security.

## Benefits of OIDC

- ✅ No secrets to rotate or manage
- ✅ Short-lived tokens (automatically expire)
- ✅ Better security posture
- ✅ Fine-grained access control
- ✅ Audit trail of token requests

---

## Step 1: Azure Configuration

### 1.1 Get Your Azure Information

```powershell
# Login to Azure
az login

# Get your subscription and tenant info
az account show --query '{subscriptionId:id, tenantId:tenantId}' -o table

# Get your existing app registration ID (if you have one)
az ad app list --display-name "github-actions" --query '[0].appId' -o tsv
```

### 1.2 Create or Update App Registration

```powershell
# If you don't have an app registration, create one
APP_ID=$(az ad app create --display-name "github-actions-oidc" --query appId -o tsv)
echo "App ID: $APP_ID"

# Create a service principal for the app
SP_ID=$(az ad sp create --id $APP_ID --query id -o tsv)
echo "Service Principal ID: $SP_ID"

# Assign Contributor role to the service principal (adjust scope as needed)
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
az role assignment create \
  --assignee $APP_ID \
  --role Contributor \
  --scope /subscriptions/$SUBSCRIPTION_ID
```

### 1.3 Configure Federated Credentials for GitHub OIDC

```powershell
# Set your GitHub repo details
GITHUB_ORG="dbruun"
GITHUB_REPO="MCP-LLM-Learning"

# For main branch
az ad app federated-credential create \
  --id $APP_ID \
  --parameters "{
    \"name\": \"github-oidc-main\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${GITHUB_ORG}/${GITHUB_REPO}:ref:refs/heads/main\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }"

# For all branches (useful for feature branches)
az ad app federated-credential create \
  --id $APP_ID \
  --parameters "{
    \"name\": \"github-oidc-branches\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${GITHUB_ORG}/${GITHUB_REPO}:ref:refs/heads/*\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }"

# For pull requests
az ad app federated-credential create \
  --id $APP_ID \
  --parameters "{
    \"name\": \"github-oidc-pr\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${GITHUB_ORG}/${GITHUB_REPO}:pull_request\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }"

# For specific environments (optional, for production approval gates)
az ad app federated-credential create \
  --id $APP_ID \
  --parameters "{
    \"name\": \"github-oidc-production\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${GITHUB_ORG}/${GITHUB_REPO}:environment:production\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }"
```

### 1.4 Verify Federated Credentials

```powershell
# List all federated credentials
az ad app federated-credential list --id $APP_ID -o table
```

---

## Step 2: Update GitHub Secrets

### 2.1 Set New Secrets

```powershell
# Get the values you need
TENANT_ID=$(az account show --query tenantId -o tsv)
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
# APP_ID from previous step

echo "Client ID: $APP_ID"
echo "Tenant ID: $TENANT_ID"
echo "Subscription ID: $SUBSCRIPTION_ID"

# Set the secrets in GitHub
gh secret set AZURE_CLIENT_ID -b "$APP_ID"
gh secret set AZURE_TENANT_ID -b "$TENANT_ID"
gh secret set AZURE_SUBSCRIPTION_ID -b "$SUBSCRIPTION_ID"

# Verify secrets are set
gh secret list
```

### 2.2 Remove Old Secret (Optional)

```powershell
# After verifying OIDC works, you can delete the old secret
gh secret delete AZURE_CREDENTIALS
```

---

## Step 3: Workflow Changes Summary

All workflow files have been updated with these changes:

### Changes Made:

1. **Added `permissions` block** to all jobs that authenticate:
   ```yaml
   permissions:
     id-token: write  # Required for OIDC
     contents: read   # Required for checkout/artifacts
   ```

2. **Updated `azure/login@v2` action** from:
   ```yaml
   - name: Login to Azure
     uses: azure/login@v2
     with:
       creds: ${{ secrets.AZURE_CREDENTIALS }}
   ```
   
   To:
   ```yaml
   - name: Login to Azure via OIDC
     uses: azure/login@v2
     with:
       client-id: ${{ secrets.AZURE_CLIENT_ID }}
       tenant-id: ${{ secrets.AZURE_TENANT_ID }}
       subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
   ```

3. **Updated secrets in reusable workflows**:
   - Replaced `azure_credentials` with `azure_client_id`, `azure_tenant_id`, `azure_subscription_id`
   - Updated all workflow calls to pass these three secrets

### Files Modified:

- ✅ `.github/workflows/deploy.yml`
- ✅ `.github/workflows/orchestrator.yml`
- ✅ `.github/workflows/manual-deploy.yml`
- ✅ `.github/workflows/template-deploy-dev.yml`
- ✅ `.github/workflows/template-deploy-staging.yml`
- ✅ `.github/workflows/template-deploy-prod.yml`

---

## Step 4: Test the Configuration

### 4.1 Test with Manual Trigger

```powershell
# Trigger a dev deployment to test
gh workflow run manual-deploy.yml -f environment=dev -f project_number=RITM1234567

# Watch the execution
gh run watch
```

### 4.2 Verify Authentication

In the workflow logs, you should see:
- ✅ "Login to Azure via OIDC" step succeeds
- ✅ No "client secret" references
- ✅ Token exchange with Azure AD

### 4.3 Check for Issues

```powershell
# View logs if there are failures
gh run list --limit 1
gh run view <run-id> --log-failed
```

---

## Common Issues & Troubleshooting

### Issue: "federated credential already exists"

**Solution:**
```powershell
# List and delete existing credential
az ad app federated-credential list --id $APP_ID
az ad app federated-credential delete --id $APP_ID --federated-credential-id <credential-name>
```

### Issue: "AADSTS70021: No matching federated identity record found"

**Causes:**
- Subject claim in federated credential doesn't match
- Wrong issuer URL
- Credential not properly created

**Solution:**
```powershell
# Verify the subject format matches exactly
# For main branch: repo:OWNER/REPO:ref:refs/heads/main
# Check your federated credentials
az ad app federated-credential show --id $APP_ID --federated-credential-id github-oidc-main
```

### Issue: "Secrets not found" lint errors

**This is expected!** The lint errors appear because the secrets don't exist yet in GitHub. They will disappear once you set the secrets using `gh secret set`.

### Issue: "Login failed"

**Check:**
1. Service principal has correct role assignment
2. Federated credentials match your repo exactly
3. Secrets are set correctly in GitHub

```powershell
# Verify role assignment
az role assignment list --assignee $APP_ID --all -o table

# Test locally with Azure CLI to verify SP works
az login --service-principal -u $APP_ID --tenant $TENANT_ID --federated-token "$(gh api /repos/dbruun/MCP-LLM-Learning/actions/oidc/token --jq .token)"
```

---

## Security Best Practices

1. **Use fine-grained scopes**: Assign roles only to specific resource groups, not entire subscriptions
2. **Use environment-specific credentials**: Create separate federated credentials for each environment
3. **Enable approval gates**: Use GitHub Environments with required reviewers for production
4. **Audit regularly**: Monitor Azure AD sign-in logs for federated authentication

```powershell
# Assign role to specific resource group only
az role assignment create \
  --assignee $APP_ID \
  --role Contributor \
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/YOUR_RESOURCE_GROUP
```

---

## Rollback Plan

If you need to revert to client secret authentication:

1. Keep the old `AZURE_CREDENTIALS` secret (don't delete it immediately)
2. Test OIDC thoroughly in dev first
3. If issues arise, revert the workflow files using:
   ```powershell
   git revert <commit-hash>
   ```

---

## Additional Resources

- [Azure OIDC Documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/workload-identity-federation)
- [GitHub Actions OIDC](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/configuring-openid-connect-in-azure)
- [azure/login Action](https://github.com/Azure/login)

---

## Summary

✅ Azure app registration configured with federated credentials  
✅ GitHub secrets updated (CLIENT_ID, TENANT_ID, SUBSCRIPTION_ID)  
✅ All 6 workflow files updated to use OIDC  
✅ Permissions blocks added to all jobs  

**Next steps:**
1. Run the Azure configuration commands above
2. Set the GitHub secrets
3. Test with a dev deployment
4. Monitor the first few runs for any issues
