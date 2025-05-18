// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Todd.Applicationkernel.Core.Networking.Shared
{
    /// <summary>
    /// 管理多个I/O队列调度器 实现简单的轮询负载均衡策略。
    /// </summary>
    internal class SocketSchedulers
    {
        private static readonly PipeScheduler[] ThreadPoolSchedulerArray = new PipeScheduler[] { PipeScheduler.ThreadPool };
        private  readonly int _numSchedulers; //调度器数量
        private readonly PipeScheduler[] _schedulers;//存储调度器的数组
        private int nextScheduler;//轮询分配的计数器

        public SocketSchedulers(IOptions<SocketConnectionOptions> options)
        {
            var o = options.Value;
            if (o.IOQueueCount > 0)
            {
                _numSchedulers = o.IOQueueCount;
                _schedulers = new IOQueue[_numSchedulers];

                for (var i = 0; i < _numSchedulers; i++)
                {
                    _schedulers[i] = new IOQueue();
                }
            }
            else
            {
                _numSchedulers = ThreadPoolSchedulerArray.Length;
                _schedulers = ThreadPoolSchedulerArray;
            }
        }

        public PipeScheduler GetScheduler() => _schedulers[++nextScheduler % _numSchedulers];
    }
}
