import React, { useEffect, useRef } from "react";
import * as powerbiClient from "powerbi-client";
import { models, type IEmbedSettings, type IEmbedConfiguration } from "powerbi-client";
import type { EmbedConfig } from "../api/embedTokenApi";

interface ReportEmbedProps {
  config: EmbedConfig;
  settings?: IEmbedSettings;
}

export const ReportEmbed: React.FC<ReportEmbedProps> = ({ config, settings }) => {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const serviceRef = useRef<powerbiClient.service.Service | null>(null);

  useEffect(() => {
    if (!containerRef.current) return;

    serviceRef.current ??= new powerbiClient.service.Service(
      powerbiClient.factories.hpmFactory,
      powerbiClient.factories.wpmpFactory,
      powerbiClient.factories.routerFactory
    );

    const embedConfig: IEmbedConfiguration = {
      type: "report",
      id: config.reportId,
      embedUrl: config.embedUrl,
      accessToken: config.accessToken,
      tokenType: models.TokenType.Embed,
      settings
    };

    serviceRef.current.embed(containerRef.current, embedConfig);

    return () => {
      if (containerRef.current) serviceRef.current?.reset(containerRef.current);
    };
  }, [config, settings]);

  return <div data-testid="powerbi-report-container" style={{ width: "100%", height: "100%" }} ref={containerRef} />;
};
