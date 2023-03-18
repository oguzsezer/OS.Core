using Microsoft.AspNetCore.Http;

namespace OS.Core.ResponseWrappers.Models;

public sealed class Error
{
    private Error() { }
    public ErrorReason Reason { get; private init; }
    public string? ErrorMessage { get; private init; }

    public enum ErrorReason
    {
        BusinessRule = StatusCodes.Status400BadRequest,
        Forbidden = StatusCodes.Status403Forbidden,
        NotFound = StatusCodes.Status404NotFound,
        Conflict = StatusCodes.Status409Conflict
    }

    internal static Error BusinessRule(string? errorMessage = null)
    {
        return new Error
        {
            ErrorMessage = errorMessage,
            Reason = ErrorReason.BusinessRule
        };
    }

    internal static Error Forbidden(string? errorMessage = null)
    {
        return new Error
        {
            ErrorMessage = errorMessage,
            Reason = ErrorReason.Forbidden
        };
    }

    internal static Error NotFound(string? errorMessage = null)
    {
        return new Error
        {
            ErrorMessage = errorMessage,
            Reason = ErrorReason.NotFound
        };
    }

    internal static Error Conflict(string? errorMessage = null)
    {
        return new Error
        {
            ErrorMessage = errorMessage,
            Reason = ErrorReason.Conflict
        };
    }
}