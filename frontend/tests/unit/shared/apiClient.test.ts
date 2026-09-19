import { describe, it, expect } from "vitest";
import { apiClient } from "@shared/lib/http/apiClient";

describe("apiClient HTTP Interceptors", () => {
  it("injects X-Correlation-Id header automatically into request config", async () => {
    // Interceptors run when request is dispatched; test interceptor behavior directly
    const interceptor = (apiClient.interceptors.request as any).handlers[0].fulfilled;
    const config = {
      headers: {} as any
    };

    const transformed = await interceptor(config);
    expect(transformed.headers["X-Correlation-Id"]).toBeDefined();
    expect(typeof transformed.headers["X-Correlation-Id"]).toBe("string");
    expect(transformed.headers["X-Correlation-Id"].length).toBeGreaterThan(0);
  });

  it("sets multipart/form-data for FormData payloads to allow fetch adapter boundary delegation", async () => {
    const interceptor = (apiClient.interceptors.request as any).handlers[0].fulfilled;
    const formData = new FormData();
    formData.append("file", new Blob(["dummy content"]), "data.csv");

    const config = {
      data: formData,
      headers: {} as any
    };

    const transformed = await interceptor(config);
    expect(transformed.headers["Content-Type"]).toBe("multipart/form-data");
  });
});
