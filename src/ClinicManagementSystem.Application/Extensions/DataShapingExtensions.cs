using System.Reflection;

namespace ClinicManagementSystem.Application.Extensions;

public static class DataShapingExtensions
{
    public static IEnumerable<IDictionary<string, object?>> ShapeData<T>(
        this IEnumerable<T> source, 
        string? fields)
    {
        ArgumentNullException.ThrowIfNull(source);

        var propertyInfoList = new List<PropertyInfo>();
        var allProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        if (string.IsNullOrWhiteSpace(fields))
        {
            propertyInfoList.AddRange(allProperties);
        }
        else
        {
            var requestedFields = fields.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach (var field in requestedFields)
            {
                var property = allProperties.FirstOrDefault(p => p.Name.Equals(field, StringComparison.OrdinalIgnoreCase));
                if (property != null)
                {
                    propertyInfoList.Add(property);
                }
            }

            if (propertyInfoList.Count == 0)
            {
                propertyInfoList.AddRange(allProperties);
            }
        }

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

        var shapedObject = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var allProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        if (string.IsNullOrWhiteSpace(fields))
        {
            foreach (var property in allProperties)
            {
                shapedObject[property.Name] = property.GetValue(entity);
            }
            return shapedObject;
        }

        var requestedFields = fields.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var field in requestedFields)
        {
            var property = allProperties.FirstOrDefault(p => p.Name.Equals(field, StringComparison.OrdinalIgnoreCase));
            if (property != null)
            {
                shapedObject[property.Name] = property.GetValue(entity);
            }
        }

        if (shapedObject.Count == 0)
        {
            foreach (var property in allProperties)
            {
                shapedObject[property.Name] = property.GetValue(entity);
            }
        }

        return shapedObject;
    }
}

