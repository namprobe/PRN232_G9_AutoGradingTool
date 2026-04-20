import { Link, useParams } from "react-router-dom";
import { useEffect, useState } from "react";
import { useAuth } from "../auth/AuthContext";
import { getExamSession } from "../api/gradingApi";
import type { ExamSessionDetail } from "../api/gradingTypes";
import { SessionStatusBadge } from "../components/StatusBadge";
import { inferSessionStatus } from "../lib/gradingUi";

export function ExamSessionDetailPage() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const { token } = useAuth();
  const [detail, setDetail] = useState<ExamSessionDetail | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!sessionId) {
      setLoading(false);
      setErr("Thiếu mã ca thi");
      return;
    }
    let cancelled = false;
    (async () => {
      setLoading(true);
      setErr(null);
      const r = await getExamSession(token, sessionId);
      if (cancelled) return;
      if (!r.isSuccess || !r.data) {
        setDetail(null);
        setErr(r.message ?? "Không tải được ca thi");
      } else setDetail(r.data);
      setLoading(false);
    })();
    return () => {
      cancelled = true;
    };
  }, [token, sessionId]);

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

  const subsUrl = `/submissions?examSessionId=${encodeURIComponent(detail.id)}`;

  return (
    <div className="ag-stack ag-stack--lg">
      <div className="ag-detail-head ag-animate-in">
        <div>
          <Link to="/exam-sessions" className="ag-backlink">
            ← Danh sách ca thi
          </Link>
          <h2 className="ag-detail-head__title">
            <code className="ag-code ag-code--lg">{detail.code}</code> {detail.title}
          </h2>
          <p className="ag-detail-head__meta">
            Học kỳ <code className="ag-code">{detail.semesterCode}</code> · Bắt đầu{" "}
            {new Date(detail.startsAtUtc).toLocaleString("vi-VN")} UTC · Thời lượng{" "}
            <strong>{detail.examDurationMinutes}</strong> phút · Đóng nộp{" "}
            {new Date(detail.endsAtUtc).toLocaleString("vi-VN")} UTC ·{" "}
            <SessionStatusBadge status={inferSessionStatus(detail.startsAtUtc, detail.endsAtUtc)} />
          </p>
        </div>
        <div className="ag-detail-head__side">
          <Link to={subsUrl} className="ag-btn ag-btn--primary">
            Bài nộp ca này
          </Link>
          <Link to="/grading-pack" className="ag-btn ag-btn--secondary">
            Pack chấm
          </Link>
        </div>
      </div>

      <div className="ag-alert ag-alert--info" role="status">
        Chủ đề và câu hỏi lấy từ <code className="ag-code ag-code--sm">GET /api/cms/grading/exam-sessions/{"{id}"}</code>.
        Tạo / sửa cấu trúc đề trong DB hoặc chờ API quản trị từ nhóm backend.
      </div>

      <section className="ag-card ag-animate-in">
        <div className="ag-card__head">
          <h3 className="ag-card__title">Chủ đề &amp; câu hỏi</h3>
          <p className="ag-card__desc">Testcase kèm theo từng câu</p>
        </div>
        <ul className="ag-stack ag-stack--sm" style={{ listStyle: "none", padding: 0, margin: 0 }}>
          {detail.topics.map((t) => (
            <li key={t.id} className="ag-card" style={{ padding: "0.75rem 1rem" }}>
              <strong>{t.title}</strong>
              <span className="ag-table__muted"> · thứ tự {t.sortOrder}</span>
              <ul className="ag-stack ag-stack--sm" style={{ marginTop: "0.5rem", paddingLeft: "1.1rem" }}>
                {t.questions.map((q) => (
                  <li key={q.id}>
                    <span className="ag-qtag">{q.label}</span> {q.title}{" "}
                    <span className="ag-table__muted">(tối đa {q.maxScore} điểm)</span>
                    <ul style={{ marginTop: "0.25rem", paddingLeft: "1rem", fontSize: "0.88rem" }}>
                      {q.testCases.map((tc) => (
                        <li key={tc.id}>
                          {tc.name} — {tc.maxPoints} điểm
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
    </div>
  );
}
