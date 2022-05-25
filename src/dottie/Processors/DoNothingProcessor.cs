namespace dottie.Processors;

public class DoNothingProcessor : IProcessor
{
    private readonly int _total;
    private readonly TimeSpan _delay;

    public DoNothingProcessor(int total = 10, int delayMs = 1000)
    {
        _total = total;
        _delay = TimeSpan.FromMilliseconds(delayMs);
    }
    public event EventHandler<ProcessProgress>? Progress;

    public string Name => "Nothing";

    public async Task<Status> Run()
    {
        for (int i = 0; i <= _total; i++)
        {
            var result = decimal.Divide(i, _total);
            OnProgress(new ProcessProgress() { CurrentItem = $"Processing {i}", TotalPercentComplete = result});
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
        OnProgress(new ProcessProgress() { CurrentItem = $"Done", TotalPercentComplete = 1});

        return new Status() { Successful = true };
    }

    private void OnProgress(ProcessProgress processProgress)
    {
        Progress?.Invoke(this, processProgress);
    }
}