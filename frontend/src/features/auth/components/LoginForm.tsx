import React, { useState } from "react";
import { useAuth } from "../hooks/useAuth";
import { login } from "../api/authApi";
import { Button } from "@shared/ui/Button/Button";

export const LoginForm: React.FC = () => {
  const { login: setSession } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const user = await login(email, password);
    setSession(user);
  };

  return (
    <form onSubmit={handleSubmit} aria-label="login-form">
      <input value={email} onChange={(e) => setEmail(e.target.value)} placeholder="Email" type="email" />
      <input value={password} onChange={(e) => setPassword(e.target.value)} placeholder="Password" type="password" />
      <Button type="submit">Sign in</Button>
    </form>
  );
};
