import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { listSemesters } from "../api/gradingApi";
import { cmsCreateSemester, cmsDeleteSemester, cmsUpdateSemester } from "../api/gradingCmsApi";
import type { SemesterListItem } from "../api/gradingTypes";

function toLocalInput(iso: string | null): string {
  if (!iso) return "";
  const d = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export function SemestersPage() {
  const { token } = useAuth();
  const [rows, setRows] = useState<SemesterListItem[]>([]);
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [msg, setMsg] = useState<string | null>(null);
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState({
    code: "",
    name: "",
    startLocal: "",
    endLocal: "",
  });

  const load = useCallback(async () => {
    setErr(null);
    const r = await listSemesters(token);
    if (!r.isSuccess || !r.data) {
      setRows([]);
      setErr(r.message ?? "Không tải được học kỳ");
    } else setRows(r.data);
  }, [token]);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoading(true);
      await load();
      if (!cancelled) setLoading(false);
    })();
    return () => {
      cancelled = true;
    };
  }, [load]);

  function bodyFromForm() {
    return {
      code: form.code.trim(),
      name: form.name.trim(),
      startDateUtc: form.startLocal ? new Date(form.startLocal).toISOString() : null,
      endDateUtc: form.endLocal ? new Date(form.endLocal).toISOString() : null,
    };
  }

  async function onCreate(e: React.FormEvent) {
    e.preventDefault();
    setMsg(null);
    const r = await cmsCreateSemester(token, bodyFromForm());
    if (!r.isSuccess) setMsg(r.message ?? "Lỗi");
    else {
      setMsg(r.message ?? "Đã tạo");
      setForm({ code: "", name: "", startLocal: "", endLocal: "" });
      await load();
    }
  }

  async function onUpdate(e: React.FormEvent) {
    e.preventDefault();
    if (!editId) return;
    setMsg(null);
    const r = await cmsUpdateSemester(token, editId, bodyFromForm());
    if (!r.isSuccess) setMsg(r.message ?? "Lỗi");
    else {
      setMsg(r.message ?? "Đã lưu");
      setEditId(null);
      await load();
    }
  }

  async function onDelete(id: string) {
    if (!confirm("Xóa học kỳ này?")) return;
    setMsg(null);
    const r = await cmsDeleteSemester(token, id);
    if (!r.isSuccess) setMsg(r.message ?? "Lỗi");
    else {
      setMsg(r.message ?? "Đã xóa");
      await load();
    }
  }

  function startEdit(s: SemesterListItem) {
    setEditId(s.id);
    setForm({
      code: s.code,
      name: s.name,
      startLocal: toLocalInput(s.startDateUtc),
      endLocal: toLocalInput(s.endDateUtc),
    });
    setMsg(null);
  }

  return (
    <div className="ag-stack ag-stack--lg">
      <div className="ag-toolbar">
        <div>
          <p className="ag-toolbar__lead">Quản lý học kỳ — tạo, sửa và xóa</p>
        </div>
        <div className="ag-toolbar__actions">
          <Link to="/exam-sessions" className="ag-btn ag-btn--secondary">
            Ca thi
          </Link>
        </div>
      </div>

      {msg ? (
        <div className="ag-alert ag-alert--info" role="status">
          {msg}
        </div>
      ) : null}

      <section className="ag-card ag-animate-in" style={{ padding: "1rem 1.25rem" }}>
        <h3 className="ag-card__title" style={{ marginTop: 0 }}>
          {editId ? "Sửa học kỳ" : "Tạo học kỳ"}
        </h3>
        <form className="ag-stack ag-stack--sm" onSubmit={editId ? onUpdate : onCreate}>
          <div className="ag-upload-grid">
            <div className="ag-field">
              <label className="ag-label" htmlFor="sem-code">
                Mã
              </label>
              <input
                id="sem-code"
                className="ag-input"
                value={form.code}
                onChange={(e) => setForm((f) => ({ ...f, code: e.target.value }))}
                required
              />
            </div>
            <div className="ag-field">
              <label className="ag-label" htmlFor="sem-name">
                Tên
              </label>
              <input
                id="sem-name"
                className="ag-input"
                value={form.name}
                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                required
              />
            </div>
          </div>
          <div className="ag-upload-grid">
            <div className="ag-field">
              <label className="ag-label" htmlFor="sem-start">
                Bắt đầu (local)
              </label>
              <input
                id="sem-start"
                type="datetime-local"
                className="ag-input"
                value={form.startLocal}
                onChange={(e) => setForm((f) => ({ ...f, startLocal: e.target.value }))}
              />
            </div>
            <div className="ag-field">
              <label className="ag-label" htmlFor="sem-end">
                Kết thúc (local)
              </label>
              <input
                id="sem-end"
                type="datetime-local"
                className="ag-input"
                value={form.endLocal}
                onChange={(e) => setForm((f) => ({ ...f, endLocal: e.target.value }))}
              />
            </div>
          </div>
          <div className="ag-upload-actions">
            {editId ? (
              <button type="button" className="ag-btn ag-btn--ghost" onClick={() => setEditId(null)}>
                Huỷ sửa
              </button>
            ) : null}
            <button type="submit" className="ag-btn ag-btn--primary">
              {editId ? "Lưu" : "Tạo mới"}
            </button>
          </div>
        </form>
      </section>

      {err ? (
        <div className="ag-alert ag-alert--err" role="alert">
          {err}
        </div>
      ) : null}

      <div className="ag-card ag-card--flush ag-animate-in">
        <div className="ag-table-wrap">
          <table className="ag-table">
            <thead>
              <tr>
                <th>Mã</th>
                <th>Tên</th>
                <th>Bắt đầu (UTC)</th>
                <th>Kết thúc (UTC)</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={5} className="ag-table__muted">
                    Đang tải…
                  </td>
                </tr>
              ) : rows.length === 0 ? (
                <tr>
                  <td colSpan={5} className="ag-table__muted">
                    Chưa có học kỳ
                  </td>
                </tr>
              ) : (
                rows.map((s) => (
                  <tr key={s.id}>
                    <td>
                      <span className="ag-table__strong">{s.code}</span>
                    </td>
                    <td className="ag-table__strong">{s.name}</td>
                    <td className="ag-table__muted">
                      {s.startDateUtc ? new Date(s.startDateUtc).toLocaleString("vi-VN") : "—"}
                    </td>
                    <td className="ag-table__muted">
                      {s.endDateUtc ? new Date(s.endDateUtc).toLocaleString("vi-VN") : "—"}
                    </td>
                    <td className="ag-table__actions">
                      <button type="button" className="ag-linkbtn" onClick={() => startEdit(s)}>
                        Sửa
                      </button>
                      <button type="button" className="ag-linkbtn" onClick={() => void onDelete(s.id)}>
                        Xóa
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
