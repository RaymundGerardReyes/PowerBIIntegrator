import React from "react";

interface SensitiveModeToggleProps {
  enabled: boolean;
  onToggle: (enabled: boolean) => void;
}

export const SensitiveModeToggle: React.FC<SensitiveModeToggleProps> = ({ enabled, onToggle }) => {
  return (
    <div className="sensitive-mode-toggle flex items-center space-x-2" data-testid="sensitive-mode-toggle">
      <input
        type="checkbox"
        id="sensitive-mode-checkbox"
        checked={enabled}
        onChange={(e) => onToggle(e.target.checked)}
        className="rounded text-indigo-600 focus:ring-indigo-500"
      />
      <label htmlFor="sensitive-mode-checkbox" className="text-sm font-medium text-gray-700">
        Sensitive / Camera Data Mode
      </label>
    </div>
  );
};
