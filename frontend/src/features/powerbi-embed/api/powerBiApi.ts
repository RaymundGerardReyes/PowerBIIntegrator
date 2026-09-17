import { apiClient } from "@shared/lib/http/apiClient";
import type {
  CompilePbipRequest,
  PbipCompilationResultDto,
  CompilePbirRequest,
  PbirReportDefinitionDto,
  CompileTmdlRequest,
  TmdlSemanticModelDto,
  PublishRequest,
  FabricPublishResultDto,
  ImportPowerBiArtifactResponseDto,
  LocalPowerBiStatusDto,
  LaunchLocalPowerBiRequest,
  LaunchProjectResultDto,
  OpenLocalPowerBiFolderRequest
} from "@shared/types/api-contracts";

export async function compilePbip(payload: CompilePbipRequest): Promise<PbipCompilationResultDto> {
  const { data } = await apiClient.post<PbipCompilationResultDto>("/api/powerbi/compile-pbip", payload);
  return data;
}

export async function downloadPbip(payload: CompilePbipRequest): Promise<Blob> {
  const response = await apiClient.post<Blob>("/api/powerbi/compile-pbip/download", payload, {
    responseType: "blob"
  });
  return response.data;
}

export async function compilePbir(payload: CompilePbirRequest): Promise<PbirReportDefinitionDto> {
  const { data } = await apiClient.post<PbirReportDefinitionDto>("/api/powerbi/compile-pbir", payload);
  return data;
}

export async function compileTmdl(payload: CompileTmdlRequest): Promise<TmdlSemanticModelDto> {
  const { data } = await apiClient.post<TmdlSemanticModelDto>("/api/powerbi/compile-tmdl", payload);
  return data;
}

export async function publishPbip(payload: PublishRequest): Promise<FabricPublishResultDto> {
  const { data } = await apiClient.post<FabricPublishResultDto>("/api/powerbi/publish", payload);
  return data;
}

export async function importArtifact(
  workspaceId: string,
  datasetDisplayName: string,
  file: File
): Promise<ImportPowerBiArtifactResponseDto> {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("workspaceId", workspaceId);
  formData.append("datasetDisplayName", datasetDisplayName);

  const { data } = await apiClient.post<ImportPowerBiArtifactResponseDto>("/api/powerbi/import", formData);
  return data;
}

export async function getLocalPowerBiStatus(): Promise<LocalPowerBiStatusDto> {
  const { data } = await apiClient.get<LocalPowerBiStatusDto>("/api/powerbi/desktop/status");
  return data;
}

export async function launchLocalPowerBi(payload: LaunchLocalPowerBiRequest): Promise<LaunchProjectResultDto> {
  const { data } = await apiClient.post<LaunchProjectResultDto>("/api/powerbi/desktop/launch", payload);
  return data;
}

export async function openLocalPowerBiFolder(folderPath?: string): Promise<{ success: boolean }> {
  const { data } = await apiClient.post<{ success: boolean }>("/api/powerbi/desktop/open-folder", { folderPath });
  return data;
}
