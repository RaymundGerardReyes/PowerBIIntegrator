import React from "react";

interface SensitiveModeToggleProps {
  enabled: boolean;
  onToggle: (enabled: boolean) => void;
}

export const SensitiveModeToggle: React.FC<SensitiveModeToggleProps> = ({ enabled, onToggle }) => {
  return (
    <div
      className={`sensitive-mode-toggle flex items-center justify-between p-2 rounded-lg border transition-all duration-200 ${
        enabled
          ? "bg-amber-500/10 border-amber-500/30 text-amber-900 dark:text-amber-200 shadow-xs"
          : "bg-gray-100/50 dark:bg-gray-800/50 border-gray-200 dark:border-gray-700 text-gray-700 dark:text-gray-300"
      }`}
      data-testid="sensitive-mode-toggle"
    >
      <div className="flex items-center gap-2">
        <span className="text-base select-none" aria-hidden="true">
          {enabled ? "🛡️" : "🌐"}
        </span>
        <div className="flex flex-col">
          <label htmlFor="sensitive-mode-checkbox" className="text-xs font-semibold cursor-pointer">
            Sensitive / Camera Data Mode
          </label>
          <span className="text-[10px] text-gray-500 dark:text-gray-400">
            {enabled ? "Zero-Data-Leak Enforced (Local Ollama)" : "Cloud & Local models available"}
          </span>
        </div>
      </div>

      <div className="relative inline-flex items-center cursor-pointer">
        <input
          type="checkbox"
          id="sensitive-mode-checkbox"
          checked={enabled}
          onChange={(e) => onToggle(e.target.checked)}
          className="sr-only peer"
          aria-label="Toggle Sensitive Data Mode"
        />
        <div className="w-8 h-4 bg-gray-300 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-3 after:w-3 after:transition-all peer-checked:bg-amber-500 dark:bg-gray-700" />
      </div>
    </div>
  );
};
