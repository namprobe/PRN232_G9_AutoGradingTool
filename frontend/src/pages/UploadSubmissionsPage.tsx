import { FormEvent, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { baseUrl } from "../api/client";

/**
 * UI sẵn cho upload Q1/Q2. Khi có API: thay phần alert bằng FormData + fetch có Bearer.
 */
export function UploadSubmissionsPage() {
  const { token } = useAuth();
  const [q1, setQ1] = useState<File | null>(null);
  const [q2, setQ2] = useState<File | null>(null);
  const [msg, setMsg] = useState<string | null>(null);

  function onSubmit(e: FormEvent) {
    e.preventDefault();
    if (!q1 || !q2) {
      setMsg("Chọn đủ 2 file zip (Q1 và Q2).");
      return;
    }
    setMsg(
      `Sẵn sàng gửi: ${q1.name}, ${q2.name}. Endpoint upload do Person 2 thêm — tạm thời chỉ kiểm tra UI. API base: ${baseUrl()} · có token: ${token ? "có" : "không"}`
    );
  }

  return (
    <div className="shell">
      <header className="topbar">
        <strong>Nộp bài thi</strong>
        <Link to="/">← Dashboard</Link>
      </header>
      <main className="main">
        <h2>Upload zip Q1 & Q2</h2>
        <form className="card" onSubmit={onSubmit}>
          <label>
            Zip Question 1
            <input
              type="file"
              accept=".zip,application/zip"
              onChange={(e) => setQ1(e.target.files?.[0] ?? null)}
            />
          </label>
          <label>
            Zip Question 2
            <input
              type="file"
              accept=".zip,application/zip"
              onChange={(e) => setQ2(e.target.files?.[0] ?? null)}
            />
          </label>
          <button type="submit">Gửi (stub)</button>
        </form>
        {msg ? <p className="muted">{msg}</p> : null}
      </main>
    </div>
  );
}
