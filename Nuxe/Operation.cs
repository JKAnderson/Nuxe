namespace Nuxe;

internal record OperationProgress(int Max, int Current, string Message);

internal abstract class Operation
{
    protected IProgress<OperationProgress> Progress { get; private set; }
    protected CancellationToken CancellationToken { get; private set; }

    public void Run(IProgress<OperationProgress> progress, CancellationToken cancellationToken)
    {
        Progress = progress;
        CancellationToken = cancellationToken;
        Run();
    }

    protected abstract void Run();
}
