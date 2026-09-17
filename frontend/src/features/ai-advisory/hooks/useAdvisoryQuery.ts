import { useState, useCallback } from "react";
import { queryAdvisory, unlockConfidentialExposure } from "../api/advisoryApi";
import type { AdvisoryResultDto } from "../model/types";

export function useAdvisoryQuery(initialRunId = "run-demo-001", userRole = "DataSteward") {
  const [runId, setRunId] = useState(initialRunId);
  const [query, setQuery] = useState("");
  const [result, setResult] = useState<AdvisoryResultDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isUnlockedConfidential, setIsUnlockedConfidential] = useState(false);
  const [isUnlockModalOpen, setIsUnlockModalOpen] = useState(false);

  const askQuestion = useCallback(
    async (questionText: string, questionType = "General") => {
      if (!questionText.trim()) return;

      try {
        setIsLoading(true);
        setError(null);
        const data = await queryAdvisory({
          runId,
          questionType,
          userQuestion: questionText.trim(),
          userId: "current-user",
          userRole,
          unlockConfidential: isUnlockedConfidential
        });
        setResult(data);
      } catch (err: any) {
        const errorMsg =
          err?.response?.data?.errors?.[0] ||
          err?.response?.data ||
          err?.message ||
          "Failed to execute advisory query.";
        setError(typeof errorMsg === "string" ? errorMsg : JSON.stringify(errorMsg));
      } finally {
        setIsLoading(false);
      }
    },
    [runId, userRole, isUnlockedConfidential]
  );

  const handleUnlockConfirm = useCallback(
    async (reason: string) => {
      await unlockConfidentialExposure({
        runId,
        userId: "current-user",
        reason,
        userRole
      });
      setIsUnlockedConfidential(true);
    },
    [runId, userRole]
  );

  return {
    runId,
    setRunId,
    query,
    setQuery,
    result,
    isLoading,
    error,
    isUnlockedConfidential,
    isUnlockModalOpen,
    setIsUnlockModalOpen,
    askQuestion,
    handleUnlockConfirm
  };
}

