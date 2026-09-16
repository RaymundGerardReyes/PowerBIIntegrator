# Runbook 02: Power BI / Fabric Capacity Scaling & Failover

## 1. Overview & Objective
This runbook governs capacity throttling mitigation, automated SKU scaling (e.g. F32 to F64 / F128), and emergency failover to standby Fabric capacities when Power BI embedded rendering times exceed acceptable SLAs (P99 > 3.5s or HTTP 429 Too Many Requests).

---

## 2. Capacity Monitoring & Alert Triggers

| Metric | Warning Threshold | Critical Failover Threshold |
|---|---|---|
| **CPU Utilization (Capacity Metrics)** | > 80% sustained over 10m | > 95% sustained over 5m |
| **Interactive Query Delay** | > 1,500 ms | > 3,500 ms |
| **HTTP 429 Throttling Rejections** | > 5 requests / min | > 50 requests / min |

---

## 3. Scale-Up Procedure (Vertical Scaling)

To upscale capacity instantly without disrupting embedded sessions:

```bash
# 1. Scale Fabric capacity from F32 to F64
az fabric capacity update \
  --name "fabriccapacityprod" \
  --resource-group "rg-analytics-platform-prod" \
  --sku-name "F64"

# 2. Verify state
az fabric capacity show \
  --name "fabriccapacityprod" \
  --resource-group "rg-analytics-platform-prod" \
  --query "state" -o tsv
```

Scaling completes within 60 to 90 seconds without dropping active sessions.

---

## 4. Emergency Cross-Capacity Failover

If primary capacity experiences regional Azure outages:

### Step 1: Activate Secondary Standby Capacity
```bash
az fabric capacity resume \
  --name "fabriccapacitystandby" \
  --resource-group "rg-analytics-platform-prod"
```

### Step 2: Reassign Workspaces to Standby Capacity
Using the Power BI REST API:

```bash
ACCESS_TOKEN=$(az account get-access-token --resource "https://analysis.windows.net/powerbi/api" --query "accessToken" -o tsv)

curl -X POST "https://api.powerbi.com/v1.0/myorg/groups/WORKSPACE_ID/AssignToCapacity" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"capacityId": "STANDBY_CAPACITY_GUID"}'
```

### Step 3: Verify Embed Token Issuance
Execute platform embed configuration query to confirm tokens point to new capacity:

```bash
curl -f https://ca-analytics-api-prod.azurecontainerapps.io/api/powerbi/embed-config/sample-report-id
```

---

## 5. Post-Incident Review
1. Export Fabric Capacity Metrics App telemetry for the incident window.
2. Analyze DAX query execution plans for unoptimized high-cardinality cross-joins.
3. Review auto-scaling policies to prevent recurring resource starvation.

