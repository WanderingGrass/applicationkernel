
namespace Todd.Applicationkernel.Core.Abstractions;

public static class PublicTaskExtensions
{
    private static readonly Action<Task> IgnoreTaskContinuation = t => { _ = t.Exception; };

    public static void Ignore(this Task task)
    {
        if (task.IsCompleted)
        {
            _ = task.Exception;
        }
        else
        {
            task.ContinueWith(
                IgnoreTaskContinuation,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }
}