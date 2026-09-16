import { describe, it, expect } from "vitest";
import * as powerBiApi from "@features/powerbi-embed/api/powerBiApi";

describe("Power BI API integration", () => {
  it("compiles PBIR definition files via the mocked backend contract", async () => {
    const result = await powerBiApi.compilePbir({
      dashboardDefinitionId: "00000000-0000-0000-0000-000000000001",
      semanticModelRelativePath: "../definition"
    });

    expect(result.reportId).toBe("report-1");
    expect(result.files["definition/report.json"]).toBeDefined();
  });

  it("compiles TMDL semantic model via the mocked backend contract", async () => {
    const result = await powerBiApi.compileTmdl({
      analyticsModelId: "00000000-0000-0000-0000-000000000002"
    });

    expect(result.modelName).toBe("SalesModel");
    expect(result.tables?.length).toBe(1);
  });

  it("compiles complete PBIP project archive via mocked backend contract", async () => {
    const result = await powerBiApi.compilePbip({
      dashboardDefinitionId: "00000000-0000-0000-0000-000000000001",
      analyticsModelId: "00000000-0000-0000-0000-000000000002",
      projectName: "TestProject"
    });

    expect(result.projectName).toBe("TestProject");
    expect(result.pbirDefinition.reportId).toBe("report-1");
    expect(result.tmdlModel.modelName).toBe("TestModel");
  });

  it("downloads PBIP package zip stream", async () => {
    const blob = await powerBiApi.downloadPbip({
      dashboardDefinitionId: "00000000-0000-0000-0000-000000000001",
      analyticsModelId: "00000000-0000-0000-0000-000000000002",
      projectName: "TestProject"
    });

    expect(blob).toBeDefined();
    expect(blob.type).toBe("application/zip");
  });

  it("imports direct artifact via multipart upload", async () => {
    const file = new File(["dummy pbix binary content"], "SalesDashboard.pbix", {
      type: "application/octet-stream"
    });

    const result = await powerBiApi.importArtifact(
      "00000000-0000-0000-0000-000000000003",
      "SalesDashboard",
      file
    );

    expect(result.importId).toBe("import-1");
    expect(result.displayName).toBe("ImportedDataset");
    expect(result.importState).toBe("Succeeded");
  });
});

