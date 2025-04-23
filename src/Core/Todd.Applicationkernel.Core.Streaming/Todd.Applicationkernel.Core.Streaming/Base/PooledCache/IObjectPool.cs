// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Todd.Applicationkernel.Core.Streaming.PooledCache
{
    public interface IObjectPool<T> where T : IDisposable
    {
        /// <summary>
        /// 分配资源
        /// </summary>
        /// <returns></returns>
        T Allocate();
        /// <summary>
        /// 将资源返回池中
        /// </summary>
        /// <param name="resource"></param>

        void Free(T resource);
    }
    /// <summary>
    /// 通过允许池对象跟踪它们来自的池并在处置时返回池来支持池对象的实用程序类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PooledResource<T> : IDisposable where T : PooledResource<T>, IDisposable
    {
        private IObjectPool<T> pool;

        public IObjectPool<T> Pool { set { pool = value; } }
        /// <summary>
        /// 如果要在固定大小的对象池中使用此对象，则此调用应为用将对象返回池的清除实现覆盖。
        /// </summary>
        public virtual void SignalPurge()
        {
            Dispose();
        }
        /// <summary>
        /// 将项目返回池中
        /// </summary>
        public void Dispose()
        {
            IObjectPool<T> localPoll = Interlocked.Exchange(ref pool, null);
            if(localPoll != null)
            {
                OnResetState();
                localPoll.Free((T)this);
            }
        }
        /// <summary>
        /// 通知对象它已被清除，这样它就可以将自己重置为新分配对象的状态。
        /// </summary>
        public virtual void OnResetState()
        {

        }
    }
}
