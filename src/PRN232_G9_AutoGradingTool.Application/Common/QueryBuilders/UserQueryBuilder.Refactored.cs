// using System.Linq.Expressions;
// using PRN232_G9_AutoGradingTool.Application.Common.DTOs.User;
// using PRN232_G9_AutoGradingTool.Domain.Entities;

// namespace PRN232_G9_AutoGradingTool.Application.Common.QueryBuilders;

// /// <summary>
// /// Refactored UserQueryBuilder using GenericQueryBuilder
// /// </summary>
// public static class UserQueryBuilderRefactored
// {
//     private static GenericQueryBuilder<AppUser, UserFilter> CreateBuilder(UserFilter filter)
//     {
//         return filter.CreateQueryBuilder<AppUser, UserFilter>()
//             // Configure search properties - supports nullable strings
//             .AddSearchProperty(x => (object)x.FirstName)
//             .AddSearchProperty(x => (object)x.LastName)
//             .AddSearchProperty(x => (object)x.Email!)
//             .AddSearchProperty(x => (object)x.PhoneNumber!)
            
//             // Configure custom filters
//             .AddFilterRule(
//                 f => f.Status.HasValue,
//                 f => x => x.Status == f.Status!.Value)
            
//             .AddFilterRule(
//                 f => f.JoiningFrom.HasValue,
//                 f => x => x.JoiningAt >= f.JoiningFrom!.Value)
            
//             .AddFilterRule(
//                 f => f.JoiningTo.HasValue,
//                 f => x => x.JoiningAt <= f.JoiningTo!.Value)
            
//             // Configure sort mappings
//             .AddSortMapping("firstname", x => x.FirstName)
//             .AddSortMapping("lastname", x => x.LastName)
//             .AddSortMapping("email", x => x.Email!)
//             .AddSortMapping("joiningat", x => x.JoiningAt)
//             .AddSortMapping("lastloginat", x => x.LastLoginAt!)
//             .AddSortMapping("createdat", x => x.CreatedAt!)
//             .AddSortMapping("updatedat", x => x.UpdatedAt!)
            
//             // Set default order by
//             .SetDefaultOrderBy(x => x.CreatedAt!);
//     }

//     public static Expression<Func<AppUser, bool>> BuildPredicate(this UserFilter filter)
//     {
//         return CreateBuilder(filter).BuildPredicate();
//     }

//     public static Expression<Func<AppUser, object>> BuildOrderBy(this UserFilter filter)
//     {
//         return CreateBuilder(filter).BuildOrderBy();
//     }
// }
