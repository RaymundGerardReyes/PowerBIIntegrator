import axios from "axios";
import { env } from "@shared/config/env";

export const apiClient = axios.create({
  baseURL: env.apiBaseUrl,
  adapter: "fetch",
  headers: { "Content-Type": "application/json" }
});

apiClient.interceptors.request.use((config) => {
  const correlationId =
    typeof crypto !== "undefined" && typeof crypto.randomUUID === "function"
      ? crypto.randomUUID()
      : "client-" + Date.now();

  if (typeof config.headers?.set === "function") {
    config.headers.set("X-Correlation-Id", correlationId);
  } else {
    config.headers["X-Correlation-Id"] = correlationId;
  }

  // For FormData, set multipart/form-data so Axios fetch adapter recognizes it and delegates boundary computation to fetch()
  if (config.data instanceof FormData) {
    if (typeof config.headers?.setContentType === "function") {
      config.headers.setContentType("multipart/form-data");
    } else {
      config.headers["Content-Type"] = "multipart/form-data";
    }
  }

  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    console.error("[apiClient] request failed", error?.response?.status, error?.config?.url);
    return Promise.reject(error);
  }
);
