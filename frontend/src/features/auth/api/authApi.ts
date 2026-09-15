import { apiClient } from "@shared/lib/http/apiClient";
import type { User } from "@entities/user/types";

export async function login(email: string, password: string): Promise<User> {
  const { data } = await apiClient.post<User>("/api/auth/login", { email, password });
  return data;
}
