import React, { useState } from "react";
import { ProfileSummaryPanel } from "./ProfileSummaryPanel";
import { DuplicateReviewTable, DuplicateReviewItem } from "./DuplicateReviewTable";
import { CleaningRuleEditor } from "./CleaningRuleEditor";
import { TransformationPlanBuilder } from "./TransformationPlanBuilder";
import { ChartSuggestionPanel } from "./ChartSuggestionPanel";
import { AdvisoryPanel } from "@features/ai-advisory";
import { usePipelineRun } from "../hooks/usePipelineRun";
import { Button } from "@shared/ui/Button/Button";
import { Badge } from "@shared/ui/Badge/Badge";
import { Card } from "@shared/ui/Card/Card";

export const DataQualityDashboardPage: React.FC = () => {
  const [activeStage, setActiveStage] = useState<"profile" | "dedupe" | "clean" | "transform" | "visuals" | "advisory">("profile");
  const {
    profile,
    runResult,
    suggestions,
    isRunning,
    error,
    runProfiling,
    executeFullPipeline
  } = usePipelineRun();

  const [mockClusters, setMockClusters] = useState<DuplicateReviewItem[]>([
    {
      clusterId: "cluster_1",
      ruleFired: "ExactHashDedupe",
      keptRowId: "row_101",
      droppedRowIds: ["row_108", "row_114"],
      confidenceScore: 1.0,
      reasonCode: "Exact SHA-256 row payload match on [CustomerName, InvoiceNo, Date]"
    },
    {
      clusterId: "cluster_2",
      ruleFired: "SimilarityClusterDedupe",
      keptRowId: "row_204",
      droppedRowIds: ["row_211"],
      confidenceScore: 0.92,
      reasonCode: "Levenshtein similarity 92% on free-text column 'CompanyName'"
    }
  ]);

  const handleKeepOverride = (clusterId: string, newKeptRowId: string) => {
    setMockClusters((prev) =>
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
    runProfiling("sources/enterprise_sales_2026.csv", "EnterpriseSales2026");
  };

  const handleRunFull = () => {
    executeFullPipeline("sources/enterprise_sales_2026.csv", "EnterpriseSales2026", "Gold_Fact_Sales");
  };

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "var(--space-6)" }}>
      {/* Page Header */}
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "flex-start",
          flexWrap: "wrap",
          gap: "var(--space-4)"
        }}
      >
        <div>
          <div style={{ display: "flex", alignItems: "center", gap: "var(--space-2)", marginBottom: "var(--space-1)" }}>
            <h2 style={{ margin: 0 }}>Data Quality & Transformation Engine</h2>
            <Badge variant="info">Deterministic</Badge>
          </div>
          <p style={{ margin: 0, color: "var(--text-secondary)", fontSize: "0.875rem" }}>
            Rule-driven Medallion staging (Bronze → Silver → Gold) with explainable deduplication, declarative cleaning, and heuristic visual suggestions.
          </p>
        </div>

        <div style={{ display: "flex", gap: "var(--space-2)" }}>
          <Button variant="secondary" onClick={handleStartProfiling} disabled={isRunning}>
            {isRunning ? "Profiling..." : "Profile Sample Dataset"}
          </Button>
          <Button variant="primary" onClick={handleRunFull} disabled={isRunning}>
            {isRunning ? "Executing..." : "Run Full Medallion Pipeline"}
          </Button>
        </div>
      </div>

      {error && (
        <Card style={{ backgroundColor: "var(--danger-bg)", borderColor: "var(--danger-border)" }}>
          <p style={{ margin: 0, color: "var(--danger)", fontSize: "0.875rem", fontWeight: 500 }}>
            {error}
          </p>
        </Card>
      )}

      {/* Stage Navigation Pills */}
      <div className="tab-list" role="tablist">
        <button
          className={`tab-item ${activeStage === "profile" ? "tab-item-active" : ""}`}
          onClick={() => setActiveStage("profile")}
          role="tab"
          aria-selected={activeStage === "profile"}
        >
          1. Profiling & Contracts
        </button>
        <button
          className={`tab-item ${activeStage === "dedupe" ? "tab-item-active" : ""}`}
          onClick={() => setActiveStage("dedupe")}
          role="tab"
          aria-selected={activeStage === "dedupe"}
        >
          2. Deduplication Review ({mockClusters.length})
        </button>
        <button
          className={`tab-item ${activeStage === "clean" ? "tab-item-active" : ""}`}
          onClick={() => setActiveStage("clean")}
          role="tab"
          aria-selected={activeStage === "clean"}
        >
          3. Declarative Cleaning
        </button>
        <button
          className={`tab-item ${activeStage === "transform" ? "tab-item-active" : ""}`}
          onClick={() => setActiveStage("transform")}
          role="tab"
          aria-selected={activeStage === "transform"}
        >
          4. Silver → Gold Transformation
        </button>
        <button
          className={`tab-item ${activeStage === "visuals" ? "tab-item-active" : ""}`}
          onClick={() => setActiveStage("visuals")}
          role="tab"
          aria-selected={activeStage === "visuals"}
        >
          5. Chart Suggestions
        </button>
        <button
          className={`tab-item ${activeStage === "advisory" ? "tab-item-active" : ""}`}
          onClick={() => setActiveStage("advisory")}
          role="tab"
          aria-selected={activeStage === "advisory"}
        >
          💡 6. AI Advisory (Read-Only)
        </button>
      </div>

      {/* Stage Content */}
      <div>
        {activeStage === "profile" && (
          <div style={{ display: "flex", flexDirection: "column", gap: "var(--space-4)" }}>
            <ProfileSummaryPanel profile={profile} isLoading={isRunning} />
          </div>
        )}

        {activeStage === "dedupe" && (
          <div style={{ display: "flex", flexDirection: "column", gap: "var(--space-4)" }}>
            <DuplicateReviewTable clusters={mockClusters} onKeepOverride={handleKeepOverride} />
          </div>
        )}

        {activeStage === "clean" && (
          <div style={{ display: "flex", flexDirection: "column", gap: "var(--space-4)" }}>
            <CleaningRuleEditor />
          </div>
        )}

        {activeStage === "transform" && (
          <div style={{ display: "flex", flexDirection: "column", gap: "var(--space-4)" }}>
            <TransformationPlanBuilder />
          </div>
        )}

        {activeStage === "visuals" && (
          <div style={{ display: "flex", flexDirection: "column", gap: "var(--space-4)" }}>
            <ChartSuggestionPanel suggestions={suggestions} />
          </div>
        )}

        {activeStage === "advisory" && (
          <div style={{ display: "flex", flexDirection: "column", gap: "var(--space-4)", minHeight: "650px" }}>
            <AdvisoryPanel runId={runResult?.runId || "run-demo-001"} userRole="DataSteward" />
          </div>
        )}
      </div>

      {/* Run Summary Card if available */}
      {runResult && (
        <Card style={{ backgroundColor: "var(--bg-subtle)" }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "var(--space-2)" }}>
            <h4 style={{ margin: 0 }}>Pipeline Run Telemetry</h4>
            <Badge variant="success">Completed</Badge>
          </div>
          <p style={{ margin: 0, fontSize: "0.8125rem", color: "var(--text-secondary)" }}>
            Run ID: <code style={{ color: "var(--primary)" }}>{runResult.runId}</code> | Source: {runResult.sourceReference}
          </p>
          <div style={{ display: "flex", gap: "var(--space-6)", marginTop: "var(--space-3)" }}>
            {runResult.stageSummaries.map((stage, idx) => (
              <div key={idx} style={{ fontSize: "0.75rem" }}>
                <span style={{ fontWeight: 600, color: "var(--text-primary)" }}>{stage.stageName}</span>: {stage.outputRowCount} rows
              </div>
            ))}
          </div>
        </Card>
      )}
    </div>
  );
};

