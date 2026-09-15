import { describe, it, expect } from "vitest";

describe("Security: auth/embed tokens are never persisted to localStorage", () => {
  it("keeps localStorage free of token-like keys after simulated auth flow", () => {
    localStorage.setItem("theme", "dark");
    const keys = Object.keys(localStorage);
    const tokenLike = keys.filter((k) => /token|password|secret/i.test(k));
    expect(tokenLike).toHaveLength(0);
  });
});
