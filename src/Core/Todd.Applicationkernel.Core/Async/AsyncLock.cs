
namespace Todd.Applicationkernel.Core;

/// <summary>
/// 是一个异步编程设计的互斥锁实现，解决了传统lock关键字异步编程中的局限性，允许在锁定的代码块中使用 await关键字，
/// 同时保持了互斥锁的基本语义，通过内部类和using模式提供了清晰的锁获取和释放机制
/// </summary>
internal class AsyncLock
{
    private readonly SemaphoreSlim semaphore;
    public AsyncLock()
    {
        semaphore = new SemaphoreSlim(1);
    }
    /// <summary>
    /// 获取异步锁
    /// </summary>
    /// <returns></returns>
    public ValueTask<IDisposable> LockAsync()
    {
        Task await = semaphore.WaitAsync();
        if (await.IsCompletedSuccessfully)
        {
            return new(new LockReleaser(this));
        }
        else
        {
            return LockAsyncInternal(this, await);
            static async ValueTask<IDisposable> LockAsyncInternal(AsyncLock self, Task waitTask)
            {
                await waitTask.ConfigureAwait(false);
                return new LockReleaser(self);
            }
        }
    }
    private class LockReleaser : IDisposable
    {
        private AsyncLock target;
        public LockReleaser(AsyncLock target)
        {
            this.target = target;
        }
        public void Dispose()
        {
            if (target == null)
                return;
            //首先为null，然后为Release，所以即使Release抛出，我们也不再持有引用。
            AsyncLock realeas = target;
            target = null;
            try
            {
                realeas.semaphore.Release();
            }
            catch (Exception) { }
        }
    }
}