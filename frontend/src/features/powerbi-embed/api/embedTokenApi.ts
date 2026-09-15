import { apiClient } from "@shared/lib/http/apiClient";

export interface EmbedConfig {
  reportId: string;
  embedUrl: string;
  accessToken: string;
}

export async function getEmbedConfig(reportId: string): Promise<EmbedConfig> {
  const { data } = await apiClient.get<EmbedConfig>(`/api/powerbi/embed-config/${reportId}`);
  return data;
}
