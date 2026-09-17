import React, { useState, useEffect } from "react";
import { LayoutGrid } from "@shared/ui/LayoutGrid/LayoutGrid";
import { PageSelector } from "./PageSelector";
import { VisualLayoutEditor } from "./VisualLayoutEditor";
import { useDashboardStore } from "../model/dashboardSlice";

export const DashboardCanvas: React.FC = () => {
  const dashboard = useDashboardStore((s) => s.current);
  const [selectedPage, setSelectedPage] = useState<string>(dashboard?.pages[0]?.name ?? "");

  useEffect(() => {
    if (dashboard?.pages && dashboard.pages.length > 0) {
      if (!dashboard.pages.some((p) => p.name === selectedPage)) {
        setSelectedPage(dashboard.pages[0].name);
      }
    }
  }, [dashboard, selectedPage]);

  if (!dashboard) return <p>No dashboard loaded.</p>;

  const page = dashboard.pages.find((p) => p.name === selectedPage) ?? dashboard.pages[0];

  return (
    <div>
      <PageSelector pages={dashboard.pages} selected={page.name} onSelect={setSelectedPage} />
      <LayoutGrid width={page.canvasWidth} height={page.canvasHeight}>
        {page.visuals.map((visual) => (
          <VisualLayoutEditor key={visual.name} pageName={page.name} visual={visual} />
        ))}
      </LayoutGrid>
    </div>
  );
};
