using Microsoft.AspNetCore.Mvc;

namespace ParkDesk.Api.Common;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex, "Business rule violation: {Message}", ex.Message);
            await WriteProblemAsync(context, ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "Ocurrio un error inesperado.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string detail)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhraseFor(statusCode),
            Detail = detail,
            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static string ReasonPhraseFor(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Solicitud invalida",
        StatusCodes.Status401Unauthorized => "No autorizado",
        StatusCodes.Status404NotFound => "No encontrado",
        StatusCodes.Status409Conflict => "Conflicto",
        StatusCodes.Status422UnprocessableEntity => "Regla de negocio violada",
        _ => "Error del servidor"
    };
}
