import React, { useEffect, useState } from "react";
import { DashboardCanvas } from "./DashboardCanvas";
import { useDashboardStore } from "../model/dashboardSlice";
import { Button } from "@shared/ui/Button/Button";
import { Modal } from "@shared/ui/Modal/Modal";
import { EmptyState } from "@shared/ui";
import { ModelValidationModal } from "@features/analytics/components/ModelValidationModal";
import { getAnalyticsModels } from "@features/analytics/api/analyticsApi";
import { LocalDesktopOrchestrator } from "@features/powerbi-embed/components/LocalDesktopOrchestrator";
import * as powerBiApi from "@features/powerbi-embed/api/powerBiApi";
import { getDashboardForModel } from "../api/dashboardsApi";
import type { DashboardDefinition } from "../model/types";
import type { AnalyticsModelDto } from "@shared/types/api-contracts";

const fallbackDashboard: DashboardDefinition = {
  id: "00000000-0000-0000-0000-000000000000",
  name: "Analytics Workspace Dashboard",
  pages: [
    {
      name: "Overview",
      canvasWidth: 1280,
      canvasHeight: 720,
      visuals: []
    }
  ]
};

export const DashboardWorkspacePage: React.FC = () => {
  const current = useDashboardStore((s) => s.current);
  const setDashboard = useDashboardStore((s) => s.setDashboard);

  const [viewMode, setViewMode] = useState<"canvas" | "embed">("canvas");
  const [isCompiling, setIsCompiling] = useState(false);
  const [notification, setNotification] = useState<{ type: "success" | "error"; message: string } | null>(null);
  const [isValidateModalOpen, setIsValidateModalOpen] = useState(false);
  const [isImportModalOpen, setIsImportModalOpen] = useState(false);
  const [importWorkspaceId, setImportWorkspaceId] = useState("00000000-0000-0000-0000-000000000001");
  const [importDatasetName, setImportDatasetName] = useState("DirectImportDataset");
  const [importFile, setImportFile] = useState<File | null>(null);

  const [availableModels, setAvailableModels] = useState<AnalyticsModelDto[]>([]);
  const [selectedModelId, setSelectedModelId] = useState<string>("");

  useEffect(() => {
    getAnalyticsModels()
      .then((models) => {
        if (models && models.length > 0) {
          setAvailableModels(models);
          setSelectedModelId((prev) => {
            // 1. Check URL query params (?modelId=... or ?dataset=...)
            const searchParams = new URLSearchParams(window.location.search);
            const queryModelId = searchParams.get("modelId");
            const queryDataset = searchParams.get("dataset");
            if (queryModelId && models.some((m) => m.id === queryModelId)) {
              return queryModelId;
            }
            if (queryDataset) {
              const matched = models.find((m) => m.name.toLowerCase().includes(queryDataset.toLowerCase()));
              if (matched) return matched.id;
            }

            // 2. Check localStorage
            const savedModelId = localStorage.getItem("powerbi_active_model_id");
            if (savedModelId && models.some((m) => m.id === savedModelId)) {
              return savedModelId;
            }

            const savedModelName = localStorage.getItem("powerbi_active_model_name");
            if (savedModelName) {
              const matched = models.find((m) => m.name.toLowerCase().includes(savedModelName.toLowerCase()));
              if (matched) return matched.id;
            }

            const exists = models.some((m) => m.id === prev);
            return exists ? prev : models[0].id;
          });
        }
      })
      .catch((err) => {
        console.warn("Failed to fetch analytics models:", err);
      });
  }, []);

  useEffect(() => {
    if (!selectedModelId) return;
    getDashboardForModel(selectedModelId)
      .then((dash) => {
        if (dash && dash.pages && dash.pages.length > 0) {
          setDashboard(dash);
        }
      })
      .catch((err) => {
        console.warn("Failed to fetch tailored dashboard for model:", err);
      });
  }, [selectedModelId, setDashboard]);

  const activeDashboard = current ?? fallbackDashboard;
  const selectedModel = availableModels.find((m) => m.id === selectedModelId) ?? availableModels[0];
  const activeModelId = selectedModel?.id ?? selectedModelId ?? "";
  const activeModelName = selectedModel?.name ?? (activeDashboard.name ? `${activeDashboard.name} Model` : "Analytics Model");

  const handleCompilePbir = async () => {
    if (!activeModelId) {
      setNotification({ type: "error", message: "No active semantic model available to compile PBIR." });
      return;
    }
    setIsCompiling(true);
    setNotification(null);
    try {
      const result = await powerBiApi.compilePbir({
        dashboardDefinitionId: activeDashboard.id,
        analyticsModelId: activeModelId,
        semanticModelRelativePath: "../definition"
      });
      setNotification({
        type: "success",
        message: `PBIR compiled successfully! Output directory contains ${result.files ? Object.keys(result.files).length : 0} files (definition/report.json).`
      });
    } catch (err) {
      setNotification({
        type: "error",
        message: err instanceof Error ? err.message : "Failed to compile PBIR definition."
      });
    } finally {
      setIsCompiling(false);
    }
  };

  const handleCompileTmdl = async () => {
    if (!activeModelId) {
      setNotification({ type: "error", message: "No active semantic model available to compile TMDL." });
      return;
    }
    setIsCompiling(true);
    setNotification(null);
    try {
      const result = await powerBiApi.compileTmdl({ analyticsModelId: activeModelId });
      const fileCount = result.files ? Object.keys(result.files).length : (result.tables?.length ?? 0);
      setNotification({
        type: "success",
        message: `TMDL semantic model compiled: ${fileCount} files generated for '${result.modelName}'.`
      });
    } catch (err) {
      setNotification({
        type: "error",
        message: err instanceof Error ? err.message : "Failed to compile TMDL semantic model."
      });
    } finally {
      setIsCompiling(false);
    }
  };

  const handleDownloadPbip = async () => {
    if (!activeModelId) {
      setNotification({ type: "error", message: "No active semantic model available to download PBIP." });
      return;
    }
    setIsCompiling(true);
    setNotification(null);
    try {
      const blob = await powerBiApi.downloadPbip({
        dashboardDefinitionId: activeDashboard.id,
        analyticsModelId: activeModelId,
        projectName: activeModelName
      });
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `${activeModelName.replace(/\s+/g, "_")}.pbip.zip`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      window.URL.revokeObjectURL(url);
      setNotification({
        type: "success",
        message: "PBIP project archive compiled and downloaded successfully."
      });
    } catch (err) {
      setNotification({
        type: "error",
        message: err instanceof Error ? err.message : "Failed to download PBIP package."
      });
    } finally {
      setIsCompiling(false);
    }
  };

  const handleDirectImport = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!importFile) return;
    setIsCompiling(true);
    try {
      const result = await powerBiApi.importArtifact(importWorkspaceId, importDatasetName, importFile);
      setNotification({
        type: "success",
        message: `Direct import succeeded: Dataset ID ${result.datasetId}, Name: ${result.displayName}. Status: ${result.importState}`
      });
      setIsImportModalOpen(false);
    } catch (err) {
      setNotification({
        type: "error",
        message: err instanceof Error ? err.message : "Direct artifact import failed."
      });
    } finally {
      setIsCompiling(false);
    }
  };

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "1.25rem" }}>
      {/* Workspace Header — Refined */}
      <div style={{
        display: "flex",
        flexDirection: "column",
        gap: "0.875rem",
        backgroundColor: "var(--bg-card)",
        border: "1px solid var(--border-color)",
        borderRadius: "var(--radius-lg)",
        padding: "1rem 1.25rem",
        boxShadow: "var(--shadow-sm)"
      }}>
        {/* Title row */}
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", flexWrap: "wrap", gap: "0.5rem" }}>
          <div style={{ display: "flex", alignItems: "center", gap: "0.625rem", minWidth: 0 }}>
            <h2 style={{ margin: 0, fontSize: "1.125rem", letterSpacing: "-0.025em", fontWeight: 700 }}>
              {activeDashboard.name}
            </h2>
            <span className="badge badge-info" style={{ fontSize: "0.7rem", padding: "2px 8px", borderRadius: "999px", fontWeight: 500 }}>
              PBIR
            </span>
            {availableModels.length > 0 && (
              <select
                id="semantic-model-select"
                className="form-input"
                style={{
                  padding: "0.2rem 0.5rem",
                  fontSize: "0.8125rem",
                  minWidth: "200px",
                  borderRadius: "var(--radius-sm)",
                  border: "1px solid var(--border-color)",
                  backgroundColor: "var(--bg-subtle)",
                  color: "var(--text-secondary)",
                  maxWidth: "260px"
                }}
                value={activeModelId}
                onChange={(e) => {
                  setSelectedModelId(e.target.value);
                  localStorage.setItem("powerbi_active_model_id", e.target.value);
                }}
                aria-label="semantic-model-select"
              >
                {availableModels.map((m) => (
                  <option key={m.id} value={m.id}>
                    {m.name} ({m.tables?.length ?? 0} {m.tables?.length === 1 ? "table" : "tables"})
                  </option>
                ))}
              </select>
            )}
          </div>
          <p style={{ margin: 0, fontSize: "0.75rem", color: "var(--text-muted)", fontFamily: "monospace" }}>
            {activeDashboard.id.slice(0, 24)}… · {activeDashboard.pages.length} page{activeDashboard.pages.length !== 1 ? "s" : ""}
          </p>
        </div>

        {/* Action toolbar — grouped clusters */}
        <div style={{ display: "flex", alignItems: "center", gap: "0.5rem", flexWrap: "wrap" }}>
          {/* View Mode */}
          <Button
            onClick={() => setViewMode(viewMode === "canvas" ? "embed" : "canvas")}
            variant="secondary"
            aria-label="toggle-view-mode"
            style={{ fontSize: "0.8125rem" }}
          >
            {viewMode === "canvas" ? "🖥 Power BI Desktop" : "◈ Canvas Editor"}
          </Button>

          <div className="toolbar-divider" />

          {/* Validate */}
          <Button
            onClick={() => setIsValidateModalOpen(true)}
            variant="secondary"
            disabled={!activeModelId}
            aria-label="validate-model-btn"
            style={{ fontSize: "0.8125rem" }}
          >
            ✓ Validate
          </Button>

          {/* Compile cluster */}
          <div className="toolbar-group">
            <Button
              onClick={handleCompilePbir}
              variant="secondary"
              disabled={isCompiling || !activeModelId}
              aria-label="compile-pbir-btn"
              style={{ fontSize: "0.8125rem", border: "none", boxShadow: "none", backgroundColor: "transparent" }}
            >
              PBIR
            </Button>
            <div className="toolbar-divider" />
            <Button
              onClick={handleCompileTmdl}
              variant="secondary"
              disabled={isCompiling || !activeModelId}
              aria-label="compile-tmdl-btn"
              style={{ fontSize: "0.8125rem", border: "none", boxShadow: "none", backgroundColor: "transparent" }}
            >
              TMDL
            </Button>
          </div>

          <div className="toolbar-divider" />

          {/* Export / Import cluster */}
          <Button
            onClick={handleDownloadPbip}
            disabled={isCompiling || !activeModelId}
            aria-label="download-pbip-btn"
            style={{ fontSize: "0.8125rem" }}
          >
            ↓ Download PBIP
          </Button>
          <Button
            onClick={() => setIsImportModalOpen(true)}
            variant="secondary"
            aria-label="import-fabric-btn"
            style={{ fontSize: "0.8125rem" }}
          >
            ⇡ Fabric
          </Button>
        </div>
      </div>

      {/* Notification Banner */}
      {notification && (
        <div
          style={{
            padding: "0.75rem 1rem",
            borderRadius: "var(--radius-sm)",
            backgroundColor: notification.type === "success" ? "var(--success-bg)" : "var(--danger-bg)",
            color: notification.type === "success" ? "var(--success)" : "var(--danger)",
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center"
          }}
        >
          <span>{notification.message}</span>
          <button
            onClick={() => setNotification(null)}
            style={{ background: "none", border: "none", cursor: "pointer", color: "inherit", fontWeight: "bold" }}
          >
            ×
          </button>
        </div>
      )}

      {availableModels.length === 0 && !current ? (
        <EmptyState
          title="No Semantic Models Available"
          description="Upload or register a dataset (Excel, CSV, or SQL Server) to automatically synthesize your DAX measures, TMDL model, and PBIR dashboard visuals."
          primaryAction={{
            label: "Go to Data Sources & Ingestion",
            onClick: () => { window.location.href = "/data-sources"; }
          }}
        />
      ) : (
        <div className="canvas-host-card" style={{ minHeight: "600px" }}>
          {viewMode === "canvas" ? (
            <DashboardCanvas />
          ) : (
            <LocalDesktopOrchestrator
              modelId={activeModelId}
              modelName={activeModelName}
              dashboardId={activeDashboard.id}
              dashboardName={activeDashboard.name}
              onDownloadPbip={handleDownloadPbip}
              onSwitchToCanvas={() => setViewMode("canvas")}
            />
          )}
        </div>
      )}

      {/* Model Validation Modal */}
      {activeModelId && (
        <ModelValidationModal
          open={isValidateModalOpen}
          onClose={() => setIsValidateModalOpen(false)}
          modelId={activeModelId}
          modelName={activeModelName}
        />
      )}

      {/* Direct Fabric Import Modal */}
      <Modal open={isImportModalOpen} onClose={() => setIsImportModalOpen(false)}>
        <form onSubmit={handleDirectImport} style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
          <h3 style={{ margin: 0 }}>Direct Power BI / Fabric Artifact Import</h3>
          <p style={{ margin: 0, color: "var(--text-secondary)", fontSize: "0.875rem" }}>
            Upload `.pbix`, `.xlsx`, or `.rdl` files directly into a Power BI Workspace without desktop tooling.
          </p>

          <div className="form-group">
            <label className="form-label">Workspace ID (GUID)</label>
            <input
              className="form-input"
              value={importWorkspaceId}
              onChange={(e) => setImportWorkspaceId(e.target.value)}
              required
            />
          </div>

          <div className="form-group">
            <label className="form-label">Dataset Display Name</label>
            <input
              className="form-input"
              value={importDatasetName}
              onChange={(e) => setImportDatasetName(e.target.value)}
              required
            />
          </div>

          <div className="form-group">
            <label className="form-label">Power BI Artifact (.pbix, .xlsx, .rdl)</label>
            <input
              type="file"
              accept=".pbix,.xlsx,.rdl"
              onChange={(e) => setImportFile(e.target.files?.[0] ?? null)}
              required
            />
          </div>

          <div style={{ display: "flex", justifyContent: "flex-end", gap: "0.5rem" }}>
            <Button type="button" variant="secondary" onClick={() => setIsImportModalOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isCompiling || !importFile}>
              {isCompiling ? "Importing..." : "Upload & Import"}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};
