using Mono.Unix;

namespace dottie.Processors.Links;

public class SymLink
{
    private readonly string _link;
    private readonly bool _force;


    private readonly string _targetFullPath;
    private readonly string _linkFullPath;

    public SymLink(string homeDirectory, string dottieDirectory, string link, string linkTarget, bool force = false)
    {
        _link = link;
        _force = force;
        if (link.Contains("~/"))
        {
            var partialLink = link.Replace("~/", "");
            _linkFullPath = Path.Join(homeDirectory, partialLink);
        }
        else
        {
            _linkFullPath = link;
        }
         
        _targetFullPath = Path.Join(dottieDirectory, linkTarget);
    }

    public string LinkName => _link;

    public bool Exists()
    {
        var symlink = new UnixSymbolicLinkInfo(_link);
        return symlink.Exists;
    }
    
    public bool IsSymLink()
    {
        var symlink = new UnixSymbolicLinkInfo(_link);
        return symlink.IsSymbolicLink;
    }
    
    public async Task Create()
    {
        if (_force)
        {
            
            //https://csharp.hotexamples.com/examples/Mono.Unix/UnixSymbolicLinkInfo/-/php-unixsymboliclinkinfo-class-examples.html
            await Task.Delay(1200);
        }
    }
}