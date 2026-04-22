import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AuthProvider, useAuth } from "./auth/AuthContext";
import { AppShell } from "./layout/AppShell";
import { LoginPage } from "./pages/LoginPage";
import { DashboardPage } from "./pages/DashboardPage";
import { ExamSessionsPage } from "./pages/ExamSessionsPage";
import { ExamSessionDetailPage } from "./pages/ExamSessionDetailPage";
import { SemestersPage } from "./pages/SemestersPage";
import { GradingPackInfoPage } from "./pages/GradingPackInfoPage";
import { SystemFlowsPage } from "./pages/SystemFlowsPage";
import { SubmissionsListPage } from "./pages/SubmissionsListPage";
import { SubmissionDetailPage } from "./pages/SubmissionDetailPage";
import { UploadSubmissionsPage } from "./pages/UploadSubmissionsPage";
import "./styles.css";

function PrivateLayout() {
  const { token } = useAuth();
  if (!token) return <Navigate to="/login" replace />;
  return <AppShell />;
}

function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<PrivateLayout />}>
        <Route index element={<DashboardPage />} />
        <Route path="semesters" element={<SemestersPage />} />
        <Route path="exam-sessions/:sessionId/upload" element={<UploadSubmissionsPage />} />
        <Route path="exam-sessions/:sessionId/submissions" element={<SubmissionsListPage />} />
        <Route path="exam-sessions/:sessionId" element={<ExamSessionDetailPage />} />
        <Route path="exam-sessions" element={<ExamSessionsPage />} />
        <Route path="grading-pack" element={<GradingPackInfoPage />} />
        <Route path="system-flows" element={<SystemFlowsPage />} />
        <Route path="submissions/upload" element={<Navigate to="/exam-sessions" replace />} />
        <Route path="submissions" element={<SubmissionsListPage />} />
        <Route path="submissions/:submissionId" element={<SubmissionDetailPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <AppRoutes />
      </AuthProvider>
    </BrowserRouter>
  );
}
