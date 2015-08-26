﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.TestHost
{
    internal class TestWebSocket : WebSocket
    {
        private ReceiverSenderBuffer _receiveBuffer;
        private ReceiverSenderBuffer _sendBuffer;
        private readonly string _subProtocol;
        private WebSocketState _state;
        private WebSocketCloseStatus? _closeStatus;
        private string _closeStatusDescription;
        private Message _receiveMessage;

        public static Tuple<TestWebSocket, TestWebSocket> CreatePair(string subProtocol)
        {
            var buffers = new[] { new ReceiverSenderBuffer(), new ReceiverSenderBuffer() };
            return Tuple.Create(
                new TestWebSocket(subProtocol, buffers[0], buffers[1]),
                new TestWebSocket(subProtocol, buffers[1], buffers[0]));
        }

        public override WebSocketCloseStatus? CloseStatus
        {
            get { return _closeStatus; }
        }

        public override string CloseStatusDescription
        {
            get { return _closeStatusDescription; }
        }

        public override WebSocketState State
        {
            get { return _state; }
        }

        public override string SubProtocol
        {
            get { return _subProtocol; }
        }

        public async override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (State == WebSocketState.Open || State == WebSocketState.CloseReceived)
            {
                // Send a close message.
                await CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
            }

            if (State == WebSocketState.CloseSent)
            {
                // Do a receiving drain
                var data = new byte[1024];
                WebSocketReceiveResult result;
                do
                {
                    result = await ReceiveAsync(new ArraySegment<byte>(data), cancellationToken);
                }
                while (result.MessageType != WebSocketMessageType.Close);
            }
        }

        public async override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            ThrowIfOutputClosed();

            var message = new Message(closeStatus, statusDescription);
            await _sendBuffer.SendAsync(message, cancellationToken);

            if (State == WebSocketState.Open)
            {
                _state = WebSocketState.CloseSent;
            }
            else if (State == WebSocketState.CloseReceived)
            {
                _state = WebSocketState.Closed;
                Close();
            }
        }

        public override void Abort()
        {
            if (_state >= WebSocketState.Closed) // or Aborted
            {
                return;
            }

            _state = WebSocketState.Aborted;
            Close();
        }

        public override void Dispose()
        {
            if (_state >= WebSocketState.Closed) // or Aborted
            {
                return;
            }

            _state = WebSocketState.Closed;
            Close();
        }

        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            ThrowIfInputClosed();
            ValidateSegment(buffer);
            // TODO: InvalidOperationException if any receives are currently in progress.
            
            Message receiveMessage = _receiveMessage;
            _receiveMessage = null;
            if (receiveMessage == null)
            {
                receiveMessage = await _receiveBuffer.ReceiveAsync(cancellationToken);
            }
            if (receiveMessage.MessageType == WebSocketMessageType.Close)
            {
                _closeStatus = receiveMessage.CloseStatus;
                _closeStatusDescription = receiveMessage.CloseStatusDescription ?? string.Empty;
                var result = new WebSocketReceiveResult(0, WebSocketMessageType.Close, true, _closeStatus, _closeStatusDescription);
                if (_state == WebSocketState.Open)
                {
                    _state = WebSocketState.CloseReceived;
                }
                else if (_state == WebSocketState.CloseSent)
                {
                    _state = WebSocketState.Closed;
                    Close();
                }
                return result;
            }
            else
            {
                int count = Math.Min(buffer.Count, receiveMessage.Buffer.Count);
                bool endOfMessage = count == receiveMessage.Buffer.Count;
                Array.Copy(receiveMessage.Buffer.Array, receiveMessage.Buffer.Offset, buffer.Array, buffer.Offset, count);
                if (!endOfMessage)
                {
                    receiveMessage.Buffer = new ArraySegment<byte>(receiveMessage.Buffer.Array, receiveMessage.Buffer.Offset + count, receiveMessage.Buffer.Count - count);
                    _receiveMessage = receiveMessage;
                }
                endOfMessage = endOfMessage && receiveMessage.EndOfMessage;
                return new WebSocketReceiveResult(count, receiveMessage.MessageType, endOfMessage);
            }
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            ValidateSegment(buffer);
            if (messageType != WebSocketMessageType.Binary && messageType != WebSocketMessageType.Text)
            {
                // Block control frames
                throw new ArgumentOutOfRangeException(nameof(messageType), messageType, string.Empty);
            }

            var message = new Message(buffer, messageType, endOfMessage, cancellationToken);
            return _sendBuffer.SendAsync(message, cancellationToken);
        }

        private void Close()
        {
            _receiveBuffer.SetReceiverClosed();
            _sendBuffer.SetSenderClosed();
        }

        private void ThrowIfDisposed()
        {
            if (_state >= WebSocketState.Closed) // or Aborted
            {
                throw new ObjectDisposedException(typeof(TestWebSocket).FullName);
            }
        }

        private void ThrowIfOutputClosed()
        {
            if (State == WebSocketState.CloseSent)
            {
                throw new InvalidOperationException("Close already sent.");
            }
        }

        private void ThrowIfInputClosed()
        {
            if (State == WebSocketState.CloseReceived)
            {
                throw new InvalidOperationException("Close already received.");
            }
        }

        private void ValidateSegment(ArraySegment<byte> buffer)
        {
            if (buffer.Array == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (buffer.Offset < 0 || buffer.Offset > buffer.Array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(buffer.Offset), buffer.Offset, string.Empty);
            }
            if (buffer.Count < 0 || buffer.Count > buffer.Array.Length - buffer.Offset)
            {
                throw new ArgumentOutOfRangeException(nameof(buffer.Count), buffer.Count, string.Empty);
            }
        }

        private TestWebSocket(string subProtocol, ReceiverSenderBuffer readBuffer, ReceiverSenderBuffer writeBuffer)
        {
            _state = WebSocketState.Open;
            _subProtocol = subProtocol;
            _receiveBuffer = readBuffer;
            _sendBuffer = writeBuffer;
        }

        private class Message
        {
            public Message(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken token)
            {
                Buffer = buffer;
                CloseStatus = null;
                CloseStatusDescription = null;
                EndOfMessage = endOfMessage;
                MessageType = messageType;
            }

            public Message(WebSocketCloseStatus? closeStatus, string closeStatusDescription)
            {
                Buffer = new ArraySegment<byte>(new byte[0]);
                CloseStatus = closeStatus;
                CloseStatusDescription = closeStatusDescription;
                MessageType = WebSocketMessageType.Close;
                EndOfMessage = true;
            }

            public WebSocketCloseStatus? CloseStatus { get; set; }
            public string CloseStatusDescription { get; set; }
            public ArraySegment<byte> Buffer { get; set; }
            public bool EndOfMessage { get; set; }
            public WebSocketMessageType MessageType { get; set; }
        }

        private class ReceiverSenderBuffer
        {
            private bool _receiverClosed;
            private bool _senderClosed;
            private bool _disposed;
            private SemaphoreSlim _sem;
            private Queue<Message> _messageQueue;
            
            public ReceiverSenderBuffer()
            {
                _sem = new SemaphoreSlim(0);
                _messageQueue = new Queue<Message>();
            }

            public async virtual Task<Message> ReceiveAsync(CancellationToken cancellationToken)
            {
                if (_disposed)
                {
                    ThrowNoReceive();
                }
                await _sem.WaitAsync(cancellationToken);
                lock (_messageQueue)
                {
                    if (_messageQueue.Count == 0)
                    {
                        _disposed = true;
                        _sem.Dispose();
                        ThrowNoReceive();
                    }
                    return _messageQueue.Dequeue();
                }
            }

            public virtual Task SendAsync(Message message, CancellationToken cancellationToken)
            {
                lock (_messageQueue)
                {
                    if (_senderClosed)
                    {
                        throw new ObjectDisposedException(typeof(TestWebSocket).FullName);
                    }
                    if (_receiverClosed)
                    {
                        throw new IOException("The remote end closed the connection.", new ObjectDisposedException(typeof(TestWebSocket).FullName));
                    }

                    // we return immediately so we need to copy the buffer since the sender can re-use it
                    var array = new byte[message.Buffer.Count];
                    Array.Copy(message.Buffer.Array, message.Buffer.Offset, array, 0, message.Buffer.Count);
                    message.Buffer = new ArraySegment<byte>(array);

                    _messageQueue.Enqueue(message);
                    _sem.Release();

                    return Task.FromResult(true);
                }
            }

            public void SetReceiverClosed()
            {
                lock (_messageQueue)
                {
                    if (!_receiverClosed)
                    {
                        _receiverClosed = true;
                        if (!_disposed)
                        {
                            _sem.Release();
                        }
                    }
                }
            }

            public void SetSenderClosed()
            {
                lock (_messageQueue)
                {
                    if (!_senderClosed)
                    {
                        _senderClosed = true;
                        if (!_disposed)
                        {
                            _sem.Release();
                        }
                    }
                }
            }

            private void ThrowNoReceive()
            {
                if (_receiverClosed)
                {
                    throw new ObjectDisposedException(typeof(TestWebSocket).FullName);
                }
                else // _senderClosed
                {
                    throw new IOException("The remote end closed the connection.", new ObjectDisposedException(typeof(TestWebSocket).FullName));
                }
            }
        }
    }
}
