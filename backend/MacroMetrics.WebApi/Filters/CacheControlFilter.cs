namespace MacroMetrics.WebApi.Filters;

/// <summary>
/// Endpoint filter that writes <c>Cache-Control: public, max-age=3600</c> on every
/// HTTP response produced by the route group it is attached to, allowing CDNs and
/// browsers to cache responses for up to one hour (US-B16).
/// </summary>
public sealed class CacheControlFilter : IEndpointFilter
{
    /// <summary>The Cache-Control directive emitted on all /api/metrics responses.</summary>
    public const string HeaderValue = "public, max-age=3600";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        // Set the header before the handler executes so it is present regardless of
        // whether the route returns 200, 404, or any other status code.
        context.HttpContext.Response.Headers.CacheControl = HeaderValue;

        return await next(context);
    }
}
