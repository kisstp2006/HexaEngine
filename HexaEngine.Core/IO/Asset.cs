﻿namespace HexaEngine.Core.IO
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    public class Asset : IDisposable
    {
        private readonly string fullPath;
        private bool exists;
        private uint crc32Hash;

        private int refCount;

        public Asset(string fullPath)
        {
            this.fullPath = fullPath;
            exists = FileSystem.Exists(fullPath);
            crc32Hash = exists ? FileSystem.GetCrc32Hash(fullPath) : unchecked((uint)-1);
        }

        public string Name => Path.GetFileName(fullPath);

        public string Extension => Path.GetExtension(fullPath);

        public string? Directory => Path.GetDirectoryName(fullPath);

        public string FullPath => fullPath;

        public bool Exists
        {
            get => exists;
            internal set
            {
                if (exists == value)
                {
                    return;
                }
                exists = value;
                OnExistsChanged?.Invoke(this, value);
            }
        }

        public uint Crc32Hash
        {
            get => crc32Hash;
            internal set
            {
                if (crc32Hash == value)
                {
                    return;
                }
                crc32Hash = value;
                OnContentChanged?.Invoke(this);
            }
        }

        public event Action<Asset, bool>? OnExistsChanged;

        public event Action<Asset>? OnContentChanged;

        internal void Refresh()
        {
            Exists = FileSystem.Exists(fullPath);
            Crc32Hash = exists ? FileSystem.GetCrc32Hash(fullPath) : unchecked((uint)-1);
        }

        /// <summary>
        /// Opens a virtual stream for reading the file at the specified path.
        /// </summary>
        /// <param name="path">The path of the file to open.</param>
        /// <returns>A <see cref="VirtualStream"/> for reading the file.</returns>
        public VirtualStream Open()
        {
            if (!Exists)
            {
                throw new FileNotFoundException(fullPath);
            }

            return FileSystem.Open(fullPath);
        }

        /// <summary>
        /// Opens a <see cref="StreamReader"/> for reading the file at the specified path.
        /// </summary>
        /// <param name="path">The path of the file to open.</param>
        /// <returns>A <see cref="StreamReader"/> for reading the file.</returns>
        public StreamReader OpenRead()
        {
            if (!Exists)
            {
                throw new FileNotFoundException(fullPath);
            }

            return FileSystem.OpenRead(fullPath);
        }

        /// <summary>
        /// Tries to open a virtual stream for reading the file at the specified path.
        /// </summary>
        /// <param name="path">The path of the file to open.</param>
        /// <param name="stream">When this method returns, contains the <see cref="VirtualStream"/> for reading the file, if the file exists; otherwise, the default _value.</param>
        /// <returns><see langword="true"/> if the file was successfully opened; otherwise, <see langword="false"/>.</returns>
        public bool TryOpen([NotNullWhen(true)] out VirtualStream? stream)
        {
            if (!Exists)
            {
                stream = null;
                return false;
            }

            stream = FileSystem.Open(fullPath);
            return true;
        }

        /// <summary>
        /// Reads all lines of the file at the specified path.
        /// </summary>
        /// <param name="path">The path of the file to read.</param>
        /// <returns>An bundles of lines read from the file.</returns>
        public string[] ReadAllLines()
        {
            var fs = FileSystem.Open(fullPath);
            var reader = new StreamReader(fs);
            var result = reader.ReadToEnd().Split(Environment.NewLine);
            reader.Close();
            fs.Close();
            return result;
        }

        /// <summary>
        /// Reads all bytes of the file at the specified path.
        /// </summary>
        /// <param name="path">The path of the file to read.</param>
        /// <returns>An bundles of bytes read from the file.</returns>
        public byte[] ReadAllBytes()
        {
            var fs = FileSystem.Open(fullPath);
            var buffer = new byte[fs.Length];
            fs.Read(buffer, 0, buffer.Length);
            fs.Close();
            return buffer;
        }

        /// <summary>
        /// Reads the file at the specified path into a <see cref="FileBlob"/>.
        /// </summary>
        /// <param name="path">The path of the file to read.</param>
        /// <returns>A <see cref="FileBlob"/> containing the file data.</returns>
        public unsafe FileBlob ReadBlob()
        {
            var fs = FileSystem.Open(fullPath);
            var blob = new FileBlob((nint)fs.Length);
            fs.Read(blob.AsSpan());
            fs.Close();
            return blob;
        }

        /// <summary>
        /// Tries to read all lines of the file at the specified path.
        /// </summary>
        /// <param name="path">The path of the file to read.</param>
        /// <param name="lines">When this method returns, contains an bundles of lines read from the file if the file exists; otherwise, the default _value.</param>
        /// <returns><see langword="true"/> if the file was successfully read; otherwise, <see langword="false"/>.</returns>
        public bool TryReadAllLines([NotNullWhen(true)] out string[]? lines)
        {
            if (FileSystem.TryOpen(fullPath, out var fs))
            {
                var reader = new StreamReader(fs);
                lines = reader.ReadToEnd().Split(Environment.NewLine);
                reader.Dispose();
                return true;
            }

            lines = null;
            return false;
        }

        /// <summary>
        /// Reads all text from the file at the specified path.
        /// </summary>
        /// <param name="path">The path of the file to read.</param>
        /// <returns>The content of the file as a string.</returns>
        public string ReadAllText()
        {
            var fs = FileSystem.Open(fullPath);
            var reader = new StreamReader(fs);
            var result = reader.ReadToEnd();
            reader.Close();
            fs.Close();
            return result;
        }

        /// <summary>
        /// Tries to read all text from the file at the specified path.
        /// </summary>
        /// <param name="path">The path of the file to read.</param>
        /// <param name="text">When this method returns, contains the content of the file as a string if the file exists; otherwise, the default _value.</param>
        /// <returns><see langword="true"/> if the file was successfully read; otherwise, <see langword="false"/>.</returns>
        public bool TryReadAllText([NotNullWhen(true)] out string? text)
        {
            if (FileSystem.TryOpen(fullPath, out var fs))
            {
                var reader = new StreamReader(fs);
                text = reader.ReadToEnd();
                return true;
            }
            text = null;
            return false;
        }

        public void AddRef()
        {
            Interlocked.Increment(ref refCount);
        }

        public void RemoveRef()
        {
            Interlocked.Decrement(ref refCount);
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                RemoveRef();
                if (Volatile.Read(ref refCount) == 0)
                {
                    FileSystem.DestroyAsset(this);
                }

                // GC is calling.
                if (!disposing)
                {
                    FileSystem.DestroyAsset(this);
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Asset()
        {
            Dispose(false);
        }
    }
}