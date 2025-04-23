// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Todd.Applicationkernel.Core.Streaming.Monitors
{
    /// <summary>
    /// 监控跟踪对象池相关指标
    /// </summary>
    public interface IObjectPoolMonitor
    {
        /// <summary>
        /// 每次分配对象时调用
        /// </summary>
        void TrackObjectAllocated();
       /// <summary>
       /// 每次将对象释放回池中调用
       /// </summary>

        void TrackObjectReleased();
        /// <summary>
        /// 调用报告对象池状态
        /// </summary>
        /// <param name="totalObjects">对象池总大小</param>
        /// <param name="availableObjects">池中可用于分配的计数</param>
        /// <param name="claimedObjects">计数已声明的对象</param>

        void Report(long totalObjects, long availableObjects, long claimedObjects);
    }
}
