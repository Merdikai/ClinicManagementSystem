using System.Reflection;

namespace ClinicManagementSystem.Application.Common;

public static class DataShapingExtensions
{
    public static IEnumerable<IDictionary<string, object?>> ShapeData<T>(this IEnumerable<T> source, string? fields)
    {
        var list = new List<IDictionary<string, object?>>();
        if (source is null) return list;

        var propertyInfos = typeof(T).GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        var requiredProperties = new List<PropertyInfo>();

        if (string.IsNullOrWhiteSpace(fields))
        {
            requiredProperties.AddRange(propertyInfos);
        }
        else
        {
            var requestedFields = fields.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach (var field in requestedFields)
            {
                var prop = propertyInfos.FirstOrDefault(p => p.Name.Equals(field, StringComparison.OrdinalIgnoreCase));
                if (prop is not null)
                {
                    requiredProperties.Add(prop);
                }
            }
        }

        foreach (var item in source)
        {
            var expando = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in requiredProperties)
            {
                expando[prop.Name] = prop.GetValue(item);
            }
            list.Add(expando);
        }

        return list;
    }

    public static IDictionary<string, object?> ShapeData<T>(this T entity, string? fields)
    {
        var expando = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (entity is null) return expando;

        var propertyInfos = typeof(T).GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        var requiredProperties = new List<PropertyInfo>();

        if (string.IsNullOrWhiteSpace(fields))
        {
            requiredProperties.AddRange(propertyInfos);
        }
        else
        {
            var requestedFields = fields.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach (var field in requestedFields)
            {
                var prop = propertyInfos.FirstOrDefault(p => p.Name.Equals(field, StringComparison.OrdinalIgnoreCase));
                if (prop is not null)
                {
                    requiredProperties.Add(prop);
                }
            }
        }

        foreach (var prop in requiredProperties)
        {
            expando[prop.Name] = prop.GetValue(entity);
        }

        return expando;
    }
}
