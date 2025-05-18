
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Todd.Applicationkernel.Core.Abstractions;

namespace Todd.Applicationkernel.Core;

/// <summary>
/// 可重入的异步串行执行器（同时可以处理多个请求，需要保证顺序性而不交错）
/// 解决问题：：如何在不阻塞线程的情况下确保操作按顺序执行
/// 场景：如集群拓扑发生变化，节点加入和离开。需要重新分配队列给不同的节点，负责处理重新分配和通知，确保队列重新平衡
/// </summary>
/// <typeparam name="TResult"></typeparam>
public class AsyncSerialExecutor<TResult>
{
    private readonly ConcurrentQueue<Tuple<TaskCompletionSource<TResult>, Func<Task<TResult>>>> actions = new ConcurrentQueue<Tuple<TaskCompletionSource<TResult>, Func<Task<TResult>>>>();
    private readonly InterlockedExchangeLock locker = new InterlockedExchangeLock();
    public class InterlockedExchangeLock
    {
        private const int Locked = 1;
        private const int Unlocked = 0;
        private int state = Unlocked;

        public bool TryGetLock()
        {
            return Interlocked.Exchange(ref state, Locked) != Unlocked;
        }

        public void ReleaseLock()
        {
            Interlocked.Exchange(ref state, Unlocked);
        }
    }
    public Task<TResult> AddNext(Func<Task<TResult>> func)
    {
        var resolver = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        actions.Enqueue(new Tuple<TaskCompletionSource<TResult>, Func<Task<TResult>>>(resolver, func));
        ExecuteNext().Ignore();
        return resolver.Task;
    }
    /// <summary>
    /// 这里两层循环目的：
    /// 1：并发安全：虽然ConcurrentQueue是线程安全，但是多个操作之间不是原子性，检查队列是否为空和队列中取出数据是两个独立操作
    /// 2：内循环中处理的是异步操作，异步操作期间，需要释放锁允许其他线程工作，外循环是确保了异步操作完成后，可重新获取锁并继续处理队列
    /// 3：高效处理新添加的项目
    /// 4：防止死锁，没有外循环的话，一个异步操作完成后，锁释放，但是队列中还有项目，那么这个项目永远不能被处理。
    /// </summary>
    /// <returns></returns>
    private async Task ExecuteNext()
    {
        //外循坏：
        //每次迭代尝试获取锁，失败则退出
        //处理队列项目时有新项目被添加，确保在释放锁再次尝试处理
        while (!actions.IsEmpty)
        {
            bool gotLock = false;
            try
            {
                if (!(gotLock = locker.TryGetLock()))
                {
                    return;
                }
                //内循环：
                // 1:批量处理：持有锁情况，尽可能的多的处理队列中的项目
                // 2:高效利用锁，避免频繁获取和释放锁，提高性能。

                while (!actions.IsEmpty)
                {
                    Tuple<TaskCompletionSource<TResult>, Func<Task<TResult>>> actionTuple;
                    if (actions.TryDequeue(out actionTuple))
                    {
                        try
                        {
                            TResult result = await actionTuple.Item2();
                            actionTuple.Item1.TrySetResult(result);
                        }
                        catch (Exception exc)
                        {
                            actionTuple.Item1.TrySetException(exc);
                        }
                    }
                }
            }
            finally
            {
                if (gotLock)
                {
                    locker.ReleaseLock();
                }
            }
        }
    }
}
public class AsyncSerialExecutor
{
    private readonly AsyncSerialExecutor<bool> executor = new AsyncSerialExecutor<bool>();

    public Task AddNext(Func<Task> func)
    {
        return this.executor.AddNext(() => Wrap(func));
    }
    private static async Task<bool> Wrap(Func<Task> func)
    {
        await func();
        return true;
    }
}