import axios from "axios";
import { env } from "@shared/config/env";

export const apiClient = axios.create({
  baseURL: env.apiBaseUrl,
  adapter: "fetch",
  headers: { "Content-Type": "application/json" }
});

apiClient.interceptors.request.use((config) => {
  const correlationId = crypto.randomUUID();
  config.headers["X-Correlation-Id"] = correlationId;
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    console.error("[apiClient] request failed", error?.response?.status, error?.config?.url);
    return Promise.reject(error);
  }
);
