using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PRN232_G9_AutoGradingTool.API.Configurations;

/// <summary>Thêm mô tả ngắn cho từng nhóm tag trên Swagger UI.</summary>
public sealed class SwaggerTagDescriptionDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags =
        [
            new OpenApiTag
            {
                Name = SwaggerTagResolver.Auth,
                Description = "JWT: login, logout, profile, refresh token — dùng trước khi gọi các nhóm CMS khác."
            },
            new OpenApiTag
            {
                Name = SwaggerTagResolver.Semester,
                Description = "Quản lý học kỳ (SPRING2026, …)."
            },
            new OpenApiTag
            {
                Name = SwaggerTagResolver.ExamSession,
                Description = "Phiên thi: mã ca, khung giờ, deferredClassGrading, chi tiết cây đề."
            },
            new OpenApiTag
            {
                Name = SwaggerTagResolver.ExamClass,
                Description = "Lớp trong học kỳ (SE1830), giới hạn sĩ số — gắn với ca thi qua nhóm “Lớp trong ca”."
            },
            new OpenApiTag
            {
                Name = SwaggerTagResolver.SessionClassBatch,
                Description = "Gắn lớp vào ca, expectedStudentCount, bắt đầu chấm batch theo lớp."
            },
            new OpenApiTag
            {
                Name = SwaggerTagResolver.Topic,
                Description = "Chủ đề trong đề; POST topic gắn theo examSessionId."
            },
            new OpenApiTag
            {
                Name = SwaggerTagResolver.Question,
                Description = "Câu hỏi Q1, Q2, … và điểm tối đa."
            },
            new OpenApiTag
            {
                Name = SwaggerTagResolver.TestCase,
                Description = "Testcase rubric (tên, maxPoints) — runner thật nối qua pack sau này."
            },
            new OpenApiTag
            {
                Name = SwaggerTagResolver.GradingPack,
                Description = "Gói chấm theo version, upload asset (Postman, …)."
            },
            new OpenApiTag
            {
                Name = SwaggerTagResolver.Submissions,
                Description = "Danh sách/chi tiết bài nộp, nộp hộ (CMS), thay file, regrade."
            },
            new OpenApiTag
            {
                Name = SwaggerTagResolver.StudentSubmit,
                Description = "Portal sinh viên: nộp 2 zip trong khung giờ ca; có thể kèm examSessionClassId."
            },
            new OpenApiTag { Name = SwaggerTagResolver.Other, Description = "Các endpoint không phân loại ở trên." }
        ];
    }
}
