import React, { useEffect, useState } from "react";
import { DashboardCanvas } from "./DashboardCanvas";
import { useDashboardStore } from "../model/dashboardSlice";
import { Button } from "@shared/ui/Button/Button";
import { Modal } from "@shared/ui/Modal/Modal";
import { ModelValidationModal } from "@features/analytics/components/ModelValidationModal";
import { ReportEmbed } from "@features/powerbi-embed/components/ReportEmbed";
import * as powerBiApi from "@features/powerbi-embed/api/powerBiApi";
import type { DashboardDefinition } from "../model/types";

const starterDashboard: DashboardDefinition = {
  id: "77777777-7777-7777-7777-777777777777",
  name: "Enterprise Revenue & Operations Dashboard",
  pages: [
    {
      name: "Executive Overview",
      canvasWidth: 1280,
      canvasHeight: 720,
      visuals: [
        {
          name: "kpi-revenue",
          visualType: "card",
          layout: { x: 40, y: 30, width: 340, height: 160, z: 1, visible: true },
          boundFields: ["Sales[TotalRevenue]"]
        },
        {
          name: "kpi-margin",
          visualType: "card",
          layout: { x: 420, y: 30, width: 340, height: 160, z: 1, visible: true },
          boundFields: ["Sales[OperatingMargin]"]
        },
        {
          name: "kpi-customers",
          visualType: "card",
          layout: { x: 800, y: 30, width: 340, height: 160, z: 1, visible: true },
          boundFields: ["Customers[ActiveCount]"]
        },
        {
          name: "chart-sales-trend",
          visualType: "lineChart",
          layout: { x: 40, y: 220, width: 720, height: 440, z: 1, visible: true },
          boundFields: ["Date[Month]", "Sales[Revenue]"]
        },
        {
          name: "donut-by-region",
          visualType: "pieChart",
          layout: { x: 800, y: 220, width: 440, height: 440, z: 1, visible: true },
          boundFields: ["Geography[Region]", "Sales[Revenue]"]
        }
      ]
    },
    {
      name: "Regional Breakdown",
      canvasWidth: 1280,
      canvasHeight: 720,
      visuals: [
        {
          name: "table-regional-sales",
          visualType: "table",
          layout: { x: 40, y: 40, width: 1100, height: 600, z: 1, visible: true },
          boundFields: ["Geography[Territory]", "Sales[Revenue]", "Sales[Target]"]
        }
      ]
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

  useEffect(() => {
    if (!current) {
      setDashboard(starterDashboard);
    }
  }, [current, setDashboard]);

  const activeDashboard = current ?? starterDashboard;

  const handleCompilePbir = async () => {
    setIsCompiling(true);
    setNotification(null);
    try {
      const result = await powerBiApi.compilePbir({
        dashboardDefinitionId: activeDashboard.id,
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
    setIsCompiling(true);
    setNotification(null);
    try {
      const result = await powerBiApi.compileTmdl({
        analyticsModelId: "11111111-1111-1111-1111-111111111111"
      });
      setNotification({
        type: "success",
        message: `TMDL semantic model compiled: ${result.modelName} with ${result.tables?.length ?? 0} tables and ${result.relationships?.length ?? 0} relationships.`
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
    setIsCompiling(true);
    setNotification(null);
    try {
      const blob = await powerBiApi.downloadPbip({
        dashboardDefinitionId: activeDashboard.id,
        analyticsModelId: "11111111-1111-1111-1111-111111111111",
        projectName: "EnterpriseAnalytics"
      });
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `${activeDashboard.name.replace(/\s+/g, "_")}.pbip.zip`;
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

        <div style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap" }}>
          <Button
            onClick={() => setViewMode(viewMode === "canvas" ? "embed" : "canvas")}
            variant="secondary"
            aria-label="toggle-view-mode"
          >
            {viewMode === "canvas" ? "Native Embed View" : "Layout Canvas Editor"}
          </Button>
          <Button
            onClick={() => setIsValidateModalOpen(true)}
            variant="secondary"
            aria-label="validate-model-btn"
          >
            Validate Model
          </Button>
          <Button
            onClick={handleCompilePbir}
            variant="secondary"
            disabled={isCompiling}
            aria-label="compile-pbir-btn"
          >
            Compile PBIR
          </Button>
          <Button
            onClick={handleCompileTmdl}
            variant="secondary"
            disabled={isCompiling}
            aria-label="compile-tmdl-btn"
          >
            Compile TMDL
          </Button>
          <Button
            onClick={handleDownloadPbip}
            disabled={isCompiling}
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

      {/* Main Canvas / Native Embed Viewport */}
      <div className="card" style={{ padding: "1rem", minHeight: "650px", overflow: "auto" }}>
        {viewMode === "canvas" ? (
          <DashboardCanvas />
        ) : (
          <div style={{ height: "650px" }}>
            <ReportEmbed
              config={{
                reportId: "demo-report-01",
                embedUrl: "https://app.powerbi.com/reportEmbed?reportId=demo-report-01",
                accessToken: "dummy-embed-token"
              }}
            />
          </div>
        )}
      </div>

      {/* Model Validation Modal */}
      <ModelValidationModal
        open={isValidateModalOpen}
        onClose={() => setIsValidateModalOpen(false)}
        modelId="11111111-1111-1111-1111-111111111111"
        modelName="Enterprise Sales & Finance Semantic Model"
      />

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
