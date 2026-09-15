import React from "react";
import type { CustomLayoutConfig } from "../model/layoutTypes";

interface CustomLayoutBuilderProps {
  layout: CustomLayoutConfig;
  onChange: (layout: CustomLayoutConfig) => void;
}

export const CustomLayoutBuilder: React.FC<CustomLayoutBuilderProps> = ({ layout, onChange }) => (
  <div>
    <label>
      Page width
      <input
        type="number"
        value={layout.pageSize.width}
        onChange={(e) => onChange({ ...layout, pageSize: { ...layout.pageSize, width: Number(e.target.value) } })}
      />
    </label>
    <label>
      Page height
      <input
        type="number"
        value={layout.pageSize.height}
        onChange={(e) => onChange({ ...layout, pageSize: { ...layout.pageSize, height: Number(e.target.value) } })}
      />
    </label>
  </div>
);
