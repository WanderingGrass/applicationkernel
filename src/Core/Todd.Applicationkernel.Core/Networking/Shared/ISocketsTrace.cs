// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Todd.Applicationkernel.Core.Networking.Shared
{
    internal interface ISocketsTrace : ILogger
    {
        void ConnectionReadFin(string connectionId);

        void ConnectionWriteFin(string connectionId, string reason);

        void ConnectionError(string connectionId, Exception ex);

        void ConnectionReset(string connectionId);

        void ConnectionPause(string connectionId);

        void ConnectionResume(string connectionId);
    }
}
