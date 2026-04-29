using System.Linq.Expressions;

namespace MindWeaver.Extensions;

/// <summary>
/// Extension methods for <see cref="IQueryable{T}"/> that translate a plain
/// <c>Dictionary&lt;string, object&gt;</c> (produced by the dynamic filter UI)
/// into a fully composed EF Core LINQ <c>.Where()</c> expression at runtime.
///
/// Supported filter types and their SQL equivalents:
///   • string   → WHERE [col] LIKE '%value%'  (via EF's string.Contains translation)
///   • DateTime → WHERE CAST([col] AS date) = CAST(@value AS date)
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Dynamically builds and applies a <c>.Where()</c> predicate for every
    /// non-null entry in <paramref name="filters"/>.
    ///
    /// Each dictionary key must match (case-insensitive) a public property name
    /// on <typeparamref name="T"/>. Unknown keys are silently skipped so the
    /// caller never needs to sanitise the dictionary first.
    /// </summary>
    /// <param name="query">The base <see cref="IQueryable{T}"/> to filter.</param>
    /// <param name="filters">
    /// Map of PropertyName → filter value.  Values may be <c>null</c>, <c>""</c>,
    /// or <see cref="DateTime.MinValue"/> to indicate "no filter for this field".
    /// </param>
    public static IQueryable<T> ApplyDynamicFilters<T>(
        this IQueryable<T> query,
        Dictionary<string, object?> filters)
    {
        if (filters is null || filters.Count == 0)
            return query;

        // Parameter expression: the "x" in  x => x.Title.Contains(...)
        var parameter = Expression.Parameter(typeof(T), "x");

        Expression? combinedPredicate = null;

        foreach (var (propertyName, rawValue) in filters)
        {
            // Skip empty / null values — user left the control blank
            if (rawValue is null) continue;
            if (rawValue is string s && string.IsNullOrWhiteSpace(s)) continue;
            if (rawValue is DateTime dt && dt == DateTime.MinValue) continue;

            // Resolve property; skip silently if it doesn't exist on T
            var propertyInfo = typeof(T).GetProperty(
                propertyName,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase);

            if (propertyInfo is null) continue;

            // x.PropertyName
            var memberAccess = Expression.Property(parameter, propertyInfo);

            Expression? clause = null;

            // ── string → Contains ──────────────────────────────────────────────
            if (propertyInfo.PropertyType == typeof(string))
            {
                var containsMethod = typeof(string)
                    .GetMethod(nameof(string.Contains), new[] { typeof(string) })!;

                var constantValue = Expression.Constant(rawValue.ToString(), typeof(string));
                clause = Expression.Call(memberAccess, containsMethod, constantValue);
            }

            // ── DateTime → calendar-day equality ──────────────────────────────
            else if (propertyInfo.PropertyType == typeof(DateTime) ||
                     propertyInfo.PropertyType == typeof(DateTime?))
            {
                var filterDate = ((DateTime)rawValue).Date;

                // x.CreatedAt.Date == filterDate
                var dateProperty = Expression.Property(memberAccess, nameof(DateTime.Date));
                var constantDate = Expression.Constant(filterDate, typeof(DateTime));
                clause = Expression.Equal(dateProperty, constantDate);
            }

            if (clause is null) continue;

            // AND all clauses together
            combinedPredicate = combinedPredicate is null
                ? clause
                : Expression.AndAlso(combinedPredicate, clause);
        }

        if (combinedPredicate is null)
            return query;

        var lambda = Expression.Lambda<Func<T, bool>>(combinedPredicate, parameter);
        return query.Where(lambda);
    }
}
