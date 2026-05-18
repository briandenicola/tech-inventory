namespace TechInventory.Application.Common.Results;

public sealed record Error
{
    public Error(string code, string message)
    {
        Code = string.IsNullOrWhiteSpace(code)
            ? throw new ArgumentException("Error code is required.", nameof(code))
            : code.Trim();

        Message = string.IsNullOrWhiteSpace(message)
            ? throw new ArgumentException("Error message is required.", nameof(message))
            : message.Trim();
    }

    public string Code { get; }

    public string Message { get; }
}
