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