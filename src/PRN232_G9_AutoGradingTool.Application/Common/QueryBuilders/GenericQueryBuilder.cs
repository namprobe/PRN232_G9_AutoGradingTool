using System.Linq.Expressions;
using PRN232_G9_AutoGradingTool.Application.Common.Extensions;
using PRN232_G9_AutoGradingTool.Application.Common.Models;

namespace PRN232_G9_AutoGradingTool.Application.Common.QueryBuilders;

/// <summary>
/// Generic query builder for building predicates and orderBy expressions
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TFilter">Filter type that inherits from BasePaginationFilter</typeparam>
public class GenericQueryBuilder<TEntity, TFilter> where TFilter : BasePaginationFilter
{
    private readonly TFilter _filter;
    private readonly List<Func<TFilter, Expression<Func<TEntity, bool>>?>> _filterRules = new();
    private readonly List<Expression<Func<TEntity, string>>> _searchProperties = new();
    private readonly Dictionary<string, Expression<Func<TEntity, object>>> _sortMappings = new();
    private Expression<Func<TEntity, object>> _defaultOrderBy = x => true;

    public GenericQueryBuilder(TFilter filter)
    {
        _filter = filter;
    }

    /// <summary>
    /// Add a custom filter rule
    /// </summary>
    /// <param name="rule">Function that returns expression based on filter</param>
    public GenericQueryBuilder<TEntity, TFilter> AddFilterRule(
        Func<TFilter, Expression<Func<TEntity, bool>>?> rule)
    {
        _filterRules.Add(rule);
        return this;
    }

    /// <summary>
    /// Add a filter rule with condition check
    /// </summary>
    /// <param name="condition">Condition to check before applying filter</param>
    /// <param name="predicate">Predicate to apply if condition is true</param>
    public GenericQueryBuilder<TEntity, TFilter> AddFilterRule(
        Func<TFilter, bool> condition,
        Func<TFilter, Expression<Func<TEntity, bool>>> predicate)
    {
        _filterRules.Add(filter => condition(filter) ? predicate(filter) : null);
        return this;
    }

    /// <summary>
    /// Add searchable property (supports both nullable and non-nullable string properties)
    /// </summary>
    /// <param name="propertySelector">Property selector</param>
    public GenericQueryBuilder<TEntity, TFilter> AddSearchProperty(
        Expression<Func<TEntity, object>> propertySelector)
    {
        var parameter = propertySelector.Parameters[0];
        var body = propertySelector.Body;
        
        // Unwrap Convert expression if present (happens with value types boxed to object)
        if (body is UnaryExpression unaryExpr && unaryExpr.NodeType == ExpressionType.Convert)
        {
            body = unaryExpr.Operand;
        }
        
        var lambda = Expression.Lambda<Func<TEntity, string>>(body, parameter);
        _searchProperties.Add(lambda);
        return this;
    }

    /// <summary>
    /// Add sort mapping for a specific sort key
    /// </summary>
    /// <param name="sortKey">Sort key (case-insensitive)</param>
    /// <param name="propertySelector">Property selector</param>
    public GenericQueryBuilder<TEntity, TFilter> AddSortMapping(
        string sortKey,
        Expression<Func<TEntity, object>> propertySelector)
    {
        _sortMappings[sortKey.ToLowerInvariant()] = propertySelector;
        return this;
    }

    /// <summary>
    /// Set default order by expression
    /// </summary>
    /// <param name="propertySelector">Default property selector</param>
    public GenericQueryBuilder<TEntity, TFilter> SetDefaultOrderBy(
        Expression<Func<TEntity, object>> propertySelector)
    {
        _defaultOrderBy = propertySelector;
        return this;
    }

    /// <summary>
    /// Build the complete predicate expression
    /// </summary>
    public Expression<Func<TEntity, bool>> BuildPredicate()
    {
        var predicate = PredicateBuilder.True<TEntity>();

        // Apply custom filter rules
        foreach (var rule in _filterRules)
        {
            var filterExpression = rule(_filter);
            if (filterExpression != null)
            {
                predicate = predicate.CombineAnd(filterExpression);
            }
        }

        // Apply search across all registered search properties
        if (!string.IsNullOrWhiteSpace(_filter.Search) && _searchProperties.Any())
        {
            var searchPredicate = PredicateBuilder.False<TEntity>();
            
            foreach (var searchProperty in _searchProperties)
            {
                var searchValue = _filter.Search;
                var parameter = searchProperty.Parameters[0];
                var propertyBody = searchProperty.Body;

                // Create: property.Contains(searchValue)
                var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
                var containsCall = Expression.Call(propertyBody, containsMethod, Expression.Constant(searchValue));

                // Check if nullable - add null check
                if (IsNullableProperty(propertyBody))
                {
                    var notNull = Expression.NotEqual(propertyBody, Expression.Constant(null));
                    var combined = Expression.AndAlso(notNull, containsCall);
                    var lambda = Expression.Lambda<Func<TEntity, bool>>(combined, parameter);
                    searchPredicate = searchPredicate.CombineOr(lambda);
                }
                else
                {
                    var lambda = Expression.Lambda<Func<TEntity, bool>>(containsCall, parameter);
                    searchPredicate = searchPredicate.CombineOr(lambda);
                }
            }

            predicate = predicate.CombineAnd(searchPredicate);
        }

        return predicate;
    }

    /// <summary>
    /// Build order by expression based on filter
    /// </summary>
    public Expression<Func<TEntity, object>> BuildOrderBy()
    {
        if (string.IsNullOrWhiteSpace(_filter.SortBy))
        {
            return _defaultOrderBy;
        }

        var sortKey = _filter.SortBy.ToLowerInvariant();
        
        return _sortMappings.TryGetValue(sortKey, out var orderByExpression) 
            ? orderByExpression 
            : _defaultOrderBy;
    }

    private static bool IsNullableProperty(Expression expression)
    {
        var type = expression.Type;
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }
}

/// <summary>
/// Extension methods for easier query builder usage
/// </summary>
public static class GenericQueryBuilderExtensions
{
    /// <summary>
    /// Create a query builder for the filter
    /// </summary>
    public static GenericQueryBuilder<TEntity, TFilter> CreateQueryBuilder<TEntity, TFilter>(
        this TFilter filter) where TFilter : BasePaginationFilter
    {
        return new GenericQueryBuilder<TEntity, TFilter>(filter);
    }
}
