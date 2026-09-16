import { describe, it, expect } from "vitest";

export type BumpType = "major" | "minor" | "patch";

export function determineBumpType(commitMessages: string[]): BumpType {
  const text = commitMessages.join("\n");
  if (/(BREAKING CHANGE:|BREAKING-CHANGE:|^[a-zA-Z]+(\([^\)]+\))?!:)/m.test(text)) {
    return "major";
  }
  if (/^feat(\([^\)]+\))?:/m.test(text)) {
    return "minor";
  }
  return "patch";
}

export function parseSemVer(tag: string): { major: number; minor: number; patch: number } {
  if (!/^v\d+\.\d+\.\d+$/.test(tag)) {
    throw new Error(`Invalid SemVer tag: ${tag}. Expected format vMAJOR.MINOR.PATCH`);
  }
  const [major, minor, patch] = tag.slice(1).split(".").map(Number);
  return { major, minor, patch };
}

export function computeNextTag(prevTag: string, commitMessages: string[]): string {
  const { major, minor, patch } = parseSemVer(prevTag);
  const bump = determineBumpType(commitMessages);

  switch (bump) {
    case "major":
      return `v${major + 1}.0.0`;
    case "minor":
      return `v${major}.${minor + 1}.0`;
    case "patch":
      return `v${major}.${minor}.${patch + 1}`;
  }
}

describe("Release Workflow & Semantic Versioning Rules", () => {
  it("parses valid vMAJOR.MINOR.PATCH tags correctly", () => {
    expect(parseSemVer("v1.0.0")).toEqual({ major: 1, minor: 0, patch: 0 });
    expect(parseSemVer("v1.1.0")).toEqual({ major: 1, minor: 1, patch: 0 });
    expect(parseSemVer("v2.4.9")).toEqual({ major: 2, minor: 4, patch: 9 });
  });

  it("throws error for non-conforming tag formats", () => {
    expect(() => parseSemVer("1.0.0")).toThrow();
    expect(() => parseSemVer("v1.0")).toThrow();
    expect(() => parseSemVer("release-1.0.0")).toThrow();
  });

  it("increments minor version for 'feat' conventional commits", () => {
    const commits = [
      "docs(mcp): update conformance checklist",
      "feat(reporting): implement multi-target document generation",
      "fix(llm): replace reader.EndOfStream"
    ];
    expect(determineBumpType(commits)).toBe("minor");
    expect(computeNextTag("v1.0.0", commits)).toBe("v1.1.0");
  });

  it("increments patch version when only 'fix', 'docs', or 'build' commits exist", () => {
    const commits = [
      "fix(compiler): resolve TMDL indentation bug",
      "docs: update API overview documentation",
      "build(props): add NuGet warning suppression"
    ];
    expect(determineBumpType(commits)).toBe("patch");
    expect(computeNextTag("v1.0.0", commits)).toBe("v1.0.1");
  });

  it("increments major version for BREAKING CHANGE in footer", () => {
    const commits = [
      "feat(ir): overhaul canonical IR entity model\n\nBREAKING CHANGE: AnalyticsModel constructor signature changed"
    ];
    expect(determineBumpType(commits)).toBe("major");
    expect(computeNextTag("v1.1.0", commits)).toBe("v2.0.0");
  });

  it("increments major version for breaking change indicator in type (feat! / refactor!)", () => {
    const commits = [
      "refactor(api)!: remove legacy data source upload endpoint"
    ];
    expect(determineBumpType(commits)).toBe("major");
    expect(computeNextTag("v1.1.0", commits)).toBe("v2.0.0");
  });

  it("correctly computes next SemVer for today's commit series as v1.1.0", () => {
    const todaysCommits = [
      "feat(reporting): implement multi-target document generation engine (PDF, Excel, Word) and expand 6-category test suite to 108 tests",
      "feat(architecture): implement remaining gap closures, Postgres connector, semantic validation, direct imports, cloud IaC, and runbooks",
      "feat(mcp): implement standalone McpServer with dual transports (stdio/sse), 9 tool adapters, and stream endpoints",
      "feat(api): map POST /api/llm/chat/stream SSE endpoint in LlmEndpoints",
      "feat(llm): implement StreamChatAsync on IOllamaClient and OllamaLocalClient",
      "fix(llm): replace reader.EndOfStream with null check to avoid CA2024 warning",
      "feat(infra): add k8s ingress, services, overlays, docs/api, zap baseline, and Reqnroll BDD scenarios"
    ];
    const nextVersion = computeNextTag("v1.0.0", todaysCommits);
    expect(nextVersion).toBe("v1.1.0");
  });
});

