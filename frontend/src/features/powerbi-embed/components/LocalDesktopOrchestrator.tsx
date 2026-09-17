import React, { useEffect, useState } from "react";
import { Button } from "@shared/ui/Button/Button";
import * as powerBiApi from "../api/powerBiApi";
import type { LocalPowerBiStatusDto, LaunchProjectResultDto } from "@shared/types/api-contracts";

interface LocalDesktopOrchestratorProps {
  modelId: string;
  modelName: string;
  dashboardId: string;
  dashboardName: string;
  onDownloadPbip?: () => void;
  onSwitchToCanvas?: () => void;
}

export const LocalDesktopOrchestrator: React.FC<LocalDesktopOrchestratorProps> = ({
  modelId,
  modelName,
  dashboardId,
  dashboardName,
  onDownloadPbip,
  onSwitchToCanvas
}) => {
  const [status, setStatus] = useState<LocalPowerBiStatusDto | null>(null);
  const [isLoadingStatus, setIsLoadingStatus] = useState(false);
  const [isLaunching, setIsLaunching] = useState(false);
  const [launchResult, setLaunchResult] = useState<LaunchProjectResultDto | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const projectName = modelName ? modelName.replace(/\s+Semantic\s+Model$/i, "") : dashboardName;

  const fetchStatus = async () => {
    setIsLoadingStatus(true);
    try {
      const data = await powerBiApi.getLocalPowerBiStatus();
      setStatus(data);
    } catch (err) {
      console.warn("Failed to fetch local Power BI status:", err);
    } finally {
      setIsLoadingStatus(false);
    }
  };

  useEffect(() => {
    fetchStatus();
  }, []);

  const handleLaunch = async () => {
    setIsLaunching(true);
    setErrorMessage(null);
    setLaunchResult(null);
    try {
      const result = await powerBiApi.launchLocalPowerBi({
        analyticsModelId: modelId,
        dashboardDefinitionId: dashboardId,
        projectName
      });
      setLaunchResult(result);
      // Refresh status after launch
      setTimeout(fetchStatus, 2000);
    } catch (err) {
      setErrorMessage(err instanceof Error ? err.message : "Failed to launch Power BI Desktop.");
    } finally {
      setIsLaunching(false);
    }
  };

  const handleOpenFolder = async () => {
    try {
      const folder = launchResult?.projectDirectory || status?.defaultOutputDirectory;
      await powerBiApi.openLocalPowerBiFolder(folder);
    } catch (err) {
      console.warn("Failed to open folder:", err);
    }
  };

  return (
    <div
      data-testid="local-powerbi-control-center"
      style={{
        display: "flex",
        flexDirection: "column",
        gap: "1.25rem",
        padding: "1.5rem",
        backgroundColor: "var(--bg-surface, #1e293b)",
        borderRadius: "8px",
        color: "var(--text-primary, #f8fafc)"
      }}
    >
      {/* Header */}
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", flexWrap: "wrap", gap: "1rem" }}>
        <div>
          <div style={{ display: "flex", alignItems: "center", gap: "0.75rem" }}>
            <h3 style={{ margin: 0, fontSize: "1.25rem", fontWeight: 700, display: "flex", alignItems: "center", gap: "0.5rem" }}>
              <span>🖥️</span> Local Power BI Desktop Orchestrator
            </h3>
            <span className="badge badge-success" style={{ fontSize: "0.75rem" }}>
              Windows 11 Native
            </span>
            <span className="badge badge-info" style={{ fontSize: "0.75rem" }}>
              Offline / No Azure
            </span>
          </div>
          <p style={{ margin: "0.4rem 0 0 0", color: "var(--text-muted, #94a3b8)", fontSize: "0.875rem", lineHeight: 1.5 }}>
            Orchestrates and launches local Power BI Desktop (<code style={{ color: "#38bdf8" }}>PBIDesktop.exe</code>) directly targeting PBIP and PBIR report metadata with local Tabular TMDL models.
          </p>
        </div>

        <div style={{ display: "flex", gap: "0.5rem" }}>
          <Button variant="secondary" onClick={fetchStatus} disabled={isLoadingStatus} style={{ fontSize: "0.8rem", padding: "0.4rem 0.75rem" }}>
            {isLoadingStatus ? "Checking..." : "🔄 Refresh Status"}
          </Button>
          {onSwitchToCanvas && (
            <Button variant="secondary" onClick={onSwitchToCanvas} style={{ fontSize: "0.8rem", padding: "0.4rem 0.75rem" }}>
              📊 Layout Canvas Editor
            </Button>
          )}
        </div>
      </div>

      {/* Grid of Detection Cards */}
      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(280px, 1fr))", gap: "1rem" }}>
        {/* Card 1: Power BI Desktop Installation */}
        <div
          style={{
            padding: "1rem",
            backgroundColor: "rgba(15, 23, 42, 0.6)",
            border: "1px solid rgba(255, 255, 255, 0.1)",
            borderRadius: "6px"
          }}
        >
          <div style={{ fontSize: "0.75rem", color: "var(--text-muted, #94a3b8)", textTransform: "uppercase", fontWeight: 600 }}>
            Installation Status
          </div>
          <div style={{ display: "flex", alignItems: "center", gap: "0.5rem", marginTop: "0.5rem" }}>
            <span style={{ fontSize: "1.25rem" }}>{status?.isInstalled ? "✅" : "⚠️"}</span>
            <div>
              <div style={{ fontWeight: 600, fontSize: "0.95rem" }}>
                {status?.isInstalled ? "Power BI Desktop Detected" : "Not Detected in Standard Paths"}
              </div>
              <div style={{ fontSize: "0.75rem", color: "#38bdf8" }}>{status?.installationType || "Checking..."}</div>
            </div>
          </div>
          {status?.desktopExecutablePath && (
            <div
              style={{
                marginTop: "0.75rem",
                fontSize: "0.7rem",
                color: "var(--text-muted, #94a3b8)",
                wordBreak: "break-all",
                fontFamily: "monospace"
              }}
            >
              {status.desktopExecutablePath}
            </div>
          )}
        </div>

        {/* Card 2: Process & Analysis Services Engine */}
        <div
          style={{
            padding: "1rem",
            backgroundColor: "rgba(15, 23, 42, 0.6)",
            border: "1px solid rgba(255, 255, 255, 0.1)",
            borderRadius: "6px"
          }}
        >
          <div style={{ fontSize: "0.75rem", color: "var(--text-muted, #94a3b8)", textTransform: "uppercase", fontWeight: 600 }}>
            Local Process & Engine
          </div>
          <div style={{ display: "flex", alignItems: "center", gap: "0.5rem", marginTop: "0.5rem" }}>
            <span style={{ fontSize: "1.25rem" }}>{status?.isRunning ? "🟢" : "⚪"}</span>
            <div>
              <div style={{ fontWeight: 600, fontSize: "0.95rem" }}>
                {status?.isRunning ? `PBIDesktop Running (PID: ${status.processId})` : "PBIDesktop Idle"}
              </div>
              <div style={{ fontSize: "0.75rem", color: status?.analysisServicesPort ? "#4ade80" : "var(--text-muted, #94a3b8)" }}>
                {status?.analysisServicesPort
                  ? `Analysis Services Engine: localhost:${status.analysisServicesPort}`
                  : "Analysis Services (msmdsrv) will start on launch"}
              </div>
            </div>
          </div>
          <div style={{ marginTop: "0.75rem", fontSize: "0.7rem", color: "var(--text-muted, #94a3b8)" }}>
            Zero Azure Entra ID or Fabric subscription required
          </div>
        </div>

        {/* Card 3: Target PBIP Project */}
        <div
          style={{
            padding: "1rem",
            backgroundColor: "rgba(15, 23, 42, 0.6)",
            border: "1px solid rgba(255, 255, 255, 0.1)",
            borderRadius: "6px"
          }}
        >
          <div style={{ fontSize: "0.75rem", color: "var(--text-muted, #94a3b8)", textTransform: "uppercase", fontWeight: 600 }}>
            Active Dataset & Target Project
          </div>
          <div style={{ marginTop: "0.5rem" }}>
            <div style={{ fontWeight: 600, fontSize: "0.95rem", color: "#f8fafc" }}>{projectName}.pbip</div>
            <div style={{ fontSize: "0.75rem", color: "var(--text-muted, #94a3b8)" }}>
              Includes: <code style={{ color: "#cbd5e1" }}>definition.pbir</code>, <code style={{ color: "#cbd5e1" }}>model.tmdl</code>
            </div>
          </div>
          <div
            style={{
              marginTop: "0.75rem",
              fontSize: "0.7rem",
              color: "var(--text-muted, #94a3b8)",
              wordBreak: "break-all",
              fontFamily: "monospace"
            }}
          >
            {status?.defaultOutputDirectory ? `${status.defaultOutputDirectory}\\${projectName}` : "output\\pbip"}
          </div>
        </div>
      </div>

      {/* Action Toolbar */}
      <div
        style={{
          display: "flex",
          gap: "0.75rem",
          flexWrap: "wrap",
          alignItems: "center",
          backgroundColor: "rgba(30, 41, 59, 0.8)",
          padding: "1rem",
          borderRadius: "6px",
          border: "1px solid rgba(255, 255, 255, 0.1)"
        }}
      >
        <Button
          variant="primary"
          onClick={handleLaunch}
          disabled={isLaunching}
          aria-label="launch-desktop-btn"
          style={{ display: "flex", alignItems: "center", gap: "0.4rem", fontWeight: 600 }}
        >
          {isLaunching ? "⏳ Compiling & Launching..." : "🚀 Launch in Local Power BI Desktop"}
        </Button>

        <Button
          variant="secondary"
          onClick={handleOpenFolder}
          aria-label="open-folder-btn"
          style={{ display: "flex", alignItems: "center", gap: "0.4rem" }}
        >
          📂 Open Project Folder in Explorer
        </Button>

        {onDownloadPbip && (
          <Button
            variant="secondary"
            onClick={onDownloadPbip}
            aria-label="download-pbip-action-btn"
            style={{ display: "flex", alignItems: "center", gap: "0.4rem" }}
          >
            📥 Download PBIP Archive (.zip)
          </Button>
        )}
      </div>

      {/* Notification / Feedback Banner */}
      {launchResult && (
        <div
          style={{
            padding: "0.85rem 1.25rem",
            backgroundColor: "rgba(16, 185, 129, 0.15)",
            border: "1px solid rgba(16, 185, 129, 0.4)",
            borderRadius: "6px",
            color: "#6ee7b7",
            fontSize: "0.875rem"
          }}
        >
          <div style={{ fontWeight: 600, display: "flex", alignItems: "center", gap: "0.4rem" }}>
            <span>✅</span> {launchResult.message || "PBIP project successfully created & launched!"}
          </div>
          <div style={{ marginTop: "0.35rem", fontSize: "0.775rem", color: "#a7f3d0", fontFamily: "monospace" }}>
            File: {launchResult.pbipFilePath}
          </div>
        </div>
      )}

      {errorMessage && (
        <div
          style={{
            padding: "0.85rem 1.25rem",
            backgroundColor: "rgba(239, 68, 68, 0.15)",
            border: "1px solid rgba(239, 68, 68, 0.4)",
            borderRadius: "6px",
            color: "#fca5a5",
            fontSize: "0.875rem"
          }}
        >
          <strong>Error:</strong> {errorMessage}
        </div>
      )}

      {/* Architectural Context Note */}
      <div
        style={{
          fontSize: "0.8rem",
          color: "var(--text-muted, #94a3b8)",
          backgroundColor: "rgba(15, 23, 42, 0.4)",
          padding: "0.75rem 1rem",
          borderRadius: "6px",
          borderLeft: "3px solid #38bdf8",
          lineHeight: 1.5
        }}
      >
        💡 <strong>Architecture Note:</strong> Power BI Desktop on Windows operates entirely on local disk metadata (PBIP/PBIR/TMDL) and runs a local Analysis Services tabular engine. Unlike the Power BI Service cloud iframe (which requires Azure Entra ID / Fabric credentials), this local workflow gives you full analytical capability directly on your Windows 11 system with zero cloud subscription.
      </div>
    </div>
  );
};

