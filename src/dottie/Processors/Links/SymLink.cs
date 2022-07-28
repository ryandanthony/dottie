using Mono.Unix;
using Serilog;

namespace dottie.Processors.Links;

public class SymLink
{
    private readonly ILogger _logger;
    private readonly string _link;

    private readonly string _targetFullPath;
    private readonly string _linkFullPath;

    public SymLink(ILogger logger, string homeDirectory, string dottieDirectory, string link, string linkTarget)
    {
        _logger = logger;
        _link = link;
        _linkFullPath = link.Contains("~/")
            ? Path.Join(homeDirectory, link.Replace("~/", ""))
            : link;

        _targetFullPath = Path.Join(dottieDirectory, linkTarget);
    }

    public string LinkName => _link;

    public async Task Create()
    {
        //https://csharp.hotexamples.com/examples/Mono.Unix/UnixSymbolicLinkInfo/-/php-unixsymboliclinkinfo-class-examples.html

        var f = new UnixFileInfo(_targetFullPath);
        if (!f.Exists)
        {
            _logger.Warning("Link Target Missing: {_targetFullPath}", _targetFullPath);
        }

        f.CreateSymbolicLink(_linkFullPath);
    }
}