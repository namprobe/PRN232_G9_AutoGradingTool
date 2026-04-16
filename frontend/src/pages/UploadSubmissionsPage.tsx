import type { DragEvent } from "react";
import { FormEvent, useCallback, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { createSubmissionZip } from "../api/gradingApi";
import { DEMO_EXAM_SESSION_ID } from "../api/gradingMockData";
import { useApiMock } from "../config/env";

type Slot = "q1" | "q2";

export function UploadSubmissionsPage() {
  const { token } = useAuth();
  const mock = useApiMock();
  const [q1, setQ1] = useState<File | null>(null);
  const [q2, setQ2] = useState<File | null>(null);
  const [studentCode, setStudentCode] = useState("HE199999");
  const [studentName, setStudentName] = useState("");
  const [dragOver, setDragOver] = useState<Slot | null>(null);
  const [msg, setMsg] = useState<{ type: "ok" | "err" | "info"; text: string } | null>(null);
  const [sending, setSending] = useState(false);

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
    setSending(true);
    setMsg(null);
    const fd = new FormData();
    fd.append("examSessionId", DEMO_EXAM_SESSION_ID);
    fd.append("studentCode", code);
    if (studentName.trim()) fd.append("studentName", studentName.trim());
    fd.append("q1Zip", q1);
    fd.append("q2Zip", q2);

    const r = await createSubmissionZip(token, fd);
    setSending(false);
    if (!r.isSuccess || !r.data) {
      setMsg({ type: "err", text: r.message ?? "Upload thất bại" });
      return;
    }
    setMsg({
      type: "ok",
      text: mock
        ? `Mock: đã tạo submissionId = ${r.data}. (BE thật sẽ lưu file + stub chấm.)`
        : `Đã tạo bài nộp submissionId = ${r.data}. Xem chi tiết tại danh sách / Swagger.`,
    });
  }

  return (
    <div className="ag-stack ag-stack--lg">
      <div className="ag-steps" aria-hidden>
        <div className="ag-steps__item ag-steps__item--done">1. Chọn zip</div>
        <div className="ag-steps__item ag-steps__item--done">2. Gửi & enqueue</div>
        <div className="ag-steps__item">3. Xem điểm</div>
      </div>

      <form className="ag-stack ag-stack--md" onSubmit={onSubmit}>
        <div className="ag-field" style={{ maxWidth: 420 }}>
          <label className="ag-label" htmlFor="exam-session-ro">
            Ca thi (form field examSessionId)
          </label>
          <input
            id="exam-session-ro"
            className="ag-input"
            readOnly
            value={`PRN232-DEMO-PE (${DEMO_EXAM_SESSION_ID})`}
          />
        </div>
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
