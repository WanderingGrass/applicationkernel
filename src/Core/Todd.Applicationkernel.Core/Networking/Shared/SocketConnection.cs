// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;


namespace Todd.Applicationkernel.Core.Networking.Shared
{
    internal sealed class SocketConnection:TransportConnection
    {
        private static readonly int MinAllocBufferSize = SlabMemoryPool.BlockSize / 2;

        private readonly Socket _socket;

        private readonly ISocketsTrace _trace;

        //private readonly SocketReceiver _receiver;

        //private readonly SocketSender _sender;
    }
}
