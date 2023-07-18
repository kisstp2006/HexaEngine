﻿namespace HexaEngine.Core.Graphics.Buffers
{
    using HexaEngine.Core.Graphics;
    using System;
    using System.Runtime.CompilerServices;

    public unsafe class VertexBuffer<T> : IBuffer, IVertexBuffer<T> where T : unmanaged
    {
        private const int DefaultCapacity = 8;

        private readonly IGraphicsDevice device;
        private readonly string dbgName;

        private IBuffer buffer;
        private BufferDescription description;

        private T* items;
        private uint count;
        private uint capacity;

        private bool isDirty;

        private bool disposedValue;

        public VertexBuffer(IGraphicsDevice device, CpuAccessFlags flags, [CallerFilePath] string filename = "", [CallerLineNumber] int lineNumber = 0)
        {
            this.device = device;
            dbgName = $"VertexBuffer: {Path.GetFileNameWithoutExtension(filename)}, Line:{lineNumber}";

            capacity = DefaultCapacity;

            description = new(sizeof(T) * DefaultCapacity, BindFlags.VertexBuffer, Usage.Default, flags);
            if ((flags & CpuAccessFlags.Write) != 0)
            {
                description.Usage = Usage.Dynamic;
            }
            if ((flags & CpuAccessFlags.Read) != 0)
            {
                description.Usage = Usage.Staging;
            }
            if ((flags & CpuAccessFlags.None) != 0)
            {
                throw new InvalidOperationException("If cpu access flags are none initial data must be provided");
            }

            items = Alloc<T>(DefaultCapacity);
            ZeroMemoryT(items, (uint)DefaultCapacity);
            buffer = device.CreateBuffer(description);
            buffer.DebugName = dbgName;
            MemoryManager.Register(buffer);
        }

        public VertexBuffer(IGraphicsDevice device, T[] vertices, CpuAccessFlags flags, [CallerFilePath] string filename = "", [CallerLineNumber] int lineNumber = 0)
        {
            this.device = device;
            dbgName = $"VertexBuffer: {Path.GetFileNameWithoutExtension(filename)}, Line:{lineNumber}";

            capacity = (uint)vertices.Length;
            count = capacity;

            description = new(sizeof(T) * (int)capacity, BindFlags.VertexBuffer, Usage.Default, flags);
            if ((flags & CpuAccessFlags.None) != 0)
            {
                capacity = 0;
                description.Usage = Usage.Immutable;
                fixed (T* ptr = vertices)
                {
                    buffer = device.CreateBuffer(ptr, capacity, description);
                }
                return;
            }
            if ((flags & CpuAccessFlags.Write) != 0)
            {
                description.Usage = Usage.Dynamic;
            }
            if ((flags & CpuAccessFlags.Read) != 0)
            {
                description.Usage = Usage.Staging;
            }

            items = AllocCopy(vertices);
            buffer = device.CreateBuffer(items, capacity, description);
            buffer.DebugName = dbgName;
            MemoryManager.Register(buffer);
        }

        public VertexBuffer(IGraphicsDevice device, T* vertices, uint count, CpuAccessFlags flags, [CallerFilePath] string filename = "", [CallerLineNumber] int lineNumber = 0)
        {
            this.device = device;
            dbgName = $"VertexBuffer: {Path.GetFileNameWithoutExtension(filename)}, Line:{lineNumber}";

            capacity = count;
            this.count = capacity;

            description = new(sizeof(T) * (int)capacity, BindFlags.VertexBuffer, Usage.Default, flags);
            if ((flags & CpuAccessFlags.None) != 0)
            {
                capacity = 0;
                description.Usage = Usage.Immutable;
                buffer = device.CreateBuffer(vertices, capacity, description);
                return;
            }
            if ((flags & CpuAccessFlags.Write) != 0)
            {
                description.Usage = Usage.Dynamic;
            }
            if ((flags & CpuAccessFlags.Read) != 0)
            {
                description.Usage = Usage.Staging;
            }

            items = AllocCopy(vertices, count);
            buffer = device.CreateBuffer(items, capacity, description);
            buffer.DebugName = dbgName;
            MemoryManager.Register(buffer);
        }

        public VertexBuffer(IGraphicsDevice device, uint capacity, CpuAccessFlags flags, [CallerFilePath] string filename = "", [CallerLineNumber] int lineNumber = 0)
        {
            this.device = device;
            dbgName = $"VertexBuffer: {Path.GetFileNameWithoutExtension(filename)}, Line:{lineNumber}";

            this.capacity = capacity;

            description = new(sizeof(T) * (int)capacity, BindFlags.VertexBuffer, Usage.Default, flags);
            if (flags.HasFlag(CpuAccessFlags.Write))
            {
                description.Usage = Usage.Dynamic;
            }
            if (flags.HasFlag(CpuAccessFlags.Read))
            {
                description.Usage = Usage.Staging;
            }
            if (flags.HasFlag(CpuAccessFlags.None))
            {
                throw new InvalidOperationException("If cpu access flags are none initial data must be provided");
            }

            items = Alloc<T>(capacity);
            ZeroMemoryT(items, (uint)capacity);
            buffer = device.CreateBuffer(items, capacity, description);
            buffer.DebugName = dbgName;
            MemoryManager.Register(buffer);
        }

        public event EventHandler? OnDisposed
        {
            add
            {
                buffer.OnDisposed += value;
            }

            remove
            {
                buffer.OnDisposed -= value;
            }
        }

        public uint Count => count;

        public uint Stride { get; } = (uint)sizeof(T);

        public uint Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => capacity;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                var tmp = Alloc<T>((int)value);
                var oldsize = count * sizeof(T);
                var newsize = value * sizeof(T);
                Buffer.MemoryCopy(items, tmp, newsize, oldsize > newsize ? newsize : oldsize);
                Free(items);
                items = tmp;
                capacity = value;
                count = capacity < count ? capacity : count;
                MemoryManager.Unregister(buffer);
                buffer.Dispose();
                buffer = device.CreateBuffer(items, capacity, description);
                buffer.DebugName = dbgName;
                MemoryManager.Register(buffer);
            }
        }

        public BufferDescription Description => buffer.Description;

        public int Length => buffer.Length;

        public ResourceDimension Dimension => buffer.Dimension;

        public nint NativePointer => buffer.NativePointer;

        public string? DebugName { get => buffer.DebugName; set => buffer.DebugName = value; }

        public bool IsDisposed => buffer.IsDisposed;

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => items[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                items[index] = value;
                isDirty = true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetCounter()
        {
            count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureCapacity(uint capacity)
        {
            if (this.capacity < capacity)
            {
                Grow(capacity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Grow(uint capacity)
        {
            uint newcapacity = count == 0 ? DefaultCapacity : 2 * count;

            if (newcapacity < capacity)
            {
                newcapacity = capacity;
            }

            Capacity = newcapacity;
        }

        public void Add(params T[] vertices)
        {
            uint index = count;
            count += (uint)vertices.Length;
            EnsureCapacity(count);

            for (int i = 0; i < vertices.Length; i++)
            {
                items[index + i] = vertices[i];
            }

            isDirty = true;
        }

        public void Remove(int index)
        {
            var size = (count - index) * sizeof(T);
            Buffer.MemoryCopy(&items[index + 1], &items[index], size, size);
            isDirty = true;
        }

        public bool Update(IGraphicsContext context)
        {
            if (isDirty)
            {
                context.Write(buffer, items, (int)(count * sizeof(T)));
                isDirty = false;
                return true;
            }
            return false;
        }

        public void Clear()
        {
            count = 0;
            isDirty = true;
        }

        public void FlushMemory()
        {
            Free(items);
        }

        public void CopyTo(IGraphicsContext context, IBuffer buffer)
        {
            context.CopyResource(buffer, this.buffer);
        }

        public void Bind(IGraphicsContext context)
        {
            context.SetVertexBuffer(buffer, (uint)sizeof(T));
        }

        public void Unbind(IGraphicsContext context)
        {
            context.SetVertexBuffer(null, 0);
        }

        public void Dispose()
        {
            if (!disposedValue)
            {
                MemoryManager.Unregister(buffer);
                buffer?.Dispose();
                capacity = 0;
                count = 0;
                Free(items);

                disposedValue = true;
            }

            GC.SuppressFinalize(this);
        }
    }
}