// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Todd.Applicationkernel.Core.Networking.Shared
{
    /// <summary>
    /// 字节缓冲内存池使用的 Slab 跟踪对象。slab 是一个大的分配，它被划分为更小的块。这然后将各个块视为独立的数组段。
    /// </summary>
    internal class MemoryPoolSlab : IDisposable
    {
        private GCHandle _gcHandle;
        private bool _isDisposed;

        public MemoryPoolSlab(byte[] data)
        {
            Array = data;
            _gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            NativePointer = _gcHandle.AddrOfPinnedObject();
        }
        /// <summary>
        /// 只要此 slab 中的块被视为可返回到池中，则为 True。为了缩小内存池大小 必须删除整个 slab 的大小。这是通过以下方法完成的：
        /// （1） 将 IsActive 设置为 false，并删除slab 的 _slabs 集合中，
        /// （2） 因为当前正在使用的每个块都被 Return（） 到池中，因此它将允许进行垃圾回收而不是重新池化，
        /// （3） 当所有块跟踪对象都是垃圾时已收集，并且 slab 不再被引用，则 slab 将被垃圾回收，并且 memory unpinned 将由 slab 的 Dispose 取消固定。
        /// </summary>
        public bool IsActive => !_isDisposed;

        public IntPtr NativePointer { get; private set; }

        public byte[] Array { get; private set; }

        public static MemoryPoolSlab Create(int length)
        {
            // allocate and pin requested memory length
            var array = new byte[length];

            // allocate and return slab tracking object
            return new MemoryPoolSlab(array);
        }

        protected void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            Array = null;
            NativePointer = IntPtr.Zero;

            if (_gcHandle.IsAllocated)
            {
                _gcHandle.Free();
            }
        }

        ~MemoryPoolSlab()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
