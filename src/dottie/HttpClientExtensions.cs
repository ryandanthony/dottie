namespace dottie;

public static class HttpClientExtensions
{
    public static async Task DownloadFileAsync(this HttpClient httpClient, string uri, string outputPath)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out _))
            throw new InvalidOperationException("URI is invalid.");

        await using var fileStream = File.Create(outputPath);
        var stream = await httpClient.GetStreamAsync(uri);
        await stream.CopyToAsync(fileStream);
    }
}