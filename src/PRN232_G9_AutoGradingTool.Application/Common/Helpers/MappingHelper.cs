using AutoMapper;
using PRN232_G9_AutoGradingTool.Domain.Common;

namespace PRN232_G9_AutoGradingTool.Application.Common.Helpers;

public static class MappingHelper
{
    /// <summary>
    /// Configures mapping to ignore BaseEntity fields except Status
    /// Used for mapping from DTOs/Requests to Entities (one-way only)
    /// </summary>
    /// <typeparam name="TSource">Source type (DTO/Request)</typeparam>
    /// <typeparam name="TDestination">Destination type (Entity inheriting BaseEntity)</typeparam>
    /// <param name="mappingExpression">The mapping expression to configure</param>
    /// <returns>Configured mapping expression</returns>
    public static IMappingExpression<TSource, TDestination> IgnoreBaseEntityFields<TSource, TDestination>(
        this IMappingExpression<TSource, TDestination> mappingExpression)
        where TDestination : BaseEntity
    {
        return mappingExpression
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore());
            // Note: Status is NOT ignored - it can be mapped if needed
    }

    /// <summary>
    /// Configures mapping to ignore all BaseEntity fields including Status
    /// Use when Status should not be updated from request
    /// </summary>
    /// <typeparam name="TSource">Source type (DTO/Request)</typeparam>
    /// <typeparam name="TDestination">Destination type (Entity inheriting BaseEntity)</typeparam>
    /// <param name="mappingExpression">The mapping expression to configure</param>
    /// <returns>Configured mapping expression</returns>
    public static IMappingExpression<TSource, TDestination> IgnoreAllBaseEntityFields<TSource, TDestination>(
        this IMappingExpression<TSource, TDestination> mappingExpression)
        where TDestination : BaseEntity
    {
        return mappingExpression
            .IgnoreBaseEntityFields()
            .ForMember(dest => dest.Status, opt => opt.Ignore());
    }

    /// <summary>
    /// Configures mapping to ignore Identity-specific fields for AppUser
    /// Used when updating user profile to avoid changing authentication-related fields
    /// </summary>
    /// <typeparam name="TSource">Source type (DTO/Request)</typeparam>
    /// <param name="mappingExpression">The mapping expression to configure</param>
    /// <returns>Configured mapping expression</returns>
    public static IMappingExpression<TSource, Domain.Entities.AppUser> IgnoreIdentityFields<TSource>(
        this IMappingExpression<TSource, Domain.Entities.AppUser> mappingExpression)
    {
        return mappingExpression
            .ForMember(dest => dest.NormalizedUserName, opt => opt.Ignore())
            .ForMember(dest => dest.NormalizedEmail, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
            .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore())
            .ForMember(dest => dest.TwoFactorEnabled, opt => opt.Ignore())
            .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
            .ForMember(dest => dest.LockoutEnabled, opt => opt.Ignore())
            .ForMember(dest => dest.AccessFailedCount, opt => opt.Ignore())
            .ForMember(dest => dest.RefreshToken, opt => opt.Ignore())
            .ForMember(dest => dest.RefreshTokenExpiryTime, opt => opt.Ignore())
            .ForMember(dest => dest.JoiningAt, opt => opt.Ignore())
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore());
    }
}
