// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Todd.Applicationkernel.Core.Streaming
{
    [Serializable]
    public readonly struct QueueId : IEquatable<QueueId>, IComparable<QueueId>, ISpanFormattable
    {
        private readonly string queueNamePrefix; //队列名称前缀
        private readonly uint queueId;          //队列ID
        private readonly uint uniformHashCache;//统一哈希缓存

        private QueueId(string queuePrefix, uint id, uint hash)
        {
            queueNamePrefix = queuePrefix ?? throw new ArgumentNullException(nameof(queuePrefix));
            queueId = id;
            uniformHashCache = hash;
        }
        public static QueueId GetQueueId(string queueName, uint queueId, uint hash) => new(queueName, queueId, hash);

        public string GetStringNamePrefix() => queueNamePrefix;
        public uint GetNumericId() => queueId;
        public uint GetUniformHashCode() => uniformHashCache;
        public bool IsDefault => queueNamePrefix is null;
        public int CompareTo(QueueId other)
        {
            if (queueId != other.queueId)
                return queueId.CompareTo(other.queueId);

            var cmp = string.CompareOrdinal(queueNamePrefix, other.queueNamePrefix);
            if (cmp != 0) return cmp;

            return uniformHashCache.CompareTo(other.uniformHashCache);
        }
        /// <inheritdoc/>
        public bool Equals(QueueId other) => queueId == other.queueId && uniformHashCache == other.uniformHashCache && queueNamePrefix == other.queueNamePrefix;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is QueueId queueId && Equals(queueId);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(queueId, uniformHashCache, queueNamePrefix);
        public static bool operator ==(QueueId left, QueueId right) => left.Equals(right);
        public static bool operator !=(QueueId left, QueueId right)=>!(left == right);

        public override string ToString() => $"{this}";
        string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString();

        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            var len = queueNamePrefix.AsSpan().ToLowerInvariant(destination);
            if (len >= 0 && destination[len..].TryWrite($"-{queueId}", out var len2))
            {
                len += len2;

                if (format.Length == 1 && format[0] == 'H')
                {
                    if (!destination[len..].TryWrite($"-0x{uniformHashCache:X8}", out len2))
                    {
                        charsWritten = 0;
                        return false;
                    }
                    len += len2;
                }

                charsWritten = len;
                return true;
            }

            charsWritten = 0;
            return false;
        }
        public string ToStringWithHashCode() => $"{this:H}";

    }
}
