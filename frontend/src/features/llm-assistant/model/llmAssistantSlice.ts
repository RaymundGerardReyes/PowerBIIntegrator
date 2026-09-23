import { create } from "zustand";
import type { ChatMessage, LlmProviderPreference, LiveStreamingStatus, DockMode } from "./types";

interface LlmAssistantState {
  isOpen: boolean;
  dockMode: DockMode;
  providerPreference: LlmProviderPreference;
  sensitiveMode: boolean;
  activePolicyId: string;
  isVoiceActive: boolean;
  liveStatus: LiveStreamingStatus;
  messages: ChatMessage[];
  togglePanel: () => void;
  setOpen: (open: boolean) => void;
  setDockMode: (mode: DockMode) => void;
  setProviderPreference: (pref: LlmProviderPreference) => void;
  setSensitiveMode: (enabled: boolean) => void;
  setActivePolicyId: (policyId: string) => void;
  toggleVoice: () => void;
  setVoiceActive: (active: boolean) => void;
  setLiveStatus: (status: LiveStreamingStatus) => void;
  addMessage: (msg: ChatMessage) => void;
  clearMessages: () => void;
}

export const useLlmAssistantStore = create<LlmAssistantState>((set) => ({
  isOpen: false,
  dockMode: "docked",
  providerPreference: "LocalOllama",
  sensitiveMode: false,
  activePolicyId: "default",
  isVoiceActive: false,
  liveStatus: "idle",
  messages: [],
  togglePanel: () => set((state) => ({ isOpen: !state.isOpen })),
  setOpen: (open) => set({ isOpen: open }),
  setDockMode: (mode) => set({ dockMode: mode }),
  setProviderPreference: (pref) => set({ providerPreference: pref }),
  setSensitiveMode: (enabled) =>
    set((state) => ({
      sensitiveMode: enabled,
      providerPreference: enabled && state.providerPreference !== "LocalOllama"
        ? "LocalOllama"
        : state.providerPreference
    })),
  setActivePolicyId: (policyId) => set({ activePolicyId: policyId }),
  toggleVoice: () =>
    set((state) => ({
      isVoiceActive: !state.isVoiceActive,
      liveStatus: !state.isVoiceActive ? "listening" : "idle"
    })),
  setVoiceActive: (active) =>
    set({
      isVoiceActive: active,
      liveStatus: active ? "listening" : "idle"
    }),
  setLiveStatus: (status) => set({ liveStatus: status }),
  addMessage: (msg) => set((state) => ({ messages: [...state.messages, msg] })),
  clearMessages: () => set({ messages: [] })
}));
