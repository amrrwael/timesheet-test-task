using Microsoft.AspNetCore.Http;
using Timesheet.Api.Contracts;

namespace Timesheet.Api.Domain.Exceptions;

public abstract class BusinessException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }

    protected BusinessException(string code, string message, int statusCode)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}

/// <summary>404 — ресурс не найден.</summary>
public sealed class NotFoundException : BusinessException
{
    public NotFoundException(string message)
        : base(ErrorCodes.NotFound, message, StatusCodes.Status404NotFound) { }
}

/// <summary>409 — конфликт состояния (закрытый период, версионность и т.п.).</summary>
public sealed class ConflictException : BusinessException
{
    public ConflictException(string code, string message)
        : base(code, message, StatusCodes.Status409Conflict) { }
}