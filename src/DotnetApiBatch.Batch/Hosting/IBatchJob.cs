namespace DotnetApiBatch.Batch.Hosting;

public interface IBatchJob
{
    Task RunAsync(CancellationToken cancellationToken);
}
