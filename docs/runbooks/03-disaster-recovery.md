# Runbook 03: Disaster Recovery & Geo-Redundant Restores

## 1. Disaster Recovery Objectives

| Metric | Target SLA | Strategy |
|---|---|---|
| **Recovery Point Objective (RPO)** | **< 5 minutes** | Azure SQL Geo-Replication + Continuous Point-in-Time Restore (PITR) + Git PBIP tracking |
| **Recovery Time Objective (RTO)** | **< 30 minutes** | Multi-region Container App deployments + Terraform automated infrastructure replication |

---

## 2. Recovery Architecture

```
  PRIMARY REGION (Southeast Asia)                STANDBY REGION (East Asia)
┌─────────────────────────────────┐           ┌─────────────────────────────────┐
│ Container App (Primary)         │           │ Container App (Standby)         │
│         │                       │           │         │                       │
│         ▼                       │           │         ▼                       │
│ Azure SQL (Primary Writable) ───┼───────────┼──► Azure SQL (Active Secondary) │
│         │ (Async Replication)   │           │                                 │
│         ▼                       │           │                                 │
│ Key Vault + Storage (Primary)   │           │ Key Vault (Replicated Secrets)  │
└─────────────────────────────────┘           └─────────────────────────────────┘
```

---

## 3. Disaster Declaration & Failover Execution

### Step 1: Declare Regional Outage & Fail Over Azure SQL
Initiate an unplanned failover to promote the active geo-secondary database in East Asia:

```bash
az sql db replica set-primary \
  --resource-group "rg-analytics-platform-prod" \
  --server "sql-analytics-secondary" \
  --name "sqldb-analytics-prod" \
  --allow-data-loss
```

### Step 2: Reroute Container App Traffic via Azure Front Door / Traffic Manager
Point production DNS records to the secondary region endpoint:

```bash
az network front-door backend-pool backend update \
  --front-door-name "fd-analytics-platform" \
  --pool-name "primary-backend-pool" \
  --resource-group "rg-global-network" \
  --index 1 \
  --priority 1 \
  --weight 100
```

### Step 3: Re-compile PBIP Projects from Source Control
Because all PBIR and TMDL models are versioned in Git (`analytics-platform/`), re-sync workspaces:

```bash
# Clone clean repository state
git clone git@github.com:RaymundGerardReyes/PowerBIIntegrator.git /opt/dr-recovery
cd /opt/dr-recovery

# Trigger automated CI/CD deployment pipeline to secondary workspace
gh workflow run cd-production.yml -f deploy_region="East Asia"
```

### Step 4: Validate Platform Health
Run synthetic health probes:

```bash
# Verify API health
curl -f https://dr-analytics-api.azurecontainerapps.io/health/live

# Verify Report Generation pipeline
curl -X POST https://dr-analytics-api.azurecontainerapps.io/api/reports/pdf \
  -H "Content-Type: application/json" \
  -d '{"title":"DR Verification Report","sections":[{"heading":"Verification","narrative":"DR failover test."}]}' \
  --output test-report.pdf

# Check PDF magic bytes (%PDF-)
head -c 5 test-report.pdf
```

---

## 4. Fallback / Repatriation (Return to Primary)
Once Microsoft resolves the primary regional outage:
1. Re-establish geo-replication from East Asia back to Southeast Asia.
2. Synchronize transactions until replication lag is 0ms.
3. Perform planned failover during a scheduled maintenance window.
