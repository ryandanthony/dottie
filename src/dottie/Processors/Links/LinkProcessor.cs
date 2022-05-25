namespace dottie.Processors.Links;

public class LinkProcessor : IProcessor
{
    private readonly List<SymLink> _links;


    public LinkProcessor(List<SymLink> links)
    {
        _links = links;
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
            OnProgress(new ProcessProgress() { CurrentItem = $"Processing {link.LinkName}", TotalPercentComplete = result });
            await Execute(link);
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        OnProgress(new ProcessProgress() { CurrentItem = $"Done", TotalPercentComplete = 1 });
        return new Status() { Successful = true };
    }

    private async Task Execute(SymLink link)
    {
        link.Create();
    }

    private void OnProgress(ProcessProgress processProgress)
    {
        Progress?.Invoke(this, processProgress);
    }
}