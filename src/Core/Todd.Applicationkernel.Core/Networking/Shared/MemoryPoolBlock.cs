// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Todd.Applicationkernel.Core.Networking.Shared
{
    /// <summary>
    /// 字节缓冲内存池使用的块跟踪对象。slab 是一个大的分配，它被划分为更小的块。这然后将各个块视为独立的数组段。
    /// </summary>
    internal sealed class MemoryPoolBlock : IMemoryOwner<byte>
    {
        private readonly int _offset;
        private readonly int _length;
        internal MemoryPoolBlock(SlabMemoryPool pool, MemoryPoolSlab slab,int offset,int length)
        {
            _offset = offset;
            _length = length;

            Pool = pool;
            Slab = slab;

            Memory = MemoryMarshal.CreateFromPinnedArray(slab.Array, _offset, _length);
        }
        public SlabMemoryPool Pool { get; }
        public MemoryPoolSlab Slab { get; }

        public Memory<byte> Memory { get; }
        ~MemoryPoolBlock()
        {
            Pool.RefreshBlock(Slab, _offset, _length);
        }
        public void Dispose()
        {
            Pool.Return(this);
        }
        public void Lease()
        {
        }

    }
}
