using Microsoft.AspNetCore.Identity;
using PRN232_G9_AutoGradingTool.Domain.Enums;
using PRN232_G9_AutoGradingTool.Domain.Common;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

public class AppRole : IdentityRole<Guid>, IEntityLike
{
    public string Description { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public EntityStatusEnum Status { get; set; }
}