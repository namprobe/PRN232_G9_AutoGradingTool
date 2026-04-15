import { Link, useParams } from "react-router-dom";
import { getSubmissionById, getTestCasesForSubmission } from "../mock/cmsMockData";
import { StatusBadge } from "../components/StatusBadge";

export function SubmissionDetailPage() {
  const { submissionId } = useParams<{ submissionId: string }>();
  const sub = submissionId ? getSubmissionById(submissionId) : undefined;
  const cases = submissionId ? getTestCasesForSubmission(submissionId) : [];

  if (!sub) {
    return (
      <div className="ag-empty ag-animate-in">
        <h2 className="ag-empty__title">Không tìm thấy bài nộp</h2>
        <p className="ag-empty__text">Mã không khớp dữ liệu mẫu (sub-1 …).</p>
        <Link to="/submissions" className="ag-btn ag-btn--primary">
          Quay lại danh sách
        </Link>
      </div>
    );
  }

  const pct =
    sub.totalScore != null && sub.maxScore > 0 ? Math.round((sub.totalScore / sub.maxScore) * 100) : null;

  return (
    <div className="ag-stack ag-stack--lg">
      <div className="ag-detail-head ag-animate-in">
        <div>
          <Link to="/submissions" className="ag-backlink">
            ← Danh sách bài nộp
          </Link>
          <h2 className="ag-detail-head__title">
            {sub.studentName}{" "}
            <code className="ag-code ag-code--lg">{sub.studentCode}</code>
          </h2>
          <p className="ag-detail-head__meta">
            Ca <code className="ag-code">{sub.examSessionCode}</code> · Nộp lúc{" "}
            {new Date(sub.submittedAt).toLocaleString("vi-VN")}
          </p>
        </div>
        <div className="ag-detail-head__side">
          <div className="ag-pillboard">
            <div className="ag-pillboard__item">
              <span className="ag-pillboard__k">Q1</span>
              <StatusBadge status={sub.q1Status} />
            </div>
            <div className="ag-pillboard__item">
              <span className="ag-pillboard__k">Q2</span>
              <StatusBadge status={sub.q2Status} />
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
                <span className="ag-ring__value">{sub.totalScore}</span>
                <span className="ag-ring__max">/{sub.maxScore}</span>
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
            <p className="ag-card__desc">Bảng mẫu — sau này map từ QuestionScore / TestCase</p>
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
              {cases.map((tc) => (
                <tr key={tc.id}>
                  <td>
                    <span className="ag-qtag">{tc.questionLabel}</span>
                  </td>
                  <td className="ag-table__strong">{tc.name}</td>
                  <td className="ag-table__muted">{(tc.weight * 100).toFixed(0)}%</td>
                  <td>
                    {tc.earned != null ? (
                      <span className="ag-score">
                        {tc.earned}
                        <span className="ag-score__max">/{tc.max}</span>
                      </span>
                    ) : (
                      <span className="ag-table__muted">—</span>
                    )}
                  </td>
                  <td>
                    <span className={"ag-mini " + (tc.status === "pass" ? "ag-mini--ok" : tc.status === "fail" ? "ag-mini--err" : tc.status === "error" ? "ag-mini--err" : "ag-mini--muted")}>
                      {tc.status === "pass" ? "Đạt" : tc.status === "fail" ? "Trượt" : tc.status === "error" ? "Lỗi" : "Chờ"}
                    </span>
                    {tc.message ? <div className="ag-table__hint">{tc.message}</div> : null}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
