using System.Text.RegularExpressions;

namespace McpFileInspector.Model; // o McpFileInspector.Common

public static class McpResponse
{
    public static McpToolResponse<T> Success<T>(T result)
    {
        if (result == null)
        {
            return Fail<T>("INVALID_ARGUMENT", "Result cannot be null.");
        }

        return new McpToolResponse<T>
        {
            Success = true,
            Results = result
        };
    }

    public static McpToolResponse<T> Fail<T>(Exception ex)
    {
        var (code, message) = ex switch
        {
            ArgumentException => ("INVALID_ARGUMENT", ex.Message),
            FileNotFoundException => ("FILE_NOT_FOUND", "The requested file was not found."),
            UnauthorizedAccessException => ("ACCESS_DENIED", "Access to the requested resource was denied."),
            RegexMatchTimeoutException => ("REGEX_TIMEOUT", "The regular expression execution timed out."),
            IOException => ("FILE_ACCESS_ERROR", "An error occurred while accessing the file."),
            _ => ("INTERNAL_ERROR", "An unexpected error occurred.")
        };

        return Fail<T>(code, message);
    }

    public static McpToolResponse<T> Fail<T>(string code, string message) =>
        new()
        {
            Success = false,
            Error = new ResponseError { Code = code, Message = message }
        };
}