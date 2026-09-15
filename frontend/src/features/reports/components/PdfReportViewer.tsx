import React, { useEffect, useState } from "react";
import { useReportBlob } from "../hooks/useReportBlob";

export const PdfReportViewer: React.FC<{ reportId?: string }> = ({ reportId = "" }) => {
  const { data } = useReportBlob(reportId, "pdf");
  const [url, setUrl] = useState<string | null>(null);

  useEffect(() => {
    if (data) {
      const objectUrl = URL.createObjectURL(data);
      setUrl(objectUrl);
      return () => URL.revokeObjectURL(objectUrl);
    }
  }, [data]);

  if (!url) return <p>No PDF loaded.</p>;
  return <iframe title="pdf-report" src={url} style={{ width: "100%", height: "80vh" }} />;
};
