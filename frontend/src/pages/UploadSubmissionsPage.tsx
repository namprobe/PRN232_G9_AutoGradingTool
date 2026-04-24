import type { DragEvent } from "react";
import { FormEvent, useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { studentSubmitZip } from "../api/gradingCmsApi";
import { createSubmissionZip, getExamSession } from "../api/gradingApi";
import type { ExamTopicDetail } from "../api/gradingTypes";
import { useApiMock } from "../config/env";
import { WorkflowBreadcrumb, crumbsForUpload } from "../components/WorkflowBreadcrumb";
import { examSessionDetailPath, examSessionSubmissionsPath } from "../lib/workflowRoutes";

type Slot = "q1" | "q2";

export function UploadSubmissionsPage() {
  const { sessionId: routeSessionId } = useParams<{ sessionId: string }>();
  const { token } = useAuth();
  const mock = useApiMock();
  const [sessionCode, setSessionCode] = useState("");
  const [sessionTitle, setSessionTitle] = useState("");
  const [metaErr, setMetaErr] = useState<string | null>(null);
  const [examSessionId, setExamSessionId] = useState("");
  /** true = kênh thí sinh; false = kênh quản trị nội bộ */
  const [useStudentApi, setUseStudentApi] = useState(true);
  const [q1, setQ1] = useState<File | null>(null);
  const [q2, setQ2] = useState<File | null>(null);
  const [studentCode, setStudentCode] = useState("HE199999");
  const [studentName, setStudentName] = useState("");
  const [dragOver, setDragOver] = useState<Slot | null>(null);
  const [msg, setMsg] = useState<{ type: "ok" | "err" | "info"; text: string } | null>(null);
  const [lastOk, setLastOk] = useState<{ submissionId: string; sessionId: string } | null>(null);
  const [sending, setSending] = useState(false);
  const [sessionTopics, setSessionTopics] = useState<ExamTopicDetail[]>([]);
  const [examTopicId, setExamTopicId] = useState("");

  useEffect(() => {
    if (!routeSessionId) {
      setMetaErr("Thiếu mã ca thi trong URL — mở từ trang chi tiết ca.");
      return;
    }
    setExamSessionId(routeSessionId);
    setMetaErr(null);
    let cancelled = false;
    (async () => {
      const meta = await getExamSession(token, routeSessionId);
      if (cancelled) return;
      if (!meta.isSuccess || !meta.data) {
        setMetaErr(meta.message ?? "Không tải được ca thi.");
        setSessionCode("");
        setSessionTitle("");
        setSessionTopics([]);
        setExamTopicId("");
        return;
      }
      setSessionCode(meta.data.code);
      setSessionTitle(meta.data.title);
      const topics = meta.data.topics ?? [];
      setSessionTopics(topics);
      setExamTopicId(topics[0]?.id ?? "");
    })();
    return () => {
      cancelled = true;
    };
  }, [token, routeSessionId]);

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
      setMsg({ type: "err", text: "Chỉ dùng được tệp .zip." });
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
      setMsg({ type: "err", text: "Vui lòng nhập mã số sinh viên." });
      return;
    }
    if (!examSessionId) {
      setMsg({ type: "err", text: "Thiếu thông tin ca thi — hãy mở lại trang từ danh sách ca." });
      return;
    }
    if (!examTopicId.trim()) {
      setMsg({ type: "err", text: "Vui lòng chọn topic để nộp bài." });
      return;
    }
    setSending(true);
    setMsg(null);
    setLastOk(null);
    const fd = new FormData();
    fd.append("examSessionId", examSessionId);
    fd.append("entries[0].examTopicId", examTopicId.trim());
    fd.append("entries[0].studentCode", code);
    if (studentName.trim()) fd.append("entries[0].studentName", studentName.trim());
    fd.append("entries[0].q1Zip", q1);
    fd.append("entries[0].q2Zip", q2);

    const r = useStudentApi ? await studentSubmitZip(token, fd) : await createSubmissionZip(token, fd);
    setSending(false);
    if (!r.isSuccess || !r.data) {
      setMsg({ type: "err", text: r.message ?? "Upload thất bại" });
      return;
    }
    const via = useStudentApi ? "kênh thí sinh" : "kênh quản trị";
    setLastOk({ submissionId: r.data, sessionId: examSessionId });
    setMsg({
      type: "ok",
      text: mock
        ? `Chế độ thử (${via}): đã tạo bài nộp. Mã tham chiếu: ${r.data}.`
        : `Đã gửi bài qua ${via}. Mã tham chiếu: ${r.data}.`,
    });
  }

  if (!routeSessionId) {
    return (
      <div className="ag-empty ag-animate-in">
        <p className="ag-empty__text">Đường dẫn không hợp lệ.</p>
        <Link to="/exam-sessions" className="ag-btn ag-btn--primary">
          Chọn ca thi
        </Link>
      </div>
    );
  }

  if (metaErr || !sessionCode) {
    return (
      <div className="ag-empty ag-animate-in">
        <p className="ag-empty__text">{metaErr ?? "Đang tải thông tin ca…"}</p>
        <Link to="/exam-sessions" className="ag-btn ag-btn--secondary">
          Danh sách ca thi
        </Link>
      </div>
    );
  }

  return (
    <div className="ag-stack ag-stack--lg">
      <WorkflowBreadcrumb items={crumbsForUpload(sessionCode, routeSessionId)} />
      <div className="ag-steps" aria-hidden>
        <div className={"ag-steps__item" + (q1 && q2 ? " ag-steps__item--done" : "")}>1. Chọn zip</div>
        <div className={"ag-steps__item" + (lastOk || sending ? " ag-steps__item--done" : "")}>2. Gửi & chấm stub</div>
        <div className={"ag-steps__item" + (lastOk ? " ag-steps__item--done" : "")}>3. Xem điểm</div>
      </div>

      <form className="ag-stack ag-stack--md" onSubmit={onSubmit}>
        <div className="ag-card" style={{ padding: "0.75rem 1rem", maxWidth: 640 }}>
          <div className="ag-card__head" style={{ marginBottom: 6 }}>
            <h3 className="ag-card__title" style={{ margin: 0 }}>
              Ca thi đang nộp
            </h3>
          </div>
          <p style={{ margin: 0 }}>
            <span className="ag-table__strong">{sessionCode}</span> {sessionTitle}
          </p>
          <p className="ag-table__muted" style={{ margin: "0.5rem 0 0", fontSize: "0.85rem" }}>
            Đổi ca: quay lại danh sách hoặc dùng menu «Đổi nhanh» ở trang bài nộp.
          </p>
        </div>

        <fieldset className="ag-field" style={{ border: "none", padding: 0, margin: 0 }}>
          <legend className="ag-label" style={{ marginBottom: 8 }}>
            Gửi bài qua
          </legend>
          <div className="ag-stack ag-stack--sm" style={{ flexDirection: "row", flexWrap: "wrap", gap: 16 }}>
            <label className="ag-table__muted" style={{ display: "inline-flex", alignItems: "center", gap: 8 }}>
              <input
                type="radio"
                name="submit-channel"
                checked={useStudentApi}
                onChange={() => setUseStudentApi(true)}
              />
              Cổng dành cho thí sinh (như thi thật)
            </label>
            <label className="ag-table__muted" style={{ display: "inline-flex", alignItems: "center", gap: 8 }}>
              <input
                type="radio"
                name="submit-channel"
                checked={!useStudentApi}
                onChange={() => setUseStudentApi(false)}
              />
              Cổng quản trị (nội bộ giảng viên)
            </label>
          </div>
        </fieldset>
        {sessionTopics.length > 0 ? (
          <div className="ag-field" style={{ maxWidth: 640 }}>
            <label className="ag-label" htmlFor="exam-topic">
              Topic nộp bài <span className="ag-table__muted">(bắt buộc)</span>
            </label>
            <select id="exam-topic" className="ag-input" value={examTopicId} onChange={(e) => setExamTopicId(e.target.value)}>
              {sessionTopics.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.title}
                </option>
              ))}
            </select>
          </div>
        ) : (
          <div className="ag-alert ag-alert--err" role="alert">
            Ca thi chưa có topic nên chưa thể nộp batch.
          </div>
        )}

        <div className="ag-upload-grid" style={{ marginTop: 8 }}>
          <div className="ag-field">
            <label className="ag-label" htmlFor="student-code">
              Mã số sinh viên
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
              Họ và tên <span className="ag-table__muted">(không bắt buộc)</span>
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
            title="Câu 1"
            subtitle="Một tệp .zip cho câu 1"
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
            title="Câu 2"
            subtitle="Một tệp .zip cho câu 2"
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
                <Link className="ag-btn ag-btn--secondary" to={examSessionSubmissionsPath(lastOk.sessionId)}>
                  Danh sách bài nộp ca này
                </Link>
              </div>
            ) : null}
          </div>
        ) : null}

        <div className="ag-upload-actions">
          <Link to={examSessionDetailPath(routeSessionId)} className="ag-btn ag-btn--ghost">
            Chi tiết ca
          </Link>
          <Link to={examSessionSubmissionsPath(routeSessionId)} className="ag-btn ag-btn--ghost">
            Bài nộp
          </Link>
          <button type="submit" className="ag-btn ag-btn--primary ag-btn--lg" disabled={!q1 || !q2 || !examTopicId || sending}>
            {sending ? "Đang gửi…" : "Gửi bài"}
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
