# Runbook 01: Service Principal & Key Vault Secret Rotation

## 1. Overview & Objective
This runbook defines the zero-downtime procedure for rotating the Azure Entra ID Service Principal credentials used by `AnalyticsPlatform.Api` to authenticate with Microsoft Fabric and the Power BI REST API (`https://analysis.windows.net/powerbi/api`).

**Rotation Schedule:** Every 90 days or immediately upon suspected compromise.

---

## 2. Prerequisites
- `az` CLI version `>= 2.60.0` authenticated with `Application.ReadWrite.All` and `KeyVault.Secrets.Set` permissions.
- Access to Key Vault `kv-analytics-prod`.

---

## 3. Step-by-Step Rotation Workflow

### Step 1: Create Secondary Client Secret in Entra ID
Do **not** delete or overwrite the existing secret yet. Create a dual-secret window:

```bash
# 1. Fetch Application ID
APP_ID=$(az ad app list --display-name "sp-analytics-platform-prod" --query "[0].appId" -o tsv)

# 2. Add new secondary secret (valid for 180 days)
NEW_SECRET=$(az ad app credential reset \
  --id "$APP_ID" \
  --append \
  --years 0.5 \
  --query "password" -o tsv)

echo "New secret generated successfully."
```

### Step 2: Store New Secret in Azure Key Vault
Update the Key Vault secret with the new value:

```bash
az keyvault secret set \
  --vault-name "kv-analytics-prod" \
  --name "PowerBi--ClientSecret" \
  --value "$NEW_SECRET"
```

### Step 3: Trigger Rolling Revision in Azure Container Apps
Force Azure Container Apps to restart with fresh environment secrets:

```bash
az containerapp revision restart \
  --name "ca-analytics-api-prod" \
  --resource-group "rg-analytics-platform-prod" \
  --revision "$(az containerapp show --name ca-analytics-api-prod -g rg-analytics-platform-prod --query properties.latestRevisionName -o tsv)"
```

### Step 4: Validate Power BI API Connectivity
Execute health check endpoint to ensure the new token handshake succeeds:

```bash
curl -f https://ca-analytics-api-prod.azurecontainerapps.io/health/live
```
Verify that `"powerbi": "Healthy"` is returned in the response payload.

### Step 5: Decommission Old Secret
Once verified across all production revisions:

```bash
# Query old secret keyId
OLD_KEY_ID=$(az ad app credential list --id "$APP_ID" --query "[1].keyId" -o tsv)

# Delete old credential
az ad app credential delete --id "$APP_ID" --key-id "$OLD_KEY_ID"
```

---

## 4. Rollback Plan
If authentication fails during Step 4:
1. Revert Key Vault secret `PowerBi--ClientSecret` to the previous value.
2. Restart Container App revision.
3. Investigate Entra ID API permission grant logs in Azure Portal.

