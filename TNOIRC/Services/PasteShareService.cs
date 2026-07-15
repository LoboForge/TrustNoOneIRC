using System.Net.Http.Headers;
using System.Text;

namespace LoboForge.TNOIRC.Services;

public sealed class PasteShareService
{
    public const string PasteRsEndpoint = "https://paste.rs/";
    public const string ZeroXZeroEndpoint = "https://0x0.st";
    public const int MaxTextLength = 512 * 1024;
    public const long MaxImageBytes = 8 * 1024 * 1024;

    public async Task<PasteShareResult> UploadTextAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return PasteShareResult.Fail("Paste text is empty.");

        if (text.Length > MaxTextLength)
            return PasteShareResult.Fail($"Paste exceeds {MaxTextLength / 1024} KB limit.");

        if (!await TorHttpClientFactory.IsTorProxyAvailableAsync())
            return PasteShareResult.Fail("Tor SOCKS proxy is not available. Uploads are blocked to avoid IP leaks.");

        using var client = TorHttpClientFactory.Create();
        using var content = new StringContent(text, Encoding.UTF8, "text/plain");
        using var response = await client.PostAsync(PasteRsEndpoint, content, cancellationToken);
        var body = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();

        if (!response.IsSuccessStatusCode)
            return PasteShareResult.Fail($"Paste upload failed ({(int)response.StatusCode}): {body}");

        var url = body.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? body.Split('\n', '\r')[0].Trim()
            : $"{PasteRsEndpoint.TrimEnd('/')}/{body.Trim('/')}";

        return PasteShareResult.Ok(url, "text");
    }

    public async Task<PasteShareResult> UploadImageAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (stream.Length > MaxImageBytes)
            return PasteShareResult.Fail($"Image exceeds {MaxImageBytes / (1024 * 1024)} MB limit.");

        if (!await TorHttpClientFactory.IsTorProxyAvailableAsync())
            return PasteShareResult.Fail("Tor SOCKS proxy is not available. Uploads are blocked to avoid IP leaks.");

        using var client = TorHttpClientFactory.Create();
        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        form.Add(fileContent, "file", fileName);

        using var response = await client.PostAsync(ZeroXZeroEndpoint, form, cancellationToken);
        var url = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();

        if (!response.IsSuccessStatusCode || !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return PasteShareResult.Fail($"Image upload failed ({(int)response.StatusCode}): {url}");

        return PasteShareResult.Ok(url.Split('\n', '\r')[0].Trim(), "image");
    }
}

public sealed record PasteShareResult(bool Success, string? Url, string Kind, string? Error)
{
    public static PasteShareResult Ok(string url, string kind) => new(true, url, kind, null);
    public static PasteShareResult Fail(string error) => new(false, null, "", error);
}
