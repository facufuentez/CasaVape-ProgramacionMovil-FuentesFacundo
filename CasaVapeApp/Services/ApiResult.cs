using System.Text.Json;

namespace CasaVapeApp.Services;

public sealed class ApiResult
{
    public bool Ok { get; init; }
    public string? Error { get; init; }

    public static ApiResult Success() => new() { Ok = true };
    public static ApiResult Failure(string? error) => new() { Ok = false, Error = error };
}

public sealed class ApiResult<T>
{
    public bool Ok { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }

    public static ApiResult<T> Success(T data) => new() { Ok = true, Data = data };
    public static ApiResult<T> Failure(string? error) => new() { Ok = false, Error = error };
}

public static class ApiError
{
    public static async Task<string?> ReadAsync(HttpResponseMessage response)
    {
        try
        {
            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
                {
                    return error.GetString();
                }

                if (root.TryGetProperty("errors", out var errors))
                {
                    if (errors.ValueKind == JsonValueKind.Array)
                    {
                        var messages = errors.EnumerateArray()
                            .Where(e => e.ValueKind == JsonValueKind.String)
                            .Select(e => e.GetString())
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToList();
                        if (messages.Count > 0)
                        {
                            return string.Join(" ", messages);
                        }
                    }
                    if (errors.ValueKind == JsonValueKind.String)
                    {
                        return errors.GetString();
                    }
                }

                if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                {
                    return title.GetString();
                }
            }
        }
        catch
        {
        }

        return null;
    }
}
