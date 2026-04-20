import { Link, useParams } from "react-router-dom";
import { useCallback, useEffect, useMemo, useState } from "react";
import { useAuth } from "../auth/AuthContext";
import { getSubmission, replaceSubmissionFile, triggerRegrade } from "../api/gradingApi";
import type { ExamSubmissionDetail, ExamTestCaseScore } from "../api/gradingTypes";
import { StatusBadge } from "../components/StatusBadge";
import { questionLabelsForReplace, submissionMaxScore, workflowToQPair } from "../lib/gradingUi";

function outcomeToUi(outcome: string): "pass" | "fail" | "pending" | "error" {
  const o = (outcome || "").toLowerCase();
  if (o === "pass") return "pass";
  if (o === "fail") return "fail";
  if (o === "error") return "error";
  return "pending";
}

function weightOf(tc: ExamTestCaseScore, all: ExamTestCaseScore[]): number {
  const sum = all.reduce((a, x) => a + x.maxPoints, 0);
  if (sum <= 0) return 0;
  return tc.maxPoints / sum;
}

export function SubmissionDetailPage() {
  const { submissionId } = useParams<{ submissionId: string }>();
  const { token } = useAuth();
  const [detail, setDetail] = useState<ExamSubmissionDetail | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [adminMsg, setAdminMsg] = useState<string | null>(null);
  const [adminErr, setAdminErr] = useState<string | null>(null);
  const [replaceLabel, setReplaceLabel] = useState("Q1");
  const [replaceFile, setReplaceFile] = useState<File | null>(null);
  const [replaceBusy, setReplaceBusy] = useState(false);
  const [regradeBusy, setRegradeBusy] = useState(false);

  const load = useCallback(async () => {
    if (!submissionId) return;
    setErr(null);
    const r = await getSubmission(token, submissionId);
    if (!r.isSuccess || !r.data) {
      setDetail(null);
      setErr(r.message ?? "Không tải được chi tiết");
    } else {
      setDetail(r.data);
      const labels = questionLabelsForReplace(r.data);
      setReplaceLabel((prev) => (labels.includes(prev) ? prev : labels[0] ?? "Q1"));
    }
  }, [token, submissionId]);

  useEffect(() => {
    if (!submissionId) {
      setLoading(false);
      setErr("Thiếu mã bài nộp");
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
  }, [load, submissionId]);

  const maxScore = useMemo(() => (detail ? submissionMaxScore(detail) : 10), [detail]);
  const qPair = detail ? workflowToQPair(detail.status) : { q1: "pending" as const, q2: "pending" as const };
  const labels = detail ? questionLabelsForReplace(detail) : ["Q1", "Q2"];

  async function onReplaceSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!submissionId || !replaceFile) {
      setAdminErr("Chọn file .zip.");
      return;
    }
    setAdminErr(null);
    setAdminMsg(null);
    setReplaceBusy(true);
    const r = await replaceSubmissionFile(token, submissionId, replaceLabel, replaceFile);
    setReplaceBusy(false);
    if (!r.isSuccess) setAdminErr(r.message ?? "Thay file thất bại");
    else {
      setAdminMsg(r.message ?? "Đã thay file.");
      setReplaceFile(null);
      await load();
    }
  }

  async function onRegrade() {
    if (!submissionId) return;
    setAdminErr(null);
    setAdminMsg(null);
    setRegradeBusy(true);
    const r = await triggerRegrade(token, submissionId);
    setRegradeBusy(false);
    if (!r.isSuccess) setAdminErr(r.message ?? "Trigger regrade thất bại");
    else {
      const extra = r.data ? ` Job ${r.data.gradingJobId} · ${r.data.jobStatus}.` : "";
      setAdminMsg((r.message ?? "Đã enqueue chấm lại.") + extra);
      await load();
    }
  }

  if (loading) {
    return (
      <div className="ag-empty ag-animate-in">
        <p className="ag-table__muted">Đang tải chi tiết…</p>
      </div>
    );
  }

  if (err || !detail) {
    return (
      <div className="ag-empty ag-animate-in">
        <h2 className="ag-empty__title">Không tìm thấy bài nộp</h2>
        <p className="ag-empty__text">{err ?? "Không có dữ liệu"}</p>
        <Link to="/submissions" className="ag-btn ag-btn--primary">
          Quay lại danh sách
        </Link>
      </div>
    );
  }

  const pct =
    detail.totalScore != null && maxScore > 0 ? Math.round((detail.totalScore / maxScore) * 100) : null;
  const cases = detail.testCaseScores;
  const sessionHref = `/exam-sessions/${detail.examSessionId}`;
  const subsHref = `/submissions?examSessionId=${encodeURIComponent(detail.examSessionId)}`;

  return (
    <div className="ag-stack ag-stack--lg">
      <div className="ag-detail-head ag-animate-in">
        <div>
          <Link to="/submissions" className="ag-backlink">
            ← Danh sách bài nộp
          </Link>
          <h2 className="ag-detail-head__title">
            {detail.studentName ?? "—"}{" "}
            <code className="ag-code ag-code--lg">{detail.studentCode}</code>
          </h2>
          <p className="ag-detail-head__meta">
            Ca <code className="ag-code">{detail.examSessionCode}</code> · WorkflowStatus{" "}
            <code className="ag-code ag-code--sm">{detail.status}</code> · Nộp lúc{" "}
            {new Date(detail.submittedAtUtc).toLocaleString("vi-VN")} ·{" "}
            <Link to={sessionHref} className="ag-linkbtn">
              Cấu trúc đề
            </Link>{" "}
            ·{" "}
            <Link to={subsHref} className="ag-linkbtn">
              Cùng ca thi
            </Link>
          </p>
        </div>
        <div className="ag-detail-head__side">
          <div className="ag-pillboard">
            <div className="ag-pillboard__item">
              <span className="ag-pillboard__k">Q1</span>
              <StatusBadge status={qPair.q1} />
            </div>
            <div className="ag-pillboard__item">
              <span className="ag-pillboard__k">Q2</span>
              <StatusBadge status={qPair.q2} />
            </div>
          </div>
          {pct != null ? (
            <div className="ag-ring-wrap" aria-label={`Điểm ${pct} phần trăm`}>
              <svg className="ag-ring" viewBox="0 0 36 36">
                <path
                  className="ag-ring__bg"
                  d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831"
                />
                <path
                  className="ag-ring__fg"
                  strokeDasharray={`${pct}, 100`}
                  d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831"
                />
              </svg>
              <div className="ag-ring__label">
                <span className="ag-ring__value">{detail.totalScore}</span>
                <span className="ag-ring__max">/{maxScore}</span>
              </div>
            </div>
          ) : (
            <div className="ag-score-pending">Chưa có tổng điểm</div>
          )}
        </div>
      </div>

      {adminErr ? (
        <div className="ag-alert ag-alert--err" role="alert">
          {adminErr}
        </div>
      ) : null}
      {adminMsg ? (
        <div className="ag-alert ag-alert--ok" role="status">
          {adminMsg}
        </div>
      ) : null}

      <section className="ag-card ag-card--flush ag-animate-in">
        <div className="ag-card__head ag-card__head--row">
          <div>
            <h3 className="ag-card__title">File đã nộp</h3>
            <p className="ag-card__desc">submissionFiles từ GET submissions/{"{id}"}</p>
          </div>
        </div>
        <div className="ag-table-wrap">
          <table className="ag-table">
            <thead>
              <tr>
                <th>Câu</th>
                <th>Tên gốc</th>
                <th>Đường dẫn lưu</th>
              </tr>
            </thead>
            <tbody>
              {detail.submissionFiles.length === 0 ? (
                <tr>
                  <td colSpan={3} className="ag-table__muted">
                    Chưa có bản ghi file (có thể bài cũ trước khi có ExamSubmissionFile)
                  </td>
                </tr>
              ) : (
                detail.submissionFiles.map((f) => (
                  <tr key={`${f.questionLabel}-${f.storageRelativePath}`}>
                    <td>
                      <span className="ag-qtag">{f.questionLabel}</span>
                    </td>
                    <td>{f.originalFileName ?? "—"}</td>
                    <td className="ag-table__muted">
                      <code className="ag-code ag-code--sm">{f.storageRelativePath}</code>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </section>

      <section className="ag-card ag-animate-in">
        <div className="ag-card__head">
          <h3 className="ag-card__title">Admin — thay file &amp; chấm lại</h3>
          <p className="ag-card__desc">
            PUT /submissions/{"{id}"}/files (multipart) rồi POST /submissions/{"{id}"}/regrade — khớp Swagger CMS_Grading
          </p>
        </div>
        <div className="ag-grid2" style={{ alignItems: "start" }}>
          <form className="ag-stack ag-stack--sm" onSubmit={onReplaceSubmit}>
            <div className="ag-field">
              <label className="ag-label" htmlFor="replace-label">
                Nhãn câu (questionLabel)
              </label>
              <select
                id="replace-label"
                className="ag-input"
                value={replaceLabel}
                onChange={(e) => setReplaceLabel(e.target.value)}
              >
                {labels.map((lb) => (
                  <option key={lb} value={lb}>
                    {lb}
                  </option>
                ))}
              </select>
            </div>
            <div className="ag-field">
              <label className="ag-label" htmlFor="replace-zip">
                File zip thay thế
              </label>
              <input
                id="replace-zip"
                className="ag-input"
                type="file"
                accept=".zip,application/zip"
                onChange={(e) => setReplaceFile(e.target.files?.[0] ?? null)}
              />
            </div>
            <button type="submit" className="ag-btn ag-btn--secondary" disabled={replaceBusy}>
              {replaceBusy ? "Đang tải…" : "Thay file zip"}
            </button>
          </form>
          <div className="ag-stack ag-stack--sm">
            <p className="ag-table__muted" style={{ margin: 0 }}>
              Sau khi thay file, backend đặt workflow về Pending và xóa điểm cũ — cần bấm chấm lại.
            </p>
            <button type="button" className="ag-btn ag-btn--primary" disabled={regradeBusy} onClick={() => void onRegrade()}>
              {regradeBusy ? "Đang chấm…" : "Trigger chấm lại (regrade)"}
            </button>
          </div>
        </div>
      </section>

      {detail.questionScores.length > 0 ? (
        <section className="ag-card ag-card--flush ag-animate-in">
          <div className="ag-card__head ag-card__head--row">
            <div>
              <h3 className="ag-card__title">Điểm theo câu</h3>
              <p className="ag-card__desc">Tổng hợp questionScores</p>
            </div>
          </div>
          <div className="ag-table-wrap">
            <table className="ag-table">
              <thead>
                <tr>
                  <th>Câu</th>
                  <th>Điểm</th>
                  <th>Ghi chú</th>
                </tr>
              </thead>
              <tbody>
                {detail.questionScores.map((q) => (
                  <tr key={q.examQuestionId}>
                    <td>
                      <span className="ag-qtag">{q.questionLabel}</span>
                    </td>
                    <td>
                      <span className="ag-score">
                        {q.score}
                        <span className="ag-score__max">/{q.maxScore}</span>
                      </span>
                    </td>
                    <td className="ag-table__muted">{q.summary ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      ) : null}

      <section className="ag-card ag-card--flush ag-animate-in">
        <div className="ag-card__head ag-card__head--row">
          <div>
            <h3 className="ag-card__title">Testcase & điểm thành phần</h3>
            <p className="ag-card__desc">GET /api/cms/grading/submissions/{"{id}"}</p>
          </div>
        </div>
        <div className="ag-table-wrap">
          <table className="ag-table">
            <thead>
              <tr>
                <th>Câu</th>
                <th>Tên testcase</th>
                <th>Trọng số</th>
                <th>Đạt / Tối đa</th>
                <th>Kết quả</th>
              </tr>
            </thead>
            <tbody>
              {cases.length === 0 ? (
                <tr>
                  <td colSpan={5} className="ag-table__muted">
                    Chưa có điểm testcase (đang chấm hoặc lỗi)
                  </td>
                </tr>
              ) : (
                cases.map((tc) => {
                  const st = outcomeToUi(tc.outcome);
                  const w = weightOf(tc, cases);
                  return (
                    <tr key={`${tc.examTestCaseId}-${tc.testCaseName}`}>
                      <td>
                        <span className="ag-qtag">{tc.questionLabel}</span>
                      </td>
                      <td className="ag-table__strong">{tc.testCaseName}</td>
                      <td className="ag-table__muted">{(w * 100).toFixed(0)}%</td>
                      <td>
                        <span className="ag-score">
                          {tc.pointsEarned}
                          <span className="ag-score__max">/{tc.maxPoints}</span>
                        </span>
                      </td>
                      <td>
                        <span
                          className={
                            "ag-mini " +
                            (st === "pass"
                              ? "ag-mini--ok"
                              : st === "fail" || st === "error"
                                ? "ag-mini--err"
                                : "ag-mini--muted")
                          }
                        >
                          {st === "pass" ? "Đạt" : st === "fail" ? "Trượt" : st === "error" ? "Lỗi" : "Chờ"}
                        </span>
                        {tc.message ? <div className="ag-table__hint">{tc.message}</div> : null}
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
