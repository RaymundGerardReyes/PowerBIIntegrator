import React from "react";
import type { Visual } from "@entities/visual/types";
import { useLayoutEditor } from "../hooks/useLayoutEditor";

interface VisualLayoutEditorProps {
  pageName: string;
  visual: Visual;
}

export const VisualLayoutEditor: React.FC<VisualLayoutEditorProps> = ({ pageName, visual }) => {
  const { updateVisualLayout } = useLayoutEditor();

  return (
    <div
      data-testid={`visual-${visual.name}`}
      style={{
        position: "absolute",
        left: visual.layout.x,
        top: visual.layout.y,
        width: visual.layout.width,
        height: visual.layout.height
      }}
    >
      <span>{visual.name}</span>
      <button
        onClick={() => updateVisualLayout(pageName, visual.name, { x: visual.layout.x + 10 })}
        aria-label={`move-${visual.name}`}
      >
        Nudge
      </button>
    </div>
  );
};
