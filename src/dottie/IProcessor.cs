


namespace dottie;

public interface IProcessor
{
    event EventHandler<ProcessProgress> Progress;
    string Name { get; }
    Task<Status> Run();

}

public class ProcessProgress
{
    public string CurrentItem { get; set; }
    public decimal TotalPercentComplete { get; set; }
}

public class Status
{
    public bool Successful { get; set; }
}