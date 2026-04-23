/**
 * Formatting utilities — AutoGrading Tool
 *
 * Rule duy nhất:
 *   - Backend (PostgreSQL / .NET) luôn trả về UTC với Z suffix:
 *       "2026-05-26T08:00:00Z"  hoặc  "2026-05-26T08:00:00.0000000+00:00"
 *   - Mọi hiển thị đều chuyển sang múi giờ LOCAL của trình duyệt người dùng.
 *   - Khi người dùng nhập datetime-local, giá trị được parse là giờ LOCAL
 *     rồi convert sang UTC trước khi gửi lên BE.
 */

/**
 * Chuẩn hoá chuỗi date từ backend thành đối tượng Date (UTC-aware).
 * Xử lý các dạng:
 *   "2026-05-26T08:00:00Z"
 *   "2026-05-26T08:00:00.000Z"
 *   "2026-05-26T08:00:00.0000000+00:00"
 *   "2026-05-26T08:00:00"  (nếu BE gửi thiếu Z — xử lý như UTC)
 */
function normalizeUTCDate(date: string | Date): Date {
  if (date instanceof Date) return date;

  let s = String(date).trim();

  // Đã có timezone info → parse thẳng
  if (s.endsWith('Z') || /[+-]\d{2}:\d{2}$/.test(s)) {
    return new Date(s);
  }

  // Không có Z — BE gửi UTC nhưng thiếu suffix → thêm Z
  // Chuẩn hoá fractional seconds về 3 chữ số
  s = s.replace(/\.(\d+)$/, (_m, d) => `.${d.substring(0, 3).padEnd(3, '0')}`);
  return new Date(s + 'Z');
}

// ─── Hiển thị ────────────────────────────────────────────────────────────────

/** Định dạng đầy đủ ngày + giờ theo locale & timezone của trình duyệt.
 *  Ví dụ: "26/05/2026, 15:00"  (nếu user ở UTC+7 và datetime là 08:00 UTC)
 */
export function formatDateTime(date: string | Date | null | undefined): string {
  if (!date) return '—';
  try {
    const d = normalizeUTCDate(date);
    if (isNaN(d.getTime())) return '—';
    return d.toLocaleString(navigator.language || 'vi-VN', {
      timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone,
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    });
  } catch {
    return '—';
  }
}

/** Chỉ ngày, không giờ.  Ví dụ: "26/05/2026" */
export function formatDateOnly(date: string | Date | null | undefined): string {
  if (!date) return '—';
  try {
    const d = normalizeUTCDate(date);
    if (isNaN(d.getTime())) return '—';
    return d.toLocaleDateString(navigator.language || 'vi-VN', {
      timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone,
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
    });
  } catch {
    return '—';
  }
}

/** Chỉ giờ:phút.  Ví dụ: "15:00" */
export function formatTimeOnly(date: string | Date | null | undefined): string {
  if (!date) return '—';
  try {
    const d = normalizeUTCDate(date);
    if (isNaN(d.getTime())) return '—';
    return d.toLocaleTimeString(navigator.language || 'vi-VN', {
      timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone,
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    });
  } catch {
    return '—';
  }
}

// ─── Chuyển đổi cho <input type="datetime-local"> ────────────────────────────

/**
 * UTC ISO string → giá trị cho <input type="datetime-local"> (YYYY-MM-DDTHH:mm).
 * Trình duyệt hiển thị đúng theo local timezone.
 *
 * Ví dụ UTC+7:  "2026-05-26T08:00:00Z"  →  "2026-05-26T15:00"
 */
export function utcToLocalDateTimeInput(utcString: string | null | undefined): string {
  if (!utcString) return '';
  try {
    const d = normalizeUTCDate(utcString);
    if (isNaN(d.getTime())) return '';
    const pad = (n: number) => String(n).padStart(2, '0');
    // getFullYear/Month/Date/Hours/Minutes đều trả về theo LOCAL timezone
    return (
      `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}` +
      `T${pad(d.getHours())}:${pad(d.getMinutes())}`
    );
  } catch {
    return '';
  }
}

/**
 * Giá trị từ <input type="datetime-local"> (local time) → UTC ISO string.
 * Dùng khi chuẩn bị payload gửi lên BE.
 *
 * Ví dụ UTC+7:  "2026-05-26T15:00"  →  "2026-05-26T08:00:00.000Z"
 */
export function localDateTimeToUTC(localInput: string): string {
  if (!localInput) return '';
  // new Date("YYYY-MM-DDTHH:mm") được browser parse là LOCAL time
  return new Date(localInput).toISOString();
}
