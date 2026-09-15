import React from "react";
import { AuthProvider } from "./providers/AuthProvider";
import { QueryClientProvider } from "./providers/QueryClientProvider";
import { ThemeProvider } from "./providers/ThemeProvider";
import { AppRouter } from "./routes/AppRouter";

export const App: React.FC = () => (
  <ThemeProvider>
    <AuthProvider>
      <QueryClientProvider>
        <AppRouter />
      </QueryClientProvider>
    </AuthProvider>
  </ThemeProvider>
);
