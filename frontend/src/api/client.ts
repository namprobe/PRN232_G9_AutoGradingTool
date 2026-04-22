import type { ApiResult } from "./types";
import { parseResponseAsApiResult } from "./parseApiResult";
import { describeHttpStatus } from "../lib/httpStatusVi";

const baseUrl = (): string => {
  const u = import.meta.env.VITE_API_BASE_URL;
  // Build Docker: VITE_API_BASE_URL="" → gọi tương đối /api... (nginx proxy cùng compose)
  if (u === "") return "";
  if (u === undefined || u === null) {
    if (import.meta.env.PROD) return "";
    console.warn("VITE_API_BASE_URL chưa set — dùng http://localhost:5000");
    return "http://localhost:5000";
  }
  return String(u).replace(/\/$/, "");
};

function networkFailureResult<T>(e: unknown): ApiResult<T> {
  const hint = e instanceof Error ? e.message : "Không xác định";
  return {
    isSuccess: false,
    message: `Không kết nối được máy chủ (${hint}). [0 — ${describeHttpStatus(0)}]`,
    httpStatus: 0,
  };
}

/** fetch tới URL đầy đủ (vd. baseUrl + path), luôn trả ApiResult — dùng cho multipart. */
export async function fetchAsApiResult<T>(
  absoluteUrl: string,
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
  try {
    const res = await fetch(absoluteUrl, { ...rest, headers });
    return await parseResponseAsApiResult<T>(res, { authTokenSent: Boolean(token) });
  } catch (e) {
    return networkFailureResult<T>(e);
  }
}

export async function apiFetch<T>(
  path: string,
  options: RequestInit & { token?: string | null } = {}
): Promise<ApiResult<T>> {
  return fetchAsApiResult<T>(`${baseUrl()}${path}`, options);
}

export { baseUrl };
