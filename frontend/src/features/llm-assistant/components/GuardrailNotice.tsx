import React from "react";

interface GuardrailNoticeProps {
  message?: string;
  isBlocked?: boolean;
}

export const GuardrailNotice: React.FC<GuardrailNoticeProps> = ({ message, isBlocked }) => {
  if (!message) return null;

  const bgClass = isBlocked ? "bg-red-50 text-red-800 border-red-200" : "bg-amber-50 text-amber-800 border-amber-200";

  return (
    <div
      role="alert"
      className={`p-2 my-1 border rounded text-xs flex items-center space-x-2 ${bgClass}`}
      data-testid="guardrail-notice"
    >
      <span className="font-semibold">{isBlocked ? "[BLOCKED]" : "[GUARDRAIL NOTICE]"}</span>
      <span>{message}</span>
    </div>
  );
};
