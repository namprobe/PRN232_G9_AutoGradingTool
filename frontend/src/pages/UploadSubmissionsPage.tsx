import type { DragEvent } from "react";
import { FormEvent, useCallback, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { baseUrl } from "../api/client";

type Slot = "q1" | "q2";

export function UploadSubmissionsPage() {
  const { token } = useAuth();
  const [q1, setQ1] = useState<File | null>(null);
  const [q2, setQ2] = useState<File | null>(null);
  const [dragOver, setDragOver] = useState<Slot | null>(null);
  const [msg, setMsg] = useState<{ type: "ok" | "err" | "info"; text: string } | null>(null);

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

  function onSubmit(e: FormEvent) {
    e.preventDefault();
    if (!q1 || !q2) {
      setMsg({ type: "err", text: "Vui lòng chọn đủ hai file zip cho Q1 và Q2." });
      return;
    }
    const api = baseUrl() || window.location.origin;
    setMsg({
      type: "info",
      text: `UI sẵn sàng: ${q1.name} + ${q2.name}. Endpoint upload sẽ do P2/P3 cung cấp. Bearer: ${token ? "có" : "không"} · Base: ${api || "(cùng origin)"}`,
    });
  }

  return (
    <div className="ag-stack ag-stack--lg">
      <div className="ag-steps" aria-hidden>
        <div className="ag-steps__item ag-steps__item--done">1. Chọn zip</div>
        <div className="ag-steps__item">2. Gửi & enqueue</div>
        <div className="ag-steps__item">3. Xem điểm</div>
      </div>

      <form className="ag-stack ag-stack--md" onSubmit={onSubmit}>
        <div className="ag-upload-grid">
          <DropCard
            title="Question 1"
            subtitle="Solution zip (API / EF / Swagger…)"
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
            subtitle="MVC + HttpClient + Views"
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
          <button type="submit" className="ag-btn ag-btn--primary ag-btn--lg" disabled={!q1 || !q2}>
            Gửi bài (chờ API)
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
