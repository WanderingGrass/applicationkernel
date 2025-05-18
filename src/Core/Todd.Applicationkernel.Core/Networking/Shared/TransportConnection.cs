// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Todd.Applicationkernel.Core.Networking.Shared
{
    /// <summary>
    /// 传输连接
    /// ConnectionContext 封装有关连接的信息。
    /// </summary>
    internal abstract partial class TransportConnection: ConnectionContext
    {
        private IDictionary<object, object> _items;
        private string _connectionId;

        public TransportConnection()
        {
            FastReset();
        }
        /// <summary>
        /// 本地地址
        /// </summary>
        public override EndPoint LocalEndPoint { get; set; }
        /// <summary>
        /// 远程地址
        /// </summary>
        public override EndPoint RemoteEndPoint { get; set; }

        public override string ConnectionId
        {
            get
            {
                if (_connectionId == null)
                {
                    _connectionId = CorrelationIdGenerator.GetNextId();
                }

                return _connectionId;
            }
            set
            {
                _connectionId = value;
            }
        }

        public override IFeatureCollection Features => this;

        public virtual MemoryPool<byte> MemoryPool { get; }

        public override IDuplexPipe Transport { get; set; }

        public IDuplexPipe Application { get; set; }

        public override IDictionary<object, object> Items
        {
            get
            {
                //  Lazily allocate connection metadata
                return _items ?? (_items = new ConnectionItems());
            }
            set
            {
                _items = value;
            }
        }

        public override CancellationToken ConnectionClosed { get; set; }

        /// <summary>
        /// 所有派生类型都应该重写此方法应该重写
        /// </summary>
        /// <param name="abortReason"></param>
        public override void Abort(ConnectionAbortedException abortReason)
        {
            Application.Input.CancelPendingRead();
        }

    }
}
