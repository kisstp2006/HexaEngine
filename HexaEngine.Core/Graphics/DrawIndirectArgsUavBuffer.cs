﻿namespace HexaEngine.Core.Graphics
{
    using System;
    using System.Runtime.CompilerServices;

    public unsafe class DrawIndirectArgsUavBuffer<T> : IBuffer where T : unmanaged
    {
        private const int DefaultCapacity = 64;
        private readonly IGraphicsDevice device;
        private readonly bool canWrite;
        private readonly bool canRead;
        private readonly BufferUnorderedAccessViewFlags uavFlags;
        private readonly BufferExtendedShaderResourceViewFlags srvFlags;
        private readonly string dbgName;
        private BufferDescription bufferDescription;
        private BufferDescription copyDescription;
        private IBuffer buffer;
        private IBuffer? copyBuffer;
        private IUnorderedAccessView uav;
        private IShaderResourceView srv;

        private T* items;
        private uint count;
        private bool isDirty;
        private uint capacity;

        private bool disposedValue;

        public DrawIndirectArgsUavBuffer(IGraphicsDevice device, CpuAccessFlags accessFlags, BufferUnorderedAccessViewFlags uavFlags = BufferUnorderedAccessViewFlags.None, BufferExtendedShaderResourceViewFlags srvFlags = BufferExtendedShaderResourceViewFlags.None, [CallerFilePath] string filename = "", [CallerLineNumber] int lineNumber = 0)
        {
            this.device = device;
            canWrite = (accessFlags & CpuAccessFlags.Write) != 0;
            canRead = (accessFlags & CpuAccessFlags.Read) != 0;
            this.uavFlags = uavFlags;
            this.srvFlags = srvFlags;
            dbgName = $"DrawIndirectArgsUavBuffer: {filename}, Line:{lineNumber}";
            capacity = DefaultCapacity;
            items = AllocT<T>(DefaultCapacity);
            ZeroMemory(items, DefaultCapacity * sizeof(T));
            bufferDescription = new(sizeof(T) * DefaultCapacity, BindFlags.UnorderedAccess | BindFlags.ShaderResource, Usage.Default, CpuAccessFlags.None, ResourceMiscFlag.DrawIndirectArguments, sizeof(T));
            buffer = device.CreateBuffer(items, DefaultCapacity, bufferDescription);
            buffer.DebugName = dbgName;
            if (canWrite || canRead)
            {
                copyDescription = new(sizeof(T) * DefaultCapacity, BindFlags.ShaderResource, Usage.Dynamic, CpuAccessFlags.Write, ResourceMiscFlag.DrawIndirectArguments, sizeof(T));
                if (canRead)
                {
                    copyDescription.BindFlags = BindFlags.None;
                    copyDescription.Usage = Usage.Staging;
                    copyDescription.CPUAccessFlags = CpuAccessFlags.RW;
                }
                copyBuffer = device.CreateBuffer(copyDescription);
                copyBuffer.DebugName = dbgName + ".CopyBuffer";
            }

            uav = device.CreateUnorderedAccessView(buffer, new(buffer, Format.R32UInt, 0, (int)capacity * sizeof(T) / sizeof(uint), uavFlags));
            uav.DebugName = dbgName + ".UAV";
            srv = device.CreateShaderResourceView(buffer, new(buffer, Format.R32UInt, 0, (int)capacity * sizeof(T) / sizeof(uint), srvFlags));
            srv.DebugName = dbgName + ".SRV";
        }

        public DrawIndirectArgsUavBuffer(IGraphicsDevice device, uint intialCapacity, CpuAccessFlags accessFlags, BufferUnorderedAccessViewFlags uavFlags = BufferUnorderedAccessViewFlags.None, BufferExtendedShaderResourceViewFlags srvFlags = BufferExtendedShaderResourceViewFlags.None, [CallerFilePath] string filename = "", [CallerLineNumber] int lineNumber = 0)
        {
            this.device = device;
            canWrite = (accessFlags & CpuAccessFlags.Write) != 0;
            canRead = (accessFlags & CpuAccessFlags.Read) != 0;
            this.uavFlags = uavFlags;
            this.srvFlags = srvFlags;
            dbgName = $"DrawIndirectArgsUavBuffer: {filename}, Line:{lineNumber}";
            capacity = intialCapacity;
            items = AllocT<T>(intialCapacity);
            ZeroMemory(items, (int)intialCapacity * sizeof(T));
            bufferDescription = new(sizeof(T) * (int)intialCapacity, BindFlags.UnorderedAccess | BindFlags.ShaderResource, Usage.Default, CpuAccessFlags.None, ResourceMiscFlag.DrawIndirectArguments, sizeof(T));
            buffer = device.CreateBuffer(items, intialCapacity, bufferDescription);
            buffer.DebugName = dbgName;
            if (canWrite || canRead)
            {
                copyDescription = new(sizeof(T) * (int)intialCapacity, BindFlags.ShaderResource, Usage.Dynamic, CpuAccessFlags.Write, ResourceMiscFlag.DrawIndirectArguments, sizeof(T));
                if (canRead)
                {
                    copyDescription.BindFlags = BindFlags.None;
                    copyDescription.Usage = Usage.Staging;
                    copyDescription.CPUAccessFlags = CpuAccessFlags.RW;
                }
                copyBuffer = device.CreateBuffer(copyDescription);
                copyBuffer.DebugName = dbgName + ".CopyBuffer";
            }

            uav = device.CreateUnorderedAccessView(buffer, new(buffer, Format.R32UInt, 0, (int)capacity * sizeof(T) / sizeof(uint), uavFlags));
            uav.DebugName = dbgName + ".UAV";
            srv = device.CreateShaderResourceView(buffer, new(buffer, Format.R32UInt, 0, (int)capacity * sizeof(T) / sizeof(uint), srvFlags));
            srv.DebugName = dbgName + ".SRV";
        }

        public T this[int index]
        {
            get { return items[index]; }
            set { items[index] = value; isDirty = true; }
        }

        public T this[uint index]
        {
            get { return items[index]; }
            set { items[index] = value; isDirty = true; }
        }

        public T* Local => items;

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

        /// <summary>
        /// Gets the uav.
        /// </summary>
        /// <_value>
        /// The uav.
        /// </_value>
        public IUnorderedAccessView UAV => uav;

        /// <summary>
        /// Gets the SRV.
        /// </summary>
        /// <_value>
        /// The SRV.
        /// </_value>
        public IShaderResourceView SRV => srv;

        /// <summary>
        /// Not null when CanWrite is true
        /// </summary>
        public IBuffer? CopyBuffer => copyBuffer;

        /// <summary>
        /// Gets a _value indicating whether this instance can write.
        /// </summary>
        /// <_value>
        ///   <c>true</c> if this instance can write; otherwise, <c>false</c>.
        /// </_value>
        public bool CanWrite => canWrite;

        /// <summary>
        /// Gets a _value indicating whether this instance can read.
        /// </summary>
        /// <_value>
        ///   <c>true</c> if this instance can read; otherwise, <c>false</c>.
        /// </_value>
        public bool CanRead => canRead;

        /// <summary>
        /// Gets or sets the capacity.
        /// </summary>
        /// <_value>
        /// The capacity.
        /// </_value>
        public uint Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => capacity;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (value == 0)
                {
                    return;
                }

                if (value == capacity)
                {
                    return;
                }

                if (value < capacity)
                {
                    return;
                }

                var tmp = AllocT<T>((int)value);
                ZeroMemory(tmp, DefaultCapacity * sizeof(T));
                var oldsize = count * sizeof(T);
                var newsize = value * sizeof(T);
                Buffer.MemoryCopy(items, tmp, newsize, oldsize > newsize ? newsize : oldsize);
                Free(items);
                items = tmp;
                capacity = value;
                count = capacity < count ? capacity : count;
                srv.Dispose();
                uav.Dispose();
                buffer.Dispose();
                copyBuffer?.Dispose();
                bufferDescription.ByteWidth = sizeof(T) * (int)capacity;
                buffer = device.CreateBuffer(items, capacity, bufferDescription);
                buffer.DebugName = dbgName;
                if (canWrite || canRead)
                {
                    copyDescription.ByteWidth = sizeof(T) * (int)value;
                    copyBuffer = device.CreateBuffer(copyDescription);
                    copyBuffer.DebugName = dbgName + ".CopyBuffer";
                }

                uav = device.CreateUnorderedAccessView(buffer, new(buffer, Format.R32UInt, 0, (int)capacity * sizeof(T) / sizeof(uint), uavFlags));
                uav.DebugName = dbgName + ".UAV";
                srv = device.CreateShaderResourceView(buffer, new(buffer, Format.R32UInt, 0, (int)capacity * sizeof(T) / sizeof(uint), srvFlags));
                srv.DebugName = dbgName + ".SRV";
                isDirty = true;
            }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <_value>
        /// The description.
        /// </_value>
        public BufferDescription Description => buffer.Description;

        /// <summary>
        /// Gets the length.
        /// </summary>
        /// <_value>
        /// The length.
        /// </_value>
        public int Length => buffer.Length;

        /// <summary>
        /// Get the item count / counter of the buffer.
        /// </summary>
        public uint Count => count;

        /// <summary>
        /// Gets the dimension.
        /// </summary>
        /// <_value>
        /// The dimension.
        /// </_value>
        public ResourceDimension Dimension => buffer.Dimension;

        /// <summary>
        /// Gets the native pointer.
        /// </summary>
        /// <_value>
        /// The native pointer.
        /// </_value>
        public nint NativePointer => buffer.NativePointer;

        /// <summary>
        /// Gets or sets the dbgName of the debug.
        /// </summary>
        /// <_value>
        /// The dbgName of the debug.
        /// </_value>
        public string? DebugName { get => buffer.DebugName; set => buffer.DebugName = value; }

        /// <summary>
        /// Gets a _value indicating whether this instance is disposed.
        /// </summary>
        /// <_value>
        ///   <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </_value>
        public bool IsDisposed => buffer.IsDisposed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetCounter()
        {
            count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Increment()
        {
            count++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDirty()
        {
            isDirty = true;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Add(T args)
        {
            var index = count;
            count++;
            EnsureCapacity(count);
            items[index] = args;
            isDirty = true;
            return ref items[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(int index)
        {
            var size = (count - index) * sizeof(T);
            Buffer.MemoryCopy(&items[index + 1], &items[index], size, size);
            isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(uint index)
        {
            var size = (count - index) * sizeof(T);
            Buffer.MemoryCopy(&items[index + 1], &items[index], size, size);
            isDirty = true;
        }

        public void Clear()
        {
            for (int i = 0; i < count; i++)
            {
                items[i] = default;
            }
            count = 0;
        }

        public void Clear(IGraphicsContext context)
        {
            context.ClearUnorderedAccessViewUint(uav, 0, 0, 0, 0);
        }

        public bool Update(IGraphicsContext context)
        {
            if (copyBuffer == null)
            {
                throw new InvalidOperationException();
            }

            if (!canWrite)
            {
                throw new InvalidOperationException();
            }

            if (isDirty)
            {
                if (canRead)
                {
                    context.Write(copyBuffer, items, (int)(count * sizeof(T)), Map.Write);
                    context.CopyResource(buffer, copyBuffer);
                }
                else
                {
                    context.Write(copyBuffer, items, (int)(count * sizeof(T)), Map.WriteDiscard);
                    context.CopyResource(buffer, copyBuffer);
                }

                isDirty = false;
                return true;
            }
            return false;
        }

        public void Read(IGraphicsContext context)
        {
            if (copyBuffer == null)
            {
                throw new InvalidOperationException();
            }

            if (!canRead)
            {
                throw new InvalidOperationException();
            }

            context.CopyResource(copyBuffer, buffer);
            context.Read(copyBuffer, items, capacity);
        }

        public void CopyTo(IGraphicsContext context, IBuffer buffer)
        {
            context.CopyResource(buffer, this.buffer);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                srv.Dispose();
                uav.Dispose();
                buffer.Dispose();
                copyBuffer?.Dispose();
                count = 0;
                capacity = 0;
                disposedValue = true;
            }
        }

        ~DrawIndirectArgsUavBuffer()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}