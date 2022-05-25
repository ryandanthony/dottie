namespace dottie.Config;

class Configuration
{
    public Dictionary<string, LinkSettings> Links { get; set; }
    public List<AptVersion> AptGet { get; set; }
}