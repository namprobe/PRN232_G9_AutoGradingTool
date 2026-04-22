import type { DragEvent } from "react";
import { FormEvent, useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { studentSubmitZip } from "../api/gradingCmsApi";
import { createSubmissionZip, listExamSessions } from "../api/gradingApi";
import { DEMO_EXAM_SESSION_ID } from "../api/gradingMockData";
import type { ExamSessionListItem } from "../api/gradingTypes";
import { useApiMock } from "../config/env";

type Slot = "q1" | "q2";

export function UploadSubmissionsPage() {
  const { token } = useAuth();
  const mock = useApiMock();
  const [sessions, setSessions] = useState<ExamSessionListItem[]>([]);
  const [sessionsErr, setSessionsErr] = useState<string | null>(null);
  const [examSessionId, setExamSessionId] = useState(DEMO_EXAM_SESSION_ID);
  /** true = POST /api/student/grading/submissions (giả lập SV); false = CMS submissions */
  const [useStudentApi, setUseStudentApi] = useState(true);
  const [q1, setQ1] = useState<File | null>(null);
  const [q2, setQ2] = useState<File | null>(null);
  const [studentCode, setStudentCode] = useState("HE199999");
  const [studentName, setStudentName] = useState("");
  const [dragOver, setDragOver] = useState<Slot | null>(null);
  const [msg, setMsg] = useState<{ type: "ok" | "err" | "info"; text: string } | null>(null);
  const [lastOk, setLastOk] = useState<{ submissionId: string; sessionId: string } | null>(null);
  const [sending, setSending] = useState(false);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      const r = await listExamSessions(token, null);
      if (cancelled) return;
      if (!r.isSuccess || !r.data) {
        setSessionsErr(r.message ?? "Không tải được danh sách ca thi.");
        return;
      }
      setSessionsErr(null);
      setSessions(r.data);
      setExamSessionId((cur) => {
        if (r.data.length === 0) return "";
        if (r.data.some((s) => s.id === cur)) return cur;
        return r.data[0]!.id;
      });
    })();
    return () => {
      cancelled = true;
    };
  }, [token]);

  const setFile = (slot: Slot, file: File | null) => {
    if (slot === "q1") setQ1(file);
    else setQ2(file);
  };

  const onDrop = useCallback((slot: Slot, e: DragEvent) => {
    e.preventDefault();
    setDragOver(null);
    const f = e.dataTransfer.files[0];
    if (!f) return;
    if (!f.name.toLowerCase().endsWith(".zip")) {
      setMsg({ type: "err", text: "Chỉ chấp nhận file .zip" });
      return;
    }
    setFile(slot, f);
    setMsg(null);
  }, []);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    if (!q1 || !q2) {
      setMsg({ type: "err", text: "Vui lòng chọn đủ hai file zip cho Q1 và Q2." });
      return;
    }
    const code = studentCode.trim();
    if (!code) {
      setMsg({ type: "err", text: "MSSV (studentCode) bắt buộc." });
      return;
    }
    if (!examSessionId) {
      setMsg({ type: "err", text: "Chọn ca thi (examSessionId)." });
      return;
    }
    setSending(true);
    setMsg(null);
    setLastOk(null);
    const fd = new FormData();
    fd.append("examSessionId", examSessionId);
    fd.append("studentCode", code);
    if (studentName.trim()) fd.append("studentName", studentName.trim());
    fd.append("q1Zip", q1);
    fd.append("q2Zip", q2);

    const r = useStudentApi ? await studentSubmitZip(token, fd) : await createSubmissionZip(token, fd);
    setSending(false);
    if (!r.isSuccess || !r.data) {
      setMsg({ type: "err", text: r.message ?? "Upload thất bại" });
      return;
    }
    const via = useStudentApi ? "API học sinh" : "CMS";
    setLastOk({ submissionId: r.data, sessionId: examSessionId });
    setMsg({
      type: "ok",
      text: mock
        ? `Mock (${via}): submissionId = ${r.data}.`
        : `Đã tạo bài nộp (${via}) submissionId = ${r.data}.`,
    });
  }

  return (
    <div className="ag-stack ag-stack--lg">
      <div className="ag-steps" aria-hidden>
        <div className={"ag-steps__item" + (q1 && q2 ? " ag-steps__item--done" : "")}>1. Chọn zip</div>
        <div className={"ag-steps__item" + (lastOk || sending ? " ag-steps__item--done" : "")}>2. Gửi & chấm stub</div>
        <div className={"ag-steps__item" + (lastOk ? " ag-steps__item--done" : "")}>3. Xem điểm</div>
      </div>

      <form className="ag-stack ag-stack--md" onSubmit={onSubmit}>
        <div className="ag-field" style={{ maxWidth: 520 }}>
          <label className="ag-label" htmlFor="exam-session-select">
            Ca thi <span className="ag-table__muted">(examSessionId)</span>
          </label>
          <select
            id="exam-session-select"
            className="ag-input"
            value={examSessionId}
            onChange={(e) => setExamSessionId(e.target.value)}
            disabled={sessions.length === 0}
          >
            {sessions.length === 0 ? (
              <option value="">—</option>
            ) : (
              sessions.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.code} — {s.title}
                </option>
              ))
            )}
          </select>
          {sessionsErr ? <p className="ag-table__muted" style={{ marginTop: 6 }}>{sessionsErr}</p> : null}
        </div>

        <fieldset className="ag-field" style={{ border: "none", padding: 0, margin: 0 }}>
          <legend className="ag-label" style={{ marginBottom: 8 }}>
            Kênh gửi
          </legend>
          <div className="ag-stack ag-stack--sm" style={{ flexDirection: "row", flexWrap: "wrap", gap: 16 }}>
            <label className="ag-table__muted" style={{ display: "inline-flex", alignItems: "center", gap: 8 }}>
              <input
                type="radio"
                name="submit-channel"
                checked={useStudentApi}
                onChange={() => setUseStudentApi(true)}
              />
              Học sinh — <code className="ag-code ag-code--sm">POST /api/student/grading/submissions</code>
            </label>
            <label className="ag-table__muted" style={{ display: "inline-flex", alignItems: "center", gap: 8 }}>
              <input
                type="radio"
                name="submit-channel"
                checked={!useStudentApi}
                onChange={() => setUseStudentApi(false)}
              />
              CMS — <code className="ag-code ag-code--sm">POST /api/cms/grading/submissions</code>
            </label>
          </div>
        </fieldset>
        <div className="ag-upload-grid" style={{ marginTop: 8 }}>
          <div className="ag-field">
            <label className="ag-label" htmlFor="student-code">
              MSSV <span className="ag-table__muted">(studentCode)</span>
            </label>
            <input
              id="student-code"
              className="ag-input"
              value={studentCode}
              onChange={(e) => setStudentCode(e.target.value)}
              autoComplete="off"
            />
          </div>
          <div className="ag-field">
            <label className="ag-label" htmlFor="student-name">
              Họ tên <span className="ag-table__muted">(tuỳ chọn)</span>
            </label>
            <input
              id="student-name"
              className="ag-input"
              value={studentName}
              onChange={(e) => setStudentName(e.target.value)}
              autoComplete="name"
            />
          </div>
        </div>

        <div className="ag-upload-grid">
          <DropCard
            title="Question 1"
            subtitle="q1Zip — file .zip"
            file={q1}
            dragOver={dragOver === "q1"}
            onDragOver={(e) => {
              e.preventDefault();
              setDragOver("q1");
            }}
            onDragLeave={() => setDragOver(null)}
            onDrop={(e) => onDrop("q1", e)}
            onFileInput={(f) => {
              setFile("q1", f);
              setMsg(null);
            }}
            onClear={() => setFile("q1", null)}
          />
          <DropCard
            title="Question 2"
            subtitle="q2Zip — file .zip"
            file={q2}
            dragOver={dragOver === "q2"}
            onDragOver={(e) => {
              e.preventDefault();
              setDragOver("q2");
            }}
            onDragLeave={() => setDragOver(null)}
            onDrop={(e) => onDrop("q2", e)}
            onFileInput={(f) => {
              setFile("q2", f);
              setMsg(null);
            }}
            onClear={() => setFile("q2", null)}
          />
        </div>

        {msg ? (
          <div
            className={
              "ag-alert " +
              (msg.type === "err" ? "ag-alert--err" : msg.type === "ok" ? "ag-alert--ok" : "ag-alert--info")
            }
            role="status"
          >
            {msg.text}
            {lastOk && msg.type === "ok" ? (
              <div className="ag-stack ag-stack--sm" style={{ marginTop: 12 }}>
                <Link
                  className="ag-btn ag-btn--primary"
                  to={`/submissions/${lastOk.submissionId}`}
                  state={{ fromSessionId: lastOk.sessionId }}
                >
                  Xem chi tiết bài nộp
                </Link>
                <Link
                  className="ag-btn ag-btn--secondary"
                  to={`/submissions?examSessionId=${encodeURIComponent(lastOk.sessionId)}`}
                >
                  Danh sách bài nộp ca này
                </Link>
              </div>
            ) : null}
          </div>
        ) : null}

        <div className="ag-upload-actions">
          <Link to="/submissions" className="ag-btn ag-btn--ghost">
            Quay lại
          </Link>
          <button type="submit" className="ag-btn ag-btn--primary ag-btn--lg" disabled={!q1 || !q2 || sending}>
            {sending ? "Đang gửi…" : "Gửi bài (multipart)"}
          </button>
        </div>
      </form>
    </div>
  );
}

