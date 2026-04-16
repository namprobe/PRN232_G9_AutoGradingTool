/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string;
  /** `"true"` → login + CMS grading gọi mock local (không cần BE) */
  readonly VITE_USE_API_MOCK?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
