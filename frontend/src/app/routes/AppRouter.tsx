import React from "react";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { routePaths } from "./routePaths";
import { AppLayout } from "../layout/AppLayout";
import { LoginForm } from "@features/auth/components/LoginForm";
import { DashboardWorkspacePage } from "@features/dashboards/components/DashboardWorkspacePage";
import { DataSourcesPage } from "@features/data-sources/components/DataSourcesPage";
import { ReportsHubPage } from "@features/reports/components/ReportsHubPage";
import { DataQualityDashboardPage } from "@features/data-quality/components/DataQualityDashboardPage";

export const AppRouter: React.FC = () => (
  <BrowserRouter>
    <AppLayout>
      <Routes>
        <Route
          path={routePaths.login}
          element={
            <div style={{ display: "flex", justifyContent: "center", alignItems: "center", minHeight: "60vh" }}>
              <div className="card" style={{ maxWidth: "400px", width: "100%" }}>
                <h3 style={{ marginTop: 0, marginBottom: "1rem" }}>Sign In to Platform</h3>
                <LoginForm />
              </div>
            </div>
          }
        />
        <Route path={routePaths.dashboards} element={<DashboardWorkspacePage />} />
        <Route path={routePaths.dataSources} element={<DataSourcesPage />} />
        <Route path={routePaths.dataQuality} element={<DataQualityDashboardPage />} />
        <Route path={routePaths.reports} element={<ReportsHubPage />} />
        <Route path="*" element={<Navigate to={routePaths.dashboards} replace />} />
      </Routes>
    </AppLayout>
  </BrowserRouter>
);
