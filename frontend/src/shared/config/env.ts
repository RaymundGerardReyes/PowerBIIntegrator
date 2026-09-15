export const env = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL ?? "http://localhost:8080",
  powerBi: {
    tenantId: import.meta.env.VITE_POWERBI_TENANT_ID ?? "",
    clientId: import.meta.env.VITE_POWERBI_CLIENT_ID ?? ""
  }
} as const;
