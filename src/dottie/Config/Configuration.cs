namespace dottie.Config;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class Configuration
{
    public Dictionary<string, LinkSettings> Links { get; set; }
    public AptConfiguration Apt { get; set; }
}