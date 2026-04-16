using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Domain.Enums;
using PRN232_G9_AutoGradingTool.Infrastructure.Context;
using PRN232_G9_AutoGradingTool.Infrastructure.Seeding;

namespace PRN232_G9_AutoGradingTool.API.Extensions;

public static class SeedingExtension
{
    public static async Task SeedInitialDataAsync(this IApplicationBuilder app, ILogger logger)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var enableSeeding = configuration.GetSection("DataSeeding").GetValue<bool>("EnableSeeding");
        if (!enableSeeding)
        {
            logger.LogInformation("Data seeding is disabled.");
            return;
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        // Seed roles from RoleEnum
        foreach (var roleName in Enum.GetNames(typeof(RoleEnum)))
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new AppRole
                {
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant(),
                    Status = EntityStatusEnum.Active,
                    CreatedAt = DateTime.UtcNow
                };
                var roleResult = await roleManager.CreateAsync(role);
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    logger.LogWarning("Failed to create role {Role}: {Errors}", roleName, errors);
                }
                else
                {
                    logger.LogInformation("Created role: {Role}", roleName);
                }
            }
        }

        var db = scope.ServiceProvider.GetRequiredService<PRN232_G9_AutoGradingToolDbContext>();
        await ExamGradingSeeder.SeedAsync(db, logger, CancellationToken.None);
        await ExamGradingPackSeeder.SeedAsync(db, logger, CancellationToken.None);

        // Seed admin user
        var adminEmail = configuration.GetSection("AdminUser").GetValue<string>("Email")?.Trim();
        var adminPassword = configuration.GetSection("AdminUser").GetValue<string>("DefaultPassword");
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("AdminUser configuration is missing. Skipping admin seeding.");
            logger.LogInformation("Data seeding completed (no admin).");
            return;
        }

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var admin = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Admin",
                JoiningAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Status = EntityStatusEnum.Active
            };

            var createResult = await userManager.CreateAsync(admin, adminPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                logger.LogWarning("Failed to create admin user: {Errors}", errors);
                return;
            }
            existingAdmin = admin;
            logger.LogInformation("Created admin user successfully");
        }

        // Ensure admin is in Admin role
        var adminRoleName = RoleEnum.SystemAdmin.ToString();

        if (!await userManager.IsInRoleAsync(existingAdmin, adminRoleName))
        {
            var addRoleResult = await userManager.AddToRoleAsync(existingAdmin, adminRoleName);
            if (!addRoleResult.Succeeded)
            {
                var errors = string.Join(", ", addRoleResult.Errors.Select(e => e.Description));
                logger.LogWarning("Failed to add admin user to role {Role}: {Errors}", adminRoleName, errors);
            }
            else
            {
                logger.LogInformation("Added admin user to admin role successfully");
            }
        }

        // Final verification (without logging sensitive information)
        var roles = await userManager.GetRolesAsync(existingAdmin);
        if (roles.Any())
        {
            logger.LogInformation("Admin user roles configured successfully");
        }

        logger.LogInformation("Data seeding completed.");

        // Seed sample RAG data (Documents & Chunks) using Entity Framework
        //try
        // {
            // var db = scope.ServiceProvider.GetRequiredService<PRN232_G9_AutoGradingTool.Infrastructure.Context.PRN232_G9_AutoGradingToolDbContext>();

            // // Check if any documents exist
            // if (!await db.Documents.AnyAsync())
            // {
            //     logger.LogInformation("Seeding sample document and chunks...");

            //     var docId = Guid.NewGuid();
            //     var doc = new Document
            //     {
            //         Id = docId,
            //         Title = "Sample Doc",
            //         FilePath = "sample.txt",
            //         UploadedAt = DateTime.UtcNow,
            //         Status = EntityStatusEnum.Active,
            //         CreatedAt = DateTime.UtcNow,
            //         StatusText = "Processed"
            //     };

            //     db.Documents.Add(doc);

            //     // Create embedding vectors
            //     // Vector 1: [1.0, 0.0, ..., 0.0]
            //     var vec1 = new float[768];
            //     vec1[0] = 1.0f;

            //     // Vector 2: [0.0, 1.0, ..., 0.0]
            //     var vec2 = new float[768];
            //     vec2[1] = 1.0f;

            //     var chunks = new[]
            //     {
            //         new DocumentChunk
            //         {
            //             Id = Guid.NewGuid(),
            //             DocumentId = docId,
            //             ChunkIndex = 0,
            //             ChunkText = "Chunk A about apples",
            //             TokenCount = 10,
            //             Embedding = vec1,
            //             Status = EntityStatusEnum.Active,
            //             CreatedAt = DateTime.UtcNow
            //         },
            //         new DocumentChunk
            //         {
            //             Id = Guid.NewGuid(),
            //             DocumentId = docId,
            //             ChunkIndex = 1,
            //             ChunkText = "Chunk B about oranges",
            //             TokenCount = 9,
            //             Embedding = vec2,
            //             Status = EntityStatusEnum.Active,
            //             CreatedAt = DateTime.UtcNow
            //         }
            //     };

            //     db.DocumentChunks.AddRange(chunks);
            //     await db.SaveChangesAsync();

            //     logger.LogInformation("Seeded sample document chunks for RAG successfully.");
            // }
        // }
        // catch (Exception ex)
        // {
        //     logger.LogWarning(ex, "Failed to seed sample RAG data.");
        // }
    }
}


