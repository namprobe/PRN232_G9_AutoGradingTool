import { Link, useParams } from "react-router-dom";
import { useCallback, useEffect, useState } from "react";
import { useAuth } from "../auth/AuthContext";
import { getExamSession } from "../api/gradingApi";
import {
  cmsCreateGradingPack,
  cmsCreateQuestion,
  cmsCreateTestCase,
  cmsCreateTopic,
  cmsListGradingPacks,
  cmsUploadPackAsset,
} from "../api/gradingCmsApi";
import type { ApiResult } from "../api/types";
import type { ExamGradingPackListItem, ExamSessionDetail } from "../api/gradingTypes";
import { SessionStatusBadge } from "../components/StatusBadge";
import { inferSessionStatus } from "../lib/gradingUi";
import { examSessionSubmissionsPath, examSessionUploadPath } from "../lib/workflowRoutes";
import { formatDateTime } from "../lib/format";

export function ExamSessionDetailPage() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const { token } = useAuth();
  const [detail, setDetail] = useState<ExamSessionDetail | null>(null);
  const [packs, setPacks] = useState<ExamGradingPackListItem[]>([]);
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [cmsMsg, setCmsMsg] = useState<string | null>(null);

  const [topicF, setTopicF] = useState({ title: "", sortOrder: 1 });
  const [qF, setQF] = useState({ topicId: "", label: "", title: "", maxScore: 5 });
  const [tcF, setTcF] = useState({ questionId: "", name: "", maxPoints: 2.5, sortOrder: 1 });
  const [packF, setPackF] = useState({ label: "", version: "" as string, isActive: true });
  const [assetF, setAssetF] = useState({ packId: "", kind: 2, file: null as File | null });

  const load = useCallback(async () => {
    if (!sessionId) return;
    setErr(null);
    const r = await getExamSession(token, sessionId);
    if (!r.isSuccess || !r.data) {
      setDetail(null);
      setErr(r.message ?? "Không tải được ca thi");
    } else {
      setDetail(r.data);
      setQF((f) => ({ ...f, topicId: f.topicId || r.data!.topics[0]?.id || "" }));
      setTcF((f) => ({ ...f, questionId: f.questionId || r.data!.topics[0]?.questions[0]?.id || "" }));
    }
    const pr = await cmsListGradingPacks(token, sessionId);
    if (pr.isSuccess && pr.data) {
      setPacks(pr.data);
      setAssetF((f) => ({ ...f, packId: f.packId || pr.data![0]?.id || "" }));
    } else setPacks([]);
  }, [token, sessionId]);

  useEffect(() => {
    if (!sessionId) {
      setLoading(false);
      setErr("Thiếu mã ca thi");
      return;
    }
    let cancelled = false;
    (async () => {
      setLoading(true);
      await load();
      if (!cancelled) setLoading(false);
    })();
    return () => {
      cancelled = true;
    };
  }, [load, sessionId]);

  async function runCms(fn: () => Promise<ApiResult<unknown>>) {
    setCmsMsg(null);
    const r = await fn();
    setCmsMsg(r.message ?? (r.isSuccess ? "OK" : "Lỗi"));
    if (r.isSuccess) await load();
  }

  if (loading) {
    return (
      <div className="ag-empty ag-animate-in">
        <p className="ag-table__muted">Đang tải cấu trúc đề…</p>
      </div>
    );
  }

  if (err || !detail) {
    return (
      <div className="ag-empty ag-animate-in">
        <h2 className="ag-empty__title">Không tìm thấy ca thi</h2>
        <p className="ag-empty__text">{err ?? "Không có dữ liệu"}</p>
        <Link to="/exam-sessions" className="ag-btn ag-btn--primary">
          Quay lại danh sách
        </Link>
      </div>
    );
  }

  return (
    <div className="ag-stack ag-stack--lg">
      <div className="ag-detail-head ag-animate-in">
        <div>
          <Link to="/exam-sessions" className="ag-backlink">
            ← Danh sách ca thi
          </Link>
          <h2 className="ag-detail-head__title">
            <span className="ag-table__strong">{detail.code}</span> {detail.title}
          </h2>
          <p className="ag-detail-head__meta">
            Học kỳ <span className="ag-table__strong">{detail.semesterCode}</span> · Bắt đầu{" "}
            {formatDateTime(detail.startsAtUtc)} · Thời lượng{" "}
            <strong>{detail.examDurationMinutes}</strong> phút · Đóng nộp{" "}
            {formatDateTime(detail.endsAtUtc)} ·{" "}
            <SessionStatusBadge status={inferSessionStatus(detail.startsAtUtc, detail.endsAtUtc)} />
          </p>
        </div>
        <div className="ag-detail-head__side" style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
          <Link to={examSessionUploadPath(detail.id)} className="ag-btn ag-btn--secondary">
            Nộp ZIP
          </Link>
          <Link to={examSessionSubmissionsPath(detail.id)} className="ag-btn ag-btn--primary">
            Bài nộp ca này
          </Link>
        </div>
      </div>

      {cmsMsg ? (
        <div className="ag-alert ag-alert--info" role="status">
          {cmsMsg}
        </div>
      ) : null}

      <section className="ag-card ag-animate-in">
        <div className="ag-card__head">
          <h3 className="ag-card__title">Chủ đề &amp; câu hỏi</h3>
          <p className="ag-card__desc">Xem cấu trúc đề; thêm mục mới ở các mục bên dưới rồi tải lại trang</p>
        </div>
        <ul className="ag-stack ag-stack--sm" style={{ listStyle: "none", padding: 0, margin: 0 }}>
          {detail.topics.map((t) => (
            <li key={t.id} className="ag-card" style={{ padding: "0.75rem 1rem" }}>
              <strong>{t.title}</strong>
              <span className="ag-table__muted"> · thứ tự {t.sortOrder}</span>
              <span className="ag-table__muted" style={{ marginLeft: 8, fontSize: "0.82rem" }}>
                · Mã tham chiếu: {t.id}
              </span>
              <ul className="ag-stack ag-stack--sm" style={{ marginTop: "0.5rem", paddingLeft: "1.1rem" }}>
                {t.questions.map((q) => (
                  <li key={q.id}>
                    <span className="ag-qtag">{q.label}</span> {q.title}{" "}
                    <span className="ag-table__muted">(tối đa {q.maxScore} điểm)</span>
                    <span className="ag-table__muted" style={{ marginLeft: 6, fontSize: "0.82rem" }}>
                      · Mã tham chiếu: {q.id}
                    </span>
                    <ul style={{ marginTop: "0.25rem", paddingLeft: "1rem", fontSize: "0.88rem" }}>
                      {q.testCases.map((tc) => (
                        <li key={tc.id}>
                          {tc.name} — {tc.maxPoints} điểm{" "}
                          <span className="ag-table__muted" style={{ marginLeft: 4, fontSize: "0.82rem" }}>
                            · Mã tham chiếu: {tc.id}
                          </span>
                        </li>
                      ))}
                    </ul>
                  </li>
                ))}
              </ul>
            </li>
          ))}
        </ul>
      </section>

      <section className="ag-card ag-animate-in" style={{ padding: "1rem 1.25rem" }}>
        <h3 className="ag-card__title">Thêm chủ đề</h3>
        <form
          className="ag-stack ag-stack--sm"
          onSubmit={(e) => {
            e.preventDefault();
            void runCms(() => cmsCreateTopic(token, detail.id, topicF));
          }}
        >
          <div className="ag-upload-grid">
            <div className="ag-field">
              <label className="ag-label">Tiêu đề</label>
              <input className="ag-input" value={topicF.title} onChange={(e) => setTopicF((f) => ({ ...f, title: e.target.value }))} />
            </div>
            <div className="ag-field">
              <label className="ag-label">Thứ tự hiển thị</label>
              <input
                type="number"
                className="ag-input"
                value={topicF.sortOrder}
                onChange={(e) => setTopicF((f) => ({ ...f, sortOrder: Number(e.target.value) }))}
              />
            </div>
          </div>
          <button type="submit" className="ag-btn ag-btn--secondary">
            Lưu chủ đề mới
          </button>
        </form>
      </section>

      <section className="ag-card ag-animate-in" style={{ padding: "1rem 1.25rem" }}>
        <h3 className="ag-card__title">Thêm câu hỏi</h3>
        <form
          className="ag-stack ag-stack--sm"
          onSubmit={(e) => {
            e.preventDefault();
            if (!qF.topicId) {
              setCmsMsg("Chọn chủ đề.");
              return;
            }
            void runCms(() =>
              cmsCreateQuestion(token, qF.topicId, {
                label: qF.label,
                title: qF.title,
                maxScore: qF.maxScore,
              })
            );
          }}
        >
          <div className="ag-field">
            <label className="ag-label">Chủ đề</label>
            <select className="ag-input" value={qF.topicId} onChange={(e) => setQF((f) => ({ ...f, topicId: e.target.value }))}>
              <option value="">—</option>
              {detail.topics.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.title}
                </option>
              ))}
            </select>
          </div>
          <div className="ag-upload-grid">
            <div className="ag-field">
              <label className="ag-label">Mã câu trên đề (ví dụ Q1)</label>
              <input className="ag-input" value={qF.label} onChange={(e) => setQF((f) => ({ ...f, label: e.target.value }))} />
            </div>
            <div className="ag-field">
              <label className="ag-label">Tiêu đề</label>
              <input className="ag-input" value={qF.title} onChange={(e) => setQF((f) => ({ ...f, title: e.target.value }))} />
            </div>
            <div className="ag-field">
              <label className="ag-label">Điểm tối đa</label>
              <input
                type="number"
                className="ag-input"
                value={qF.maxScore}
                onChange={(e) => setQF((f) => ({ ...f, maxScore: Number(e.target.value) }))}
              />
            </div>
          </div>
          <button type="submit" className="ag-btn ag-btn--secondary">
            Lưu câu hỏi mới
          </button>
        </form>
      </section>

      <section className="ag-card ag-animate-in" style={{ padding: "1rem 1.25rem" }}>
        <h3 className="ag-card__title">Thêm bài kiểm tra (testcase)</h3>
        <form
          className="ag-stack ag-stack--sm"
          onSubmit={(e) => {
            e.preventDefault();
            if (!tcF.questionId) {
              setCmsMsg("Chọn câu hỏi.");
              return;
            }
            void runCms(() =>
              cmsCreateTestCase(token, tcF.questionId, {
                name: tcF.name,
                maxPoints: tcF.maxPoints,
                sortOrder: tcF.sortOrder,
              })
            );
          }}
        >
          <div className="ag-field">
            <label className="ag-label">Câu hỏi</label>
            <select
              className="ag-input"
              value={tcF.questionId}
              onChange={(e) => setTcF((f) => ({ ...f, questionId: e.target.value }))}
            >
              <option value="">—</option>
              {detail.topics.flatMap((t) => t.questions.map((q) => ({ t, q }))).map(({ t, q }) => (
                <option key={q.id} value={q.id}>
                  {t.title} / {q.label}
                </option>
              ))}
            </select>
          </div>
          <div className="ag-upload-grid">
            <div className="ag-field">
              <label className="ag-label">Tên bài kiểm tra</label>
              <input className="ag-input" value={tcF.name} onChange={(e) => setTcF((f) => ({ ...f, name: e.target.value }))} />
            </div>
            <div className="ag-field">
              <label className="ag-label">Điểm tối đa</label>
              <input
                type="number"
                step="0.01"
                className="ag-input"
                value={tcF.maxPoints}
                onChange={(e) => setTcF((f) => ({ ...f, maxPoints: Number(e.target.value) }))}
              />
            </div>
            <div className="ag-field">
              <label className="ag-label">Thứ tự hiển thị</label>
              <input
                type="number"
                className="ag-input"
                value={tcF.sortOrder}
                onChange={(e) => setTcF((f) => ({ ...f, sortOrder: Number(e.target.value) }))}
              />
            </div>
          </div>
          <button type="submit" className="ag-btn ag-btn--secondary">
            Lưu testcase mới
          </button>
        </form>
      </section>

      <section className="ag-card ag-animate-in" style={{ padding: "1rem 1.25rem" }}>
        <div className="ag-card__head">
          <h3 className="ag-card__title">Gói chấm điểm</h3>
          <p className="ag-card__desc">
            Tạo phiên bản pack, bật dùng cho ca, rồi tải script hoặc tài liệu đính kèm. Loại tệp (số): 0 — khác, 1 — tài
            liệu, 2 — Postman, 3 — môi trường Newman.
          </p>
        </div>
        <ul className="ag-table__muted" style={{ marginBottom: 12 }}>
          {packs.map((p) => (
            <li key={p.id}>
              Phiên bản {p.version} · {p.label} · đang bật: {p.isActive ? "có" : "không"} · {p.assetCount} tệp đính kèm ·
              mã tham chiếu {p.id}
            </li>
          ))}
        </ul>
        <form
          className="ag-stack ag-stack--sm"
          onSubmit={(e) => {
            e.preventDefault();
            const v = packF.version.trim() ? Number(packF.version) : null;
            return runCms(() =>
              cmsCreateGradingPack(token, detail.id, {
                label: packF.label,
                version: v && v > 0 ? v : null,
                isActive: packF.isActive,
              })
            );
          }}
        >
          <div className="ag-upload-grid">
            <div className="ag-field">
              <label className="ag-label">Tên gói</label>
              <input className="ag-input" value={packF.label} onChange={(e) => setPackF((f) => ({ ...f, label: e.target.value }))} />
            </div>
            <div className="ag-field">
              <label className="ag-label">Phiên bản (để trống để hệ thống tự đặt)</label>
              <input className="ag-input" value={packF.version} onChange={(e) => setPackF((f) => ({ ...f, version: e.target.value }))} />
            </div>
            <div className="ag-field" style={{ display: "flex", alignItems: "flex-end" }}>
              <label className="ag-label">
                <input
                  type="checkbox"
                  checked={packF.isActive}
                  onChange={(e) => setPackF((f) => ({ ...f, isActive: e.target.checked }))}
                />{" "}
                Đang dùng cho ca này
              </label>
            </div>
          </div>
          <button type="submit" className="ag-btn ag-btn--secondary">
            Tạo gói mới
          </button>
        </form>

        <form
          className="ag-stack ag-stack--sm"
          style={{ marginTop: 16 }}
          onSubmit={(e) => {
            e.preventDefault();
            if (!assetF.packId || !assetF.file) {
              setCmsMsg("Chọn gói và tệp cần tải lên.");
              return;
            }
            return runCms(() => cmsUploadPackAsset(token, assetF.packId, assetF.kind, assetF.file!));
          }}
        >
          <div className="ag-upload-grid">
            <div className="ag-field">
              <label className="ag-label">Gói chấm</label>
              <select className="ag-input" value={assetF.packId} onChange={(e) => setAssetF((f) => ({ ...f, packId: e.target.value }))}>
                <option value="">—</option>
                {packs.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.label} v{p.version}
                  </option>
                ))}
              </select>
            </div>
            <div className="ag-field">
              <label className="ag-label">Loại tệp (số theo hướng dẫn phía trên)</label>
              <input
                type="number"
                className="ag-input"
                value={assetF.kind}
                onChange={(e) => setAssetF((f) => ({ ...f, kind: Number(e.target.value) }))}
              />
            </div>
            <div className="ag-field">
              <label className="ag-label">Tệp cần tải lên</label>
              <input
                type="file"
                className="ag-input"
                onChange={(e) => setAssetF((f) => ({ ...f, file: e.target.files?.[0] ?? null }))}
              />
            </div>
          </div>
          <button type="submit" className="ag-btn ag-btn--primary">
            Tải tệp lên gói
          </button>
        </form>
      </section>
    </div>
  );
}
