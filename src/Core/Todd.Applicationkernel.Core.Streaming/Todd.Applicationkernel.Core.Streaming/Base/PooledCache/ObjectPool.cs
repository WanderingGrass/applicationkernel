using System.Collections.Concurrent;
using Todd.Applicationkernel.Core.Streaming.Internal;
using Todd.Applicationkernel.Core.Streaming.Monitors;

namespace Todd.Applicationkernel.Core.Streaming.PooledCache
{
    public class ObjectPool<T> : IObjectPool<T> where T : PooledResource<T>
    {
        private const int DefaultPollCapacity = 1 << 10;//1kb

        private readonly ConcurrentStack<T> pool; //线性安全,无锁，栈特性最近释放的对象先复用。
        /// <summary>
        /// 对象工厂函数
        /// </summary>
        private readonly Func<T> factoryFunc;
        /// <summary>
        /// 统计
        /// </summary>
        private long totalObjects;

        private readonly IObjectPoolMonitor monitor;
        private readonly PeriodicAction periodicMonitoring;

        public ObjectPool(Func<T> factoryFunc, IObjectPoolMonitor monitor = null, TimeSpan? monitorWriteInterval = null)
        {
            if (factoryFunc == null)
            {
                throw new ArgumentNullException(nameof(factoryFunc));
            }

            this.factoryFunc = factoryFunc;
            pool = new ConcurrentStack<T>();

            // monitoring
            this.monitor = monitor;
            if (this.monitor != null && monitorWriteInterval.HasValue)
            {
                this.periodicMonitoring = new PeriodicAction(monitorWriteInterval.Value, this.ReportObjectPoolStatistics);
            }

            this.totalObjects = 0;
        }
        public virtual T Allocate()
        {
            T resource;
            //无法从池中弹出资源，则使用池外的factoryFunc创建资源
            if (!pool.TryPop(out resource))
            {
                resource=factoryFunc();
                Interlocked.Increment(ref totalObjects);
            }
            this.monitor?.TrackObjectAllocated();
            this.periodicMonitoring?.TryAction(DateTime.UtcNow);
            resource.Pool = this;
            return resource;
        }
        public virtual void Free(T resource)
        {
            this.monitor?.TrackObjectReleased();
            this.periodicMonitoring?.TryAction(DateTime.UtcNow);
            pool.Push(resource);
        }
        private void ReportObjectPoolStatistics()
        {
            var availableObject=this.pool.Count;
            long claimnedObjects = this.totalObjects - availableObject;
            this.monitor.Report(this.totalObjects,availableObject,claimnedObjects);
        }
    }
}
