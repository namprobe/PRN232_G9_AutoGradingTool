namespace PRN232_G9_AutoGradingTool.Domain.Enums;

public enum GradingJobLogPhase
{
    Extract = 0,
    Discover = 1,   // Tìm thư mục publish Q[n]_[studentCode]/ bên trong zip đã giải nén
    RunServer = 2,
    RunNewman = 3,
    Grade = 4,
    Cleanup = 5
}
