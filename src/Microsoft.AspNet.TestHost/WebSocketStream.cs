using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.TestHost
{
    class WebSocketStream : Stream
    {
        private ReaderWriterBuffer _readBuffer;
        private ReaderWriterBuffer _writeBuffer;
        private bool _isDisposed;

        public static Tuple<WebSocketStream, WebSocketStream> CreatePair()
        {
            var buffers = new[] { new ReaderWriterBuffer(), new ReaderWriterBuffer() };
            return Tuple.Create(
                new WebSocketStream(buffers[0], buffers[1]),
                new WebSocketStream(buffers[1], buffers[0]));
        }

        private WebSocketStream(ReaderWriterBuffer readBuffer, ReaderWriterBuffer writeBuffer)
        {
            _readBuffer = readBuffer;
            _writeBuffer = writeBuffer;
            _isDisposed = false;
        }

        public override bool CanRead
        {
            get
            {
                return false;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        public override void Flush()
        {
            return;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return base.FlushAsync(cancellationToken);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _readBuffer.ReadAsync(new ArraySegment<byte>(buffer, offset, count), cancellationToken);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _writeBuffer.WriteAsync(new ArraySegment<byte>(buffer, offset, count), cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _writeBuffer.Close();
                _isDisposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(typeof(Stream).FullName);
            }
        }

        private class ReaderWriterBuffer
        {
            public async virtual Task<int> ReadAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            {
                int count = 0;
                while (true)
                {
                    await _sem.WaitAsync(cancellationToken);
                    lock (_buffer)
                    {
                        count = Math.Min(_buffer.Count, buffer.Count);
                        if (count != 0)
                        {
                            _buffer.CopyTo(0, buffer.Array, buffer.Offset, count);
                            _buffer.RemoveRange(0, count);
                            if (_buffer.Count != 0)
                            {
                                _sem.Release();
                            }
                            break;
                        }
                        else if (_closed)
                        {
                            break;
                        }
                    }
                }
                cancellationToken.ThrowIfCancellationRequested();
                return await Task.FromResult(count);
            }

            public virtual Task WriteAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                lock (_buffer)
                {
                    _buffer.AddRange(buffer);
                }
                _sem.Release();
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(true);
            }

            public void Close()
            {
                if (!_closed)
                {
                    _closed = true;
                    _sem.Release();
                }
            }

            public ReaderWriterBuffer()
            {
                _buffer = new List<byte>();
                _closed = false;
                _event = new ManualResetEventSlim();
                _sem = new SemaphoreSlim(0);
            }

            private List<byte> _buffer;
            private bool _closed;
            private ManualResetEventSlim _event;
            private SemaphoreSlim _sem;
        }
    }
}
