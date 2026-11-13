using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace ClinicManagementSystem.API.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ETagFilterAttribute : Attribute, IAsyncResultFilter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (!HttpMethods.IsGet(context.HttpContext.Request.Method) &&
            !HttpMethods.IsHead(context.HttpContext.Request.Method))
        {
            await next();
            return;
        }

        if (context.Result is ObjectResult { Value: not null } objectResult &&
            (objectResult.StatusCode is null or StatusCodes.Status200OK))
        {
            var payloadJson = JsonSerializer.Serialize(objectResult.Value, SerializerOptions);
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson));
            var etag = $"\"{Convert.ToHexString(hash)[..24]}\"";

            var responseHeaders = context.HttpContext.Response.Headers;
            responseHeaders[HeaderNames.ETag] = etag;
            responseHeaders[HeaderNames.CacheControl] = "private, must-revalidate";

            var ifNoneMatch = context.HttpContext.Request.Headers[HeaderNames.IfNoneMatch];
            if (!StringValues.IsNullOrEmpty(ifNoneMatch))
            {
                var incomingTags = ifNoneMatch.ToString()
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (incomingTags.Any(t => string.Equals(t, "*", StringComparison.Ordinal) ||
                                          string.Equals(t, etag, StringComparison.Ordinal) ||
                                          string.Equals(t.TrimStart('W', '/'), etag, StringComparison.Ordinal)))
                {
                    context.Result = new StatusCodeResult(StatusCodes.Status304NotModified);
                    await next();
                    return;
                }
            }
        }

        await next();
    }
}

