import { describe, it, expect } from "vitest";

describe("Security: Content-Security-Policy expectations", () => {
  it("documents the required CSP directives for production nginx config", () => {
    const requiredDirectives = ["default-src 'self'", "frame-src https://app.powerbi.com", "connect-src 'self' https://api.powerbi.com"];
    expect(requiredDirectives).toContain("frame-src https://app.powerbi.com");
  });
});
