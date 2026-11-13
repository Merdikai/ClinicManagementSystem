using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;

namespace ClinicManagementSystem.Application.Extensions;

public static class DataShapingExtensions
{
    public static IQueryable<T> SelectShaped<T>(this IQueryable<T> query, string? fields)
        where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(fields))
            return query;

        var requestedFields = fields
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (requestedFields.Length == 0)
            return query;

        var entityType = typeof(T);
        var allWritableProps = entityType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetCustomAttribute<NotMappedAttribute>() is null)
            .Where(p => p.PropertyType == typeof(string) ||
                        p.PropertyType.IsValueType ||
                        Nullable.GetUnderlyingType(p.PropertyType) is not null)
            .ToArray();

        var selectedProps = new List<PropertyInfo>();
        foreach (var field in requestedFields)
        {
            var match = allWritableProps.FirstOrDefault(p => p.Name.Equals(field, StringComparison.OrdinalIgnoreCase));
            if (match is not null && !selectedProps.Contains(match))
            {
                selectedProps.Add(match);
            }
        }

        if (selectedProps.Count == 0)
            return query;

        // Always keep Id in projection when present so HATEOAS links & identity work safely
        var idProp = allWritableProps.FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
        if (idProp is not null && !selectedProps.Contains(idProp))
        {
            selectedProps.Insert(0, idProp);
        }

        var param = Expression.Parameter(entityType, "e");
        var bindings = selectedProps
            .Select(p => Expression.Bind(p, Expression.Property(param, p)))
            .Cast<MemberBinding>();

        var body = Expression.MemberInit(Expression.New(entityType), bindings);
        var selector = Expression.Lambda<Func<T, T>>(body, param);

        return query.Select(selector);
    }

    public static IEnumerable<IDictionary<string, object?>> ShapeData<T>(
        this IEnumerable<T> source,
        string? fields)
    {
        if (source is null) return [];

        var allProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var propertyInfoList = ResolveProperties(allProperties, fields);

        var result = new List<IDictionary<string, object?>>();
        foreach (var entity in source)
        {
            var shapedObject = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in propertyInfoList)
            {
                shapedObject[property.Name] = property.GetValue(entity);
            }
            result.Add(shapedObject);
        }

        return result;
    }

    public static IDictionary<string, object?> ShapeSingle<T>(
        this T entity,
        string? fields) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);

        var allProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var propertyInfoList = ResolveProperties(allProperties, fields);

        var shapedObject = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in propertyInfoList)
        {
            shapedObject[property.Name] = property.GetValue(entity);
        }

        return shapedObject;
    }

    private static List<PropertyInfo> ResolveProperties(PropertyInfo[] allProperties, string? fields)
    {
        var list = new List<PropertyInfo>();
        if (string.IsNullOrWhiteSpace(fields))
        {
            list.AddRange(allProperties);
            return list;
        }

        var requestedFields = fields.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var field in requestedFields)
        {
            var property = allProperties.FirstOrDefault(p => p.Name.Equals(field, StringComparison.OrdinalIgnoreCase));
            if (property is not null && !list.Contains(property))
            {
                list.Add(property);
            }
        }

        if (list.Count == 0)
        {
            list.AddRange(allProperties);
        }

        return list;
    }
}
