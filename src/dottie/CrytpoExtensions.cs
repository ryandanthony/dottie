using System.Security.Cryptography;
using System.Text;

namespace dottie;

public static class CryptoExtensions
{
    public static string ComputeSha256Hash(this string rawData)
    {
        using SHA256 sha256Hash = SHA256.Create();
        var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        var builder = new StringBuilder();
        foreach (var t in bytes)
        {
            builder.Append(t.ToString("x2"));
        }
        return builder.ToString();
    }
}

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