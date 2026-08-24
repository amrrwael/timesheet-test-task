using FluentValidation;
using Timesheet.Api.Contracts;
using Timesheet.Api.Domain.Exceptions;

namespace Timesheet.Api.Middleware;

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
        catch (ValidationException ex)
        {
            var fields = ex.Errors
                .Select(e => new FieldError(ToCamelCase(e.PropertyName), e.ErrorMessage))
                .ToList();
            await WriteAsync(context, StatusCodes.Status400BadRequest, ErrorCodes.ValidationError,
                "Проверьте правильность заполнения полей.", fields);
        }
        catch (BusinessException ex)
        {
            await WriteAsync(context, ex.StatusCode, ex.Code, ex.Message);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // клиент сам прервал запрос — отвечать нечем
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Необработанное исключение при обработке {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await WriteAsync(context, StatusCodes.Status500InternalServerError,
                ErrorCodes.InternalError, "Внутренняя ошибка сервера.");
        }
    }

    private static async Task WriteAsync(HttpContext context, int statusCode, string code,
        string message, IReadOnlyList<FieldError>? errors = null)
    {
        if (context.Response.HasStarted) return;

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new ErrorResponse(code, message, errors));
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}