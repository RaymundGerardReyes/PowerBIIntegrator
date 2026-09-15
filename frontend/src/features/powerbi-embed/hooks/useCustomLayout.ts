import { useCallback, useState } from "react";
import { models } from "powerbi-client";
import type { CustomLayoutConfig } from "../model/layoutTypes";

const displayOptionMap: Record<CustomLayoutConfig["displayOption"], models.DisplayOption> = {
  FitToPage: models.DisplayOption.FitToPage,
  FitToWidth: models.DisplayOption.FitToWidth,
  ActualSize: models.DisplayOption.ActualSize
};

export function useCustomLayout(initial: CustomLayoutConfig) {
  const [layout, setLayout] = useState<CustomLayoutConfig>(initial);

  const toPowerBiSettings = useCallback((): models.ISettings => {
    const visualsLayout: Record<string, models.IVisualLayout> = {};
    for (const [name, v] of Object.entries(layout.visualsLayout)) {
      visualsLayout[name] = {
        x: v.x,
        y: v.y,
        z: v.z,
        width: v.width,
        height: v.height,
        displayState: {
          mode:
            v.displayState === "Hidden"
              ? models.VisualContainerDisplayMode.Hidden
              : models.VisualContainerDisplayMode.Visible
        }
      };
    }

    return {
      layoutType: models.LayoutType.Custom,
      customLayout: {
        pageSize: {
          type: models.PageSizeType.Custom,
          width: layout.pageSize.width,
          height: layout.pageSize.height
        } as models.ICustomPageSize,
        displayOption: displayOptionMap[layout.displayOption],
        pagesLayout: { default: { visualsLayout } as unknown as models.IPageLayout }
      }
    };
  }, [layout]);

  return { layout, setLayout, toPowerBiSettings };
}
