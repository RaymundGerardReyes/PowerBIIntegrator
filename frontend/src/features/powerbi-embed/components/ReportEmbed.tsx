import React, { useEffect, useRef, useState } from "react";
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
  const [embedError, setEmbedError] = useState<string | null>(null);

  const isMockToken =
    !config.accessToken ||
    config.accessToken.startsWith("mock-") ||
    config.accessToken.startsWith("dummy-") ||
    config.accessToken.includes("dummy");

  useEffect(() => {
    if (!containerRef.current) return;

    if (isMockToken) {
      setEmbedError(
        "Demonstration Mode: A placeholder embed token was supplied. Real-time Power BI iframe rendering requires valid Azure Entra ID / Microsoft Fabric credentials."
      );
      return;
    }

    setEmbedError(null);

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

    const embedComponent = serviceRef.current.embed(containerRef.current, embedConfig);

    if (embedComponent && typeof (embedComponent as { on?: unknown }).on === "function") {
      (embedComponent as { on: (event: string, handler: (e: { detail?: { message?: string } }) => void) => void }).on(
        "error",
        (event) => {
          setEmbedError(event?.detail?.message ?? "Power BI Service report load error.");
        }
      );
    }

    return () => {
      if (containerRef.current) serviceRef.current?.reset(containerRef.current);
    };
  }, [config, settings, isMockToken]);

  return (
    <div style={{ width: "100%", height: "100%", position: "relative" }}>
      <div data-testid="powerbi-report-container" style={{ width: "100%", height: "100%" }} ref={containerRef} />
      {embedError && (
        <div
          data-testid="embed-demo-notice"
          style={{
            position: "absolute",
            inset: 0,
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            justifyContent: "center",
            backgroundColor: "rgba(15, 23, 42, 0.85)",
            backdropFilter: "blur(4px)",
            borderRadius: "8px",
            padding: "2rem",
            textAlign: "center",
            color: "#f8fafc",
            gap: "1rem"
          }}
        >
          <div
            style={{
              width: "48px",
              height: "48px",
              borderRadius: "50%",
              backgroundColor: "rgba(245, 158, 11, 0.2)",
              color: "#f59e0b",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              fontSize: "1.5rem"
            }}
          >
            ⚡
          </div>
          <h3 style={{ margin: 0, fontSize: "1.125rem", fontWeight: 600 }}>Power BI Embedded Preview</h3>
          <p style={{ margin: 0, maxWidth: "520px", fontSize: "0.875rem", color: "#94a3b8", lineHeight: 1.5 }}>
            {embedError}
          </p>
          <div
            style={{
              backgroundColor: "rgba(30, 41, 59, 0.7)",
              border: "1px solid rgba(255, 255, 255, 0.1)",
              borderRadius: "6px",
              padding: "0.75rem 1rem",
              fontSize: "0.75rem",
              fontFamily: "monospace",
              color: "#cbd5e1"
            }}
          >
            Report ID: {config.reportId} | Token Type: Embed (Mock / Local Dev)
          </div>
        </div>
      )}
    </div>
  );
};
