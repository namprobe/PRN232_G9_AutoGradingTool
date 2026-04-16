import { useApiMock } from "../config/env";
import { apiFetch } from "./client";
import type { ApiResult, AuthResponse, LoginRequest } from "./types";

export async function login(request: LoginRequest): Promise<ApiResult<AuthResponse>> {
  if (useApiMock()) {
    await new Promise((r) => setTimeout(r, 220));
    const exp = new Date(Date.now() + 86400000).toISOString();
    return {
      isSuccess: true,
      message: "Đăng nhập mock (VITE_USE_API_MOCK=true)",
      data: {
        accessToken: "mock-access-token",
        expiresAt: exp,
        roles: [1],
      },
    };
  }
  return apiFetch<AuthResponse>("/api/cms/auth/login", {
    method: "POST",
    body: JSON.stringify(request),
  });
}
