import { useQuery } from "@tanstack/react-query";
import { getEmbedConfig } from "../api/embedTokenApi";

export function usePowerBiEmbed(reportId: string) {
  return useQuery({
    queryKey: ["powerbi-embed-config", reportId],
    queryFn: () => getEmbedConfig(reportId),
    enabled: !!reportId,
    staleTime: 5 * 60_000 // embed tokens are short-lived; refresh proactively
  });
}
