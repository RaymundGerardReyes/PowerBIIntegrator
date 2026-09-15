import React from "react";

export const WordReportViewer: React.FC<{ reportId?: string }> = ({ reportId }) => (
  <div>
    <p>Word report preview for report: {reportId ?? "none"}</p>
    <p>Rendered server-side via DocumentFormat.OpenXml; embed as download link or converted PDF preview.</p>
  </div>
);
