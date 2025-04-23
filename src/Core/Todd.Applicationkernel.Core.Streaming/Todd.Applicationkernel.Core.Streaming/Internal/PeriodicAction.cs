// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Todd.Applicationkernel.Core.Streaming.Internal
{
    internal class PeriodicAction
    {
        private readonly Action action;
        private readonly TimeSpan period;
        private DateTime nextUtc;

        public PeriodicAction(TimeSpan period, Action action, DateTime? start = null)
        {
            this.period = period;
            this.nextUtc = start ?? DateTime.UtcNow + period;
            this.action = action;
        }

        public bool TryAction(DateTime nowUtc)
        {
            if (nowUtc < this.nextUtc) return false;
            this.nextUtc = nowUtc + this.period;
            this.action();
            return true;
        }
    }
}
