// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Todd.Applicationkernel.Core.Abstractions;

namespace Todd.Applicationkernel.Core.Streaming
{
    internal readonly struct HashRing
    {
        private readonly QueueId[] _ring;

        public HashRing(QueueId[] ring)
        {
            Array.Sort(ring, (x, y) => x.GetUniformHashCode().CompareTo(y.GetUniformHashCode()));
            _ring = ring;
        }

        public QueueId[] GetAllRingMembers() => _ring;

        public QueueId CalculateResponsible(uint uniformHashCode)
        {
            // use clockwise 
            var index = _ring.AsSpan().BinarySearch(new Searcher(uniformHashCode));
            if (index < 0)
            {
                index = ~index;
                // if not found in traversal, then first element should be returned (we are on a ring)
                if (index == _ring.Length) index = 0;
            }
            return _ring[index];
        }

        private readonly struct Searcher : IComparable<QueueId>
        {
            private readonly uint _value;
            public Searcher(uint value) => _value = value;
            public int CompareTo(QueueId other) => _value.CompareTo(other.GetUniformHashCode());
        }

        public override string ToString()
            => $"All QueueIds:{Environment.NewLine}{(Utils.EnumerableToString(_ring, elem => $"{elem}/x{elem.GetUniformHashCode():X8}", Environment.NewLine, false))}";
    }
}
