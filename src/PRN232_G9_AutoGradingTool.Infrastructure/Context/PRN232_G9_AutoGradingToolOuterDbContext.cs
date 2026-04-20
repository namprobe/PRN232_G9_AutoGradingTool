using Microsoft.EntityFrameworkCore;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Context;

public class PRN232_G9_AutoGradingToolOuterDbContext : DbContext
{
    public PRN232_G9_AutoGradingToolOuterDbContext(DbContextOptions<PRN232_G9_AutoGradingToolOuterDbContext> options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply shared BaseEntity configuration to all entities
        BaseEntityConfigurationHelper.ConfigureBaseEntities(modelBuilder);
    }
}
