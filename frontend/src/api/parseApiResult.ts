import { describeHttpStatus } from "../lib/httpStatusVi";
import type { ApiResult } from "./types";

export type ParseApiOptions = {
  /** true nếu request có gửi Bearer — dùng để bắt phiên hết hạn (401) */
  authTokenSent?: boolean;
};

function asStringList(v: unknown): string[] {
  if (!Array.isArray(v)) return [];
  return v.filter((x): x is string => typeof x === "string" && x.length > 0);
}

/**
 * Đọc Response từ fetch: luôn trả về ApiResult, kèm httpStatus và thông điệp có mã HTTP cho người dùng.
 * Không ném lỗi khi body không phải JSON (trừ lỗi đọc stream — xử lý ở caller).
 */
export async function parseResponseAsApiResult<T>(
  res: Response,
  opts: ParseApiOptions = {}
): Promise<ApiResult<T>> {
  const status = res.status;
  let rawText = "";
  try {
    rawText = await res.text();
  } catch {
    rawText = "";
  }

  let parsed: Partial<ApiResult<T>> & Record<string, unknown> = {};
  const trimmed = rawText.trim();

  if (trimmed) {
    try {
      parsed = JSON.parse(trimmed) as Partial<ApiResult<T>>;
    } catch {
      parsed = {
        isSuccess: false,
        message: trimmed.length > 800 ? `${trimmed.slice(0, 800)}…` : trimmed,
      };
    }
  }

  const bodyFlag = parsed.isSuccess;
  const bodyIsSuccess = bodyFlag !== false;
  const isSuccess = res.ok && bodyIsSuccess;

  const errList = asStringList(parsed.errors);
  const serverMsg = typeof parsed.message === "string" ? parsed.message.trim() : "";

  const detailParts: string[] = [];
  if (serverMsg) detailParts.push(serverMsg);
  if (errList.length) detailParts.push(errList.join("; "));

  let message = detailParts.join(" — ").trim();
  const statusLine = `${status} — ${describeHttpStatus(status)}`;

  if (!isSuccess) {
    if (!message) {
      message = !res.ok ? statusLine : "Thao tác không thành công.";
    } else if (!res.ok && !message.includes(String(status))) {
      message = `${message} [${statusLine}]`;
    }
  }

  const result: ApiResult<T> = {
    isSuccess,
    message: message || null,
    data: parsed.data as T | undefined,
    errors: errList.length ? errList : (parsed.errors as string[] | null) ?? null,
    errorCode: typeof parsed.errorCode === "string" ? parsed.errorCode : null,
    httpStatus: status,
  };

  if (status === 401 && opts.authTokenSent && typeof window !== "undefined") {
    window.dispatchEvent(new Event("ag-http-401"));
  }

  return result;
}
