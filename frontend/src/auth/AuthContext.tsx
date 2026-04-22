import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { useNavigate } from "react-router-dom";
import { login as loginApi } from "../api/auth";
import type { AuthResponse } from "../api/types";

const STORAGE_KEY = "agt_access_token";

type AuthState = {
  token: string | null;
  user: Pick<AuthResponse, "expiresAt" | "roles"> | null;
};

type AuthContextValue = AuthState & {
  login: (email: string, password: string) => Promise<{ ok: boolean; message: string }>;
  logout: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const navigate = useNavigate();
  const [token, setToken] = useState<string | null>(() => localStorage.getItem(STORAGE_KEY));
  const [user, setUser] = useState<Pick<AuthResponse, "expiresAt" | "roles"> | null>(null);

  const logout = useCallback(() => {
    localStorage.removeItem(STORAGE_KEY);
    setToken(null);
    setUser(null);
  }, []);

  useEffect(() => {
    const onSessionExpired = () => {
      logout();
      navigate("/login", { replace: true });
    };
    window.addEventListener("ag-http-401", onSessionExpired);
    return () => window.removeEventListener("ag-http-401", onSessionExpired);
  }, [logout, navigate]);

  const login = useCallback(async (email: string, password: string) => {
    const res = await loginApi({ email, password });
    if (!res.isSuccess || !res.data?.accessToken) {
      return { ok: false, message: res.message ?? "Đăng nhập thất bại" };
    }
    localStorage.setItem(STORAGE_KEY, res.data.accessToken);
    setToken(res.data.accessToken);
    setUser({ expiresAt: res.data.expiresAt, roles: res.data.roles });
    return { ok: true, message: res.message ?? "OK" };
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      token,
      user,
      login,
      logout,
    }),
    [token, user, login, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
