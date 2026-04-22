/** Khớp `Result<T>` backend */
export type ApiResult<T> = {
  isSuccess: boolean;
  message?: string | null;
  data?: T;
  errors?: string[] | null;
  errorCode?: string | null;
  /** Mã HTTP thực tế (0 = lỗi mạng / không có phản hồi) */
  httpStatus?: number;
};

export type AuthResponse = {
  accessToken: string;
  expiresAt: string;
  roles: number[];
};

export type LoginRequest = {
  email: string;
  password: string;
};
