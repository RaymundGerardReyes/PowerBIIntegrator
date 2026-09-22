import React, { useState, useEffect } from "react";
import { ProfileSummaryPanel } from "./ProfileSummaryPanel";
import { DuplicateReviewTable, DuplicateReviewItem } from "./DuplicateReviewTable";
import { CleaningRuleEditor } from "./CleaningRuleEditor";
import { TransformationPlanBuilder } from "./TransformationPlanBuilder";
import { ChartSuggestionPanel } from "./ChartSuggestionPanel";
import { AdvisoryPanel } from "@features/ai-advisory";
import { useDashboardStore } from "@features/dashboards";
import { usePipelineRun } from "../hooks/usePipelineRun";
import { useDataSources, type DataSourceDefinition } from "@features/data-sources";
import { Button, Badge, Card, ContextBar, WorkflowStepper } from "@shared/ui";

export const DataQualityDashboardPage: React.FC = () => {
  const [activeStage, setActiveStage] = useState<"profile" | "dedupe" | "clean" | "transform" | "visuals" | "advisory">("profile");
  const { data: dataSources = [], isLoading: isLoadingSources } = useDataSources();
  const [selectedSourceId, setSelectedSourceId] = useState<string>("");

  const {
    profile,
    runResult,
    suggestions,
    isRunning,
    error,
    runProfiling,
    executeFullPipeline
  } = usePipelineRun();

  useEffect(() => {
    if (dataSources.length > 0 && !selectedSourceId) {
      const latest = dataSources[dataSources.length - 1];
      setSelectedSourceId(latest.id);
    }
  }, [dataSources, selectedSourceId]);

  const activeSource: DataSourceDefinition | null =
    dataSources.find((ds: DataSourceDefinition) => ds.id === selectedSourceId) ||
    (dataSources.length > 0 ? dataSources[dataSources.length - 1] : null);

  const [clusters, setClusters] = useState<DuplicateReviewItem[]>([]);

  const handleKeepOverride = (clusterId: string, newKeptRowId: string) => {
    setClusters((prev) =>
      prev.map((c) =>
        c.clusterId === clusterId
          ? {
              ...c,
              keptRowId: newKeptRowId,
              droppedRowIds: [c.keptRowId, ...c.droppedRowIds.filter((id) => id !== newKeptRowId)]
            }
          : c
      )
    );
  };

  const handleStartProfiling = () => {
    if (activeSource) {
      runProfiling(activeSource.connectionOrPath, activeSource.name);
    } else {
      runProfiling("DefaultSource", "DefaultDataset");
    }
  };

  const handleRunFull = () => {
    if (activeSource) {
      executeFullPipeline(activeSource.connectionOrPath, activeSource.name, `Gold_${activeSource.name.replace(/[^a-zA-Z0-9_]/g, "_")}`);
    } else {
      executeFullPipeline("DefaultSource", "DefaultDataset", "Gold_Fact_Sales");
    }
  };

  const [addedNotification, setAddedNotification] = useState<string | null>(null);

  const handleSelectSuggestion = (visualType: string) => {
    const store = useDashboardStore.getState();
    const current = store.current;
    if (!current || !current.pages.length) return;

    const page = current.pages[0];
    const visualCount = page.visuals.length;
    const newVisualName = `visual-${visualType}-${visualCount + 1}`;

    const primaryTable = activeSource?.name ? activeSource.name.replace(/[^a-zA-Z0-9_]/g, "") : "Data";
    let boundFields: string[] = [];

    if (visualType === "card") {
      boundFields = [`${primaryTable}[TotalRows]`];
    } else if (visualType === "table" || visualType === "tableEx") {
      boundFields = profile?.columnProfiles.slice(0, 5).map((c) => `${primaryTable}[${c.columnName}]`) ?? [`${primaryTable}[TotalRows]`];
    } else {
      const categoryCol = profile?.columnProfiles.find((c) => c.inferredType === "String" || c.cardinalityClass === "Low")?.columnName ?? "Category";
      boundFields = [`${primaryTable}[${categoryCol}]`, `${primaryTable}[TotalRows]`];
    }

    const newVisual = {
      name: newVisualName,
      visualType,
      layout: {
        x: (visualCount % 3) * 400 + 40,
        y: Math.floor(visualCount / 3) * 260 + 40,
        width: 380,
        height: 240,
        visible: true
      },
      boundFields
    };

    store.addVisual(page.name, newVisual);
    setAddedNotification(`Added ${visualType} (${newVisualName}) to dashboard '${current.name}'.`);
    setTimeout(() => setAddedNotification(null), 4000);
  };

  const stages = [
    { id: "profile", label: "01 Profile", status: activeStage === "profile" ? "active" : (profile ? "completed" : "pending") },
    { id: "dedupe", label: "02 Duplicates", status: activeStage === "dedupe" ? "active" : "pending", hasBadge: true, badgeCount: clusters.length },
    { id: "clean", label: "03 Cleaning", status: activeStage === "clean" ? "active" : "pending" },
    { id: "transform", label: "04 Transform", status: activeStage === "transform" ? "active" : "pending" },
    { id: "visuals", label: "05 Visuals", status: activeStage === "visuals" ? "active" : "pending" },
    { id: "advisory", label: "06 Advisory", status: activeStage === "advisory" ? "active" : "pending" }
  ] as const;

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "1.5rem" }}>
      {/* Context Bar */}
      <ContextBar
        title={activeSource ? activeSource.name : "No Dataset Selected"}
        metadata={[
          { label: "Source", value: activeSource?.type || "None" },
          { label: "Rows", value: profile?.totalRows?.toLocaleString() || "Not Profiled" },
          { label: "Quality", value: profile ? "92%" : "N/A" },
          { label: "Last Action", value: runResult ? "Processed" : "Pending" }
        ]}
        secondaryAction={
          <select
            className="form-input"
            style={{ padding: "0.3rem 0.5rem", fontSize: "0.8rem", width: "160px", height: "30px" }}
            value={activeSource?.id || ""}
            onChange={(e) => setSelectedSourceId(e.target.value)}
          >
            <option value="" disabled>Switch dataset...</option>
            {dataSources.map((ds: DataSourceDefinition) => (
              <option key={ds.id} value={ds.id}>{ds.name}</option>
            ))}
          </select>
        }
        primaryAction={
          <div style={{ display: "flex", gap: "0.5rem" }}>
            <Button variant="secondary" className="btn-sm" onClick={handleStartProfiling} disabled={isRunning || !activeSource}>
              {isRunning ? "Profiling..." : "Run Profile"}
            </Button>
            <Button variant="primary" className="btn-sm" onClick={handleRunFull} disabled={isRunning || !activeSource}>
              {isRunning ? "Executing Pipeline..." : "Run Pipeline"}
            </Button>
          </div>
        }
      />

      {error && (
        <Card style={{ backgroundColor: "var(--danger-bg)", borderColor: "var(--danger-border)" }}>
          <p style={{ margin: 0, color: "var(--danger)", fontSize: "0.875rem", fontWeight: 500 }}>
            {error}
          </p>
        </Card>
      )}

      {/* Stage Navigation */}
      <WorkflowStepper
        steps={stages as any}
        onStepClick={(id) => setActiveStage(id as any)}
      />

      {/* Stage Content */}
      <div style={{ minHeight: "400px" }}>
        {activeStage === "profile" && (
          <ProfileSummaryPanel profile={profile} isLoading={isRunning} />
        )}

        {activeStage === "dedupe" && (
          <DuplicateReviewTable clusters={clusters} onKeepOverride={handleKeepOverride} />
        )}

        {activeStage === "clean" && (
          <CleaningRuleEditor />
        )}

        {activeStage === "transform" && (
          <TransformationPlanBuilder />
        )}

        {activeStage === "visuals" && (
          <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
            {addedNotification && (
              <div
                data-testid="chart-added-notification"
                style={{
                  padding: "0.5rem 1rem",
                  backgroundColor: "var(--success-bg, #f0fdf4)",
                  border: "1px solid var(--success-border, #bbf7d0)",
                  borderRadius: "var(--radius-sm, 6px)",
                  color: "var(--success, #16a34a)",
                  fontSize: "0.8125rem",
                  fontWeight: 500
                }}
              >
                ✓ {addedNotification}
              </div>
            )}
            <ChartSuggestionPanel suggestions={suggestions} onSelectSuggestion={handleSelectSuggestion} />
          </div>
        )}

        {activeStage === "advisory" && (
          <div style={{ minHeight: "650px" }}>
            <AdvisoryPanel runId={runResult?.runId || "run-demo-001"} userRole="DataSteward" />
          </div>
        )}
      </div>

      {/* Run Summary Log */}
      {runResult && (
        <Card style={{ backgroundColor: "var(--bg-subtle)", marginTop: "2rem" }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "0.75rem" }}>
            <h4 style={{ margin: 0, fontSize: "0.95rem" }}>Pipeline Execution Log</h4>
            <Badge variant="success">Completed</Badge>
          </div>
          <div style={{ fontSize: "0.8125rem", color: "var(--text-secondary)", marginBottom: "1rem" }}>
            Run ID: <code style={{ color: "var(--text-primary)", backgroundColor: "var(--bg-card)", padding: "0.1rem 0.3rem", borderRadius: "var(--radius-sm)" }}>{runResult.runId}</code> | Source: {runResult.sourceReference}
          </div>
          <table style={{ fontSize: "0.8125rem" }}>
            <thead>
              <tr>
                <th style={{ padding: "0.5rem", borderBottom: "1px solid var(--border-color)" }}>Stage</th>
                <th style={{ padding: "0.5rem", borderBottom: "1px solid var(--border-color)" }}>Rows Out</th>
              </tr>
            </thead>
            <tbody>
              {runResult.stageSummaries?.map((stage, idx) => (
                <tr key={idx}>
                  <td style={{ padding: "0.5rem", borderBottom: "1px solid var(--border-color)", fontWeight: 500 }}>{stage?.stageName}</td>
                  <td style={{ padding: "0.5rem", borderBottom: "1px solid var(--border-color)" }}>{stage?.outputRowCount?.toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}
    </div>
  );
};

