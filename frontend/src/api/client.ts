import type { ApiResult } from "./types";

const baseUrl = (): string => {
  const u = import.meta.env.VITE_API_BASE_URL;
  if (!u) {
    console.warn("VITE_API_BASE_URL chưa set — dùng http://localhost:5000");
    return "http://localhost:5000";
  }
  return u.replace(/\/$/, "");
};

export async function apiFetch<T>(
  path: string,
  options: RequestInit & { token?: string | null } = {}
): Promise<ApiResult<T>> {
  const { token, headers: initHeaders, ...rest } = options;
  const headers = new Headers(initHeaders);
  if (!headers.has("Content-Type") && rest.body && typeof rest.body === "string") {
    headers.set("Content-Type", "application/json");
  }
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }
  const res = await fetch(`${baseUrl()}${path}`, { ...rest, headers });
  const json = (await res.json()) as ApiResult<T>;
  return json;
}

export { baseUrl };