type DropProps = {
  title: string;
  subtitle: string;
  file: File | null;
  dragOver: boolean;
  onDragOver: (e: DragEvent) => void;
  onDragLeave: () => void;
  onDrop: (e: DragEvent) => void;
  onFileInput: (f: File | null) => void;
  onClear: () => void;
};

function DropCard({ title, subtitle, file, dragOver, onDragOver, onDragLeave, onDrop, onFileInput, onClear }: DropProps) {
  const id = `zip-${title.replace(/\s+/g, "-").toLowerCase()}`;

  return (
    <label
      className={
        "ag-dropzone" + (dragOver ? " ag-dropzone--active" : "") + (file ? " ag-dropzone--filled" : "")
      }
      onDragOver={onDragOver}
      onDragLeave={onDragLeave}
      onDrop={onDrop}
    >
      <input
        id={id}
        type="file"
        accept=".zip,application/zip"
        className="ag-dropzone__input"
        onChange={(e) => onFileInput(e.target.files?.[0] ?? null)}
      />
      <div className="ag-dropzone__icon" aria-hidden>
        <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.4">
          <path d="M12 11V3m0 0L8 7m4-4l4 4" />
          <path d="M3 15v3a2 2 0 002 2h14a2 2 0 002-2v-3" />
        </svg>
      </div>
      <div className="ag-dropzone__title">{title}</div>
      <p className="ag-dropzone__sub">{subtitle}</p>
      {file ? (
        <div className="ag-filechip">
          <span className="ag-filechip__name">{file.name}</span>
          <span className="ag-filechip__size">{(file.size / 1024 / 1024).toFixed(2)} MB</span>
          <button
            type="button"
            className="ag-filechip__x"
            onClick={(e) => {
              e.preventDefault();
              e.stopPropagation();
              onClear();
            }}
            aria-label="Bỏ file"
          >
            ×
          </button>
        </div>
      ) : (
        <span className="ag-dropzone__cta">Kéo thả zip vào đây hoặc bấm để chọn</span>
      )}
    </label>
  );
}
