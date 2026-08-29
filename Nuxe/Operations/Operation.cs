namespace Nuxe;

internal record OperationProgress(double Value, string Message);

internal abstract class Operation
{
    public required IProgress<OperationProgress> Progress { get; init; }
    public required CancellationToken CancellationToken { get; init; }

    public abstract void Run();
}
