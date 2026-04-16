import { Link, useParams } from "react-router-dom";
import { useEffect, useMemo, useState } from "react";
import { useAuth } from "../auth/AuthContext";
import { getSubmission } from "../api/gradingApi";
import type { ExamSubmissionDetail, ExamTestCaseScore } from "../api/gradingTypes";
import { StatusBadge } from "../components/StatusBadge";
import { submissionMaxScore, workflowToQPair } from "../lib/gradingUi";

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

  useEffect(() => {
    if (!submissionId) {
      setLoading(false);
      setErr("Thiếu mã bài nộp");
      return;
    }
    let cancelled = false;
    (async () => {
      setLoading(true);
      setErr(null);
      const r = await getSubmission(token, submissionId);
      if (cancelled) return;
      if (!r.isSuccess || !r.data) {
        setDetail(null);
        setErr(r.message ?? "Không tải được chi tiết");
      } else setDetail(r.data);
      setLoading(false);
    })();
    return () => {
      cancelled = true;
    };
  }, [token, submissionId]);

  const maxScore = useMemo(() => (detail ? submissionMaxScore(detail) : 10), [detail]);
  const qPair = detail ? workflowToQPair(detail.status) : { q1: "pending" as const, q2: "pending" as const };

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
            Ca <code className="ag-code">{detail.examSessionCode}</code> · Pipeline{" "}
            <code className="ag-code ag-code--sm">{detail.status}</code> · Nộp lúc{" "}
            {new Date(detail.submittedAtUtc).toLocaleString("vi-VN")}
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
