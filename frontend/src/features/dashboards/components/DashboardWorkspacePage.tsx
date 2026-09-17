import React, { useEffect, useState } from "react";
import { DashboardCanvas } from "./DashboardCanvas";
import { useDashboardStore } from "../model/dashboardSlice";
import { Button } from "@shared/ui/Button/Button";
import { Modal } from "@shared/ui/Modal/Modal";
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
    <div style={{ display: "flex", flexDirection: "column", gap: "1.5rem" }}>
      {/* Workspace Header & Action Bar */}
      <div className="card" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", flexWrap: "wrap", gap: "1rem" }}>
        <div>
          <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
            <h2 style={{ margin: 0, fontSize: "1.25rem" }}>{activeDashboard.name}</h2>
            <span className="badge badge-info">PBIP/PBIR Target</span>
          </div>
          <p style={{ margin: "0.25rem 0 0 0", fontSize: "0.875rem", color: "var(--text-muted)" }}>
            ID: {activeDashboard.id} | Canvas Pages: {activeDashboard.pages.length}
          </p>
        </div>

        <div style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap", alignItems: "center" }}>
          {availableModels.length > 0 && (
            <div style={{ display: "flex", alignItems: "center", gap: "0.35rem" }}>
              <label htmlFor="semantic-model-select" style={{ fontSize: "0.85rem", fontWeight: 600, color: "var(--text-secondary)" }}>
                Target Model:
              </label>
              <select
                id="semantic-model-select"
                className="form-input"
                style={{ padding: "0.35rem 0.5rem", fontSize: "0.875rem", minWidth: "220px" }}
                value={activeModelId}
                onChange={(e) => setSelectedModelId(e.target.value)}
                aria-label="semantic-model-select"
              >
                {availableModels.map((m) => (
                  <option key={m.id} value={m.id}>
                    {m.name} ({m.tables?.length ?? 0} {m.tables?.length === 1 ? "table" : "tables"})
                  </option>
                ))}
              </select>
            </div>
          )}
          <Button
            onClick={() => setViewMode(viewMode === "canvas" ? "embed" : "canvas")}
            variant="secondary"
            aria-label="toggle-view-mode"
          >
            {viewMode === "canvas" ? "Local Power BI Desktop" : "Layout Canvas Editor"}
          </Button>
          <Button
            onClick={() => setIsValidateModalOpen(true)}
            variant="secondary"
            disabled={!activeModelId}
            aria-label="validate-model-btn"
          >
            Validate Model
          </Button>
          <Button
            onClick={handleCompilePbir}
            variant="secondary"
            disabled={isCompiling || !activeModelId}
            aria-label="compile-pbir-btn"
          >
            Compile PBIR
          </Button>
          <Button
            onClick={handleCompileTmdl}
            variant="secondary"
            disabled={isCompiling || !activeModelId}
            aria-label="compile-tmdl-btn"
          >
            Compile TMDL
          </Button>
          <Button
            onClick={handleDownloadPbip}
            disabled={isCompiling || !activeModelId}
            aria-label="download-pbip-btn"
          >
            Download PBIP (.zip)
          </Button>
          <Button
            onClick={() => setIsImportModalOpen(true)}
            variant="secondary"
            aria-label="import-fabric-btn"
          >
            Fabric Import
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

      {/* Main Viewport */}
      {availableModels.length === 0 && !current ? (
        <div
          className="card"
          style={{
            padding: "3rem 2rem",
            textAlign: "center",
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            gap: "1rem"
          }}
        >
          <div style={{ fontSize: "2.5rem" }}>📊</div>
          <h3 style={{ margin: 0, fontSize: "1.25rem" }}>No Semantic Models Available</h3>
          <p style={{ margin: 0, color: "var(--text-secondary)", maxWidth: "500px" }}>
            Upload or register a dataset (Excel, CSV, or SQL Server) to automatically synthesize your DAX measures, TMDL model, and PBIR dashboard visuals.
          </p>
          <Button variant="primary" onClick={() => { window.location.href = "/data-sources"; }}>
            Go to Data Sources & Ingestion
          </Button>
        </div>
      ) : (
        <div className="card" style={{ padding: "1rem", minHeight: "650px", overflow: "auto" }}>
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
