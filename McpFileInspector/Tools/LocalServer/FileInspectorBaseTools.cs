using McpFileInspector.Model;
using System.Text.RegularExpressions;

public class FileInspectorBaseTools
{
    internal static McpToolResponse<T> ProcessSuccessResponse<T>(T result)
    {
        if (result == null)
        {
            return new McpToolResponse<T>
            {
                Success = false,
                Error = new ResponseError
                {
                    Code = "INVALID_ARGUMENT",
                    Message = "Result cannot be null."
                }
            };
        }

        var response = new McpToolResponse<T>
        {
            Success = true,
            Results = result
        };

        return response;
    }

    internal static McpToolResponse<T> ProcessFailedResponse<T>(Exception ex)
    {
        var (code, message) = ex switch
        {
            ArgumentException =>
                ("INVALID_ARGUMENT", ex.Message),

            FileNotFoundException =>
                ("FILE_NOT_FOUND", "The requested file was not found."),

            UnauthorizedAccessException =>
                ("ACCESS_DENIED", "Access to the requested resource was denied."),

            RegexMatchTimeoutException =>
                ("REGEX_TIMEOUT", "The regular expression execution timed out."),

            IOException =>
                ("FILE_ACCESS_ERROR", "An error occurred while accessing the file."),

            _ =>
                ("INTERNAL_ERROR", "An unexpected error occurred.")
        };

        var response = new McpToolResponse<T>
        {
            Success = false,
            Error = new ResponseError
            {
                Code = code,
                Message = message
            }
        };

        return response;
    }
}
