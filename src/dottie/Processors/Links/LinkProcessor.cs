using dottie.Config;
using Serilog;

namespace dottie.Processors.Links;

public class LinkProcessor : IProcessor
{
    private readonly ILogger _logger;
    private readonly List<SymLink> _links;

    public LinkProcessor(ILogger logger,
        string homeDirectory,
        string dottieDirectory,
        Dictionary<string, LinkSettings>? links)
    {
        _logger = logger;
        _links = links == null
            ? new List<SymLink>()
            : links.Select(p =>
                    new SymLink(_logger, homeDirectory,
                        dottieDirectory,
                        p.Key,
                        p.Value.Target))
                .ToList();
    }

    public event EventHandler<ProcessProgress>? Progress;
    public string Name => "Links";

    public async Task<Status> Run()
    {
        var total = _links.Count;
        var position = 0;
        foreach (var link in _links)
        {
            position++;
            var result = decimal.Divide(position, total);
            OnProgress(new ProcessProgress()
                { CurrentItem = $"Processing {link.LinkName}", TotalPercentComplete = result });
            await Execute(link);
        }

        OnProgress(new ProcessProgress() { CurrentItem = $"Done", TotalPercentComplete = 1 });
        return new Status() { Successful = true };
    }

    private async Task Execute(SymLink link)
    {
        await link.Create();
    }

    private void OnProgress(ProcessProgress processProgress)
    {
        Progress?.Invoke(this, processProgress);
    }
}