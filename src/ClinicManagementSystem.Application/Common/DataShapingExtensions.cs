using ExtensionsDataShaping = ClinicManagementSystem.Application.Extensions.DataShapingExtensions;

namespace ClinicManagementSystem.Application.Common;

public static class DataShapingExtensions
{
    public static IQueryable<T> SelectShaped<T>(this IQueryable<T> query, string? fields)
        where T : class, new()
        => ExtensionsDataShaping.SelectShaped(query, fields);

    public static IEnumerable<IDictionary<string, object?>> ShapeData<T>(this IEnumerable<T> source, string? fields)
        => ExtensionsDataShaping.ShapeData(source, fields);

    public static IDictionary<string, object?> ShapeData<T>(this T entity, string? fields)
        where T : class
        => ExtensionsDataShaping.ShapeSingle(entity, fields);
}
