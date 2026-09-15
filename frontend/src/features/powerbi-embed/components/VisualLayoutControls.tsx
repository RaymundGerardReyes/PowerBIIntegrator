import React from "react";
import type { CustomVisualLayout } from "../model/layoutTypes";

interface VisualLayoutControlsProps {
  visualName: string;
  layout: CustomVisualLayout;
  onChange: (layout: CustomVisualLayout) => void;
}

export const VisualLayoutControls: React.FC<VisualLayoutControlsProps> = ({ visualName, layout, onChange }) => (
  <fieldset>
    <legend>{visualName}</legend>
    {(["x", "y", "width", "height"] as const).map((field) => (
      <label key={field}>
        {field}
        <input
          type="number"
          value={layout[field]}
          onChange={(e) => onChange({ ...layout, [field]: Number(e.target.value) })}
        />
      </label>
    ))}
  </fieldset>
);
