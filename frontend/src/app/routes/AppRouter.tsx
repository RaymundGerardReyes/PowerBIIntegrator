import React from "react";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { routePaths } from "./routePaths";
import { LoginForm } from "@features/auth/components/LoginForm";
import { DashboardCanvas } from "@features/dashboards/components/DashboardCanvas";
import { ExcelUploadForm } from "@features/data-sources/components/ExcelUploadForm";
import { PdfReportViewer } from "@features/reports/components/PdfReportViewer";

export const AppRouter: React.FC = () => (
  <BrowserRouter>
    <Routes>
      <Route path={routePaths.login} element={<LoginForm />} />
      <Route path={routePaths.dashboards} element={<DashboardCanvas />} />
      <Route path={routePaths.dataSources} element={<ExcelUploadForm />} />
      <Route path={routePaths.reports} element={<PdfReportViewer />} />
      <Route path="*" element={<Navigate to={routePaths.dashboards} replace />} />
    </Routes>
  </BrowserRouter>
);
