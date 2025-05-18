// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Todd.Applicationkernel.Core.Networking.Shared
{
    /// <summary>
    /// 用于分配和分发可重复使用的内存块。
    /// </summary>
    internal sealed class SlabMemoryPool : MemoryPool<byte>
    {
        /// <summary>
        /// 大多数操作系统的页面大小为4KB 操作系统都是分页机制管理内存和虚拟内存，内存会被划分为固定大小，如4kb
        /// </summary>
        private const int _blockSize = 4096;
        /// <summary>
        ///为每个 slab 分配 32 个连续的块，使 slab 大小为 128k。这大于 85k 大小，后者将放置内存在大型对象堆中。
        ///这意味着 GC 不会尝试重新定位此数组，因此它保持固定状态的事实不会产生负面影响 影响内存管理的压缩。
        /// </summary>

        private const int _blockCount = 32;

        /// <summary>
        /// 池化数据块的最大分配数据块大小，较大的值可以租用，但它们将在使用后被丢弃，而不是返回到池中。
        /// </summary>
        public override int MaxBufferSize { get; } = _blockSize;

        public static int BlockSize => _blockSize;
        /// <summary>
        /// 最大 4096 * 32 为您提供 128k 连续字节的 slabLength，每个 slab 分配
        /// </summary>
        private static readonly int _slabLength = _blockSize * _blockCount;
        /// <summary>
        /// 当前在池中的块的线程安全集合。一个 slab 将预先分配所有块跟踪对象
        /// 并将它们添加到此集合中。当请求内存时，它首先从这里获取，当它被返回时，它被重新添加。
        /// </summary>
        private readonly ConcurrentQueue<MemoryPoolBlock> _blocks = new ConcurrentQueue<MemoryPoolBlock>();
        /// <summary>
        /// 此池分配的 slab 的线程安全集合。只要 slab 在此集合和 slab 中。IsActive、返回时，这些块将被添加到 _blocks 中。
        /// </summary>

        private readonly ConcurrentStack<MemoryPoolSlab> _slabs = new ConcurrentStack<MemoryPoolSlab>();

        private bool _isDisposed;


        private int _totalAllocatedBlocks;
        /// <summary>
        /// 此默认值传递到 Rent 以使用池的默认值。
        /// </summary>
        private const int AnySize = -1;
        private readonly object _disposeSync = new object();
        public override IMemoryOwner<byte> Rent(int size = AnySize)
        {
            if (size > _blockSize)
            {
                ThrowArgumentOutOfRangeException_BufferRequestTooLarge(_blockSize);
            }

            var block = Lease();
            return block;
        }
        /// <summary>
        /// 调用 以从池中获取一个块。
        /// <summary>
        ///<returns>为被调用的 Block 保留的 block.当它不再被使用时，必须将其传递给 Return。</returns>
        public MemoryPoolBlock Lease()
        {
            if (_isDisposed)
            {
                ThrowObjectDisposedException();
            }

            if (_blocks.TryDequeue(out MemoryPoolBlock block))
            {
                // 成功从堆栈中获取区块 - 返回
                block.Lease();
                return block;
            }
            // 没有可用的区块 - 扩大矿池
            block = AllocateSlab();
            block.Lease();
            return block;
        }
        public MemoryPoolBlock AllocateSlab()
        {
            var slab =  MemoryPoolSlab.Create(_slabLength);
            _slabs.Push(slab);

            var basePtr= slab.NativePointer;
            //页面对齐块
            var offset= (int)((((ulong)basePtr + (uint)_blockSize - 1) & ~((uint)_blockSize - 1)) - (ulong)basePtr);
            var blockCount = (_slabLength - offset) / _blockSize;
            Interlocked.Add(ref _totalAllocatedBlocks, blockCount);
            MemoryPoolBlock block = null;

            for (int i = 0; i < blockCount; i++)
            {
                block = new MemoryPoolBlock(this, slab, offset, _blockSize);

                if (i != blockCount - 1) // last block
                {
#if BLOCK_LEASE_TRACKING
                    block.IsLeased = true;
#endif
                    Return(block);
                }

                offset += _blockSize;
            }

            return block;
        }
        /// <summary>
        /// 调用以将块返回到池中。调用 Return 后，内存不再属于调用者，并且如果随后读取或修改内存，则会发生非常糟糕的事情。
        /// 如果调用方未能调用 Return 并且 Block Tracking Object 被垃圾回收后，Block Tracking Object 的 Finalizer 将自动重新创建并返回池中的新跟踪对象。
        /// 这只会在服务器中存在错误时发生，但有必要避免
        /// 由于丢失了块跟踪对象，在 Slab 中留下了“死区”。
        ///<param name =“block”>要返回的块。它必须已通过对同一内存池实例调用 Lease 来获取。</param>
        internal void Return(MemoryPoolBlock block)
        {
            if (!_isDisposed)
            {
                _blocks.Enqueue(block);
            }
            else
            {
                GC.SuppressFinalize(block);
            }
        }
        internal void RefreshBlock(MemoryPoolSlab slab, int offset, int length)
        {
            lock (_disposeSync)
            {
                if (!_isDisposed && slab != null && slab.IsActive)
                {
                    //需要创建一个新对象，因为此对象正在完成请注意，这必须在 _disposeSync 锁中调用，因为块可以与终结器同时被释放。
                    Return(new MemoryPoolBlock(this, slab, offset, length));
                }
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            lock (_disposeSync)
            {
                _isDisposed = true;

                if (disposing)
                {
                    while (_slabs.TryPop(out MemoryPoolSlab slab))
                    {
                        // dispose managed state (managed objects).
                        slab.Dispose();
                    }
                }

                // Discard blocks in pool
                while (_blocks.TryDequeue(out MemoryPoolBlock block))
                {
                    GC.SuppressFinalize(block);
                }
            }
        }
        private static void ThrowArgumentOutOfRangeException_BufferRequestTooLarge(int maxSize)
        {
            throw new ArgumentOutOfRangeException("size", $"Cannot allocate more than {maxSize} bytes in a single buffer");
        }

        private static void ThrowObjectDisposedException() => throw new ObjectDisposedException("MemoryPool");
    }
}
