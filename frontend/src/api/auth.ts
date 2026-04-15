import { apiFetch } from "./client";
import type { ApiResult, AuthResponse, LoginRequest } from "./types";

export async function login(request: LoginRequest): Promise<ApiResult<AuthResponse>> {
  return apiFetch<AuthResponse>("/api/cms/auth/login", {
    method: "POST",
    body: JSON.stringify(request),
  });
}
