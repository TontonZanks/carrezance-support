namespace Carrezance.Support.App.Models;

public sealed class ActionResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? Details { get; init; }

    public static ActionResult Ok(string message, string? details = null) => new()
    {
        Success = true,
        Message = message,
        Details = details
    };

    public static ActionResult Fail(string message, string? details = null) => new()
    {
        Success = false,
        Message = message,
        Details = details
    };
}
