import { create } from "zustand";
import type { ChatMessage, LlmProviderPreference } from "./types";

interface LlmAssistantState {
  isOpen: boolean;
  providerPreference: LlmProviderPreference;
  sensitiveMode: boolean;
  activePolicyId: string;
  messages: ChatMessage[];
  togglePanel: () => void;
  setOpen: (open: boolean) => void;
  setProviderPreference: (pref: LlmProviderPreference) => void;
  setSensitiveMode: (enabled: boolean) => void;
  setActivePolicyId: (policyId: string) => void;
  addMessage: (msg: ChatMessage) => void;
  clearMessages: () => void;
}

export const useLlmAssistantStore = create<LlmAssistantState>((set) => ({
  isOpen: false,
  providerPreference: "LocalOllama",
  sensitiveMode: false,
  activePolicyId: "default",
  messages: [],
  togglePanel: () => set((state) => ({ isOpen: !state.isOpen })),
  setOpen: (open) => set({ isOpen: open }),
  setProviderPreference: (pref) => set({ providerPreference: pref }),
  setSensitiveMode: (enabled) => set({ sensitiveMode: enabled }),
  setActivePolicyId: (policyId) => set({ activePolicyId: policyId }),
  addMessage: (msg) => set((state) => ({ messages: [...state.messages, msg] })),
  clearMessages: () => set({ messages: [] })
}));
