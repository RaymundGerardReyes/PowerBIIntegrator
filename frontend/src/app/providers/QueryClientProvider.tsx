import React from "react";
import { QueryClient, QueryClientProvider as TanstackProvider } from "@tanstack/react-query";

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, staleTime: 30_000 } }
});

export const QueryClientProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <TanstackProvider client={queryClient}>{children}</TanstackProvider>
);
