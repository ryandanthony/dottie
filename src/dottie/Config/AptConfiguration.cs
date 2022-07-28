namespace dottie.Config;

public  class AptConfiguration
{
    public List<AptPackage> PreReqs { get; set; }
    public List<AptSource> Sources { get; set; }
    public List<AptPackage> Packages { get; set; }
}