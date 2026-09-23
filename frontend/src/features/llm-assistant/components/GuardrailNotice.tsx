import React from "react";

interface GuardrailNoticeProps {
  message?: string;
  isBlocked?: boolean;
}

export const GuardrailNotice: React.FC<GuardrailNoticeProps> = ({ message, isBlocked }) => {
  if (!message) return null;

  const styleConfig = isBlocked
    ? {
        container: "bg-rose-50/90 dark:bg-rose-950/60 text-rose-900 dark:text-rose-200 border-rose-300 dark:border-rose-800",
        badge: "bg-rose-600 text-white",
        icon: "🛑"
      }
    : {
        container: "bg-amber-50/90 dark:bg-amber-950/60 text-amber-900 dark:text-amber-200 border-amber-300 dark:border-amber-800",
        badge: "bg-amber-600 text-white",
        icon: "⚠️"
      };

  return (
    <div
      role="alert"
      aria-live="polite"
      className={`p-2.5 my-2 border rounded-lg text-xs flex items-start space-x-2.5 backdrop-blur-xs shadow-2xs transition-all ${styleConfig.container}`}
      data-testid="guardrail-notice"
    >
      <span className="text-sm select-none shrink-0" aria-hidden="true">
        {styleConfig.icon}
      </span>
      <div className="flex-1 flex flex-col space-y-0.5">
        <div className="flex items-center gap-1.5">
          <span className={`font-bold text-[10px] uppercase px-1.5 py-0.2 rounded font-mono ${styleConfig.badge}`}>
            {isBlocked ? "[BLOCKED]" : "[GUARDRAIL NOTICE]"}
          </span>
          <span className="text-[10px] text-gray-500 dark:text-gray-400 font-medium">
            AI Policy Guardrail
          </span>
        </div>
        <p className="text-xs leading-relaxed m-0">{message}</p>
      </div>
    </div>
  );
};
