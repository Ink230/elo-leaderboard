using System;
using System.Net;
using System.Text;
using StackExchange.Redis;

namespace NetCoreServer
{
    /// <summary>
    /// WebSocket server
    /// </summary>
    /// <remarks> WebSocket server is used to communicate with clients using WebSocket protocol. Thread-safe.</remarks>
    public class WsServer : HttpServer, IWebSocket
    {
        internal readonly WebSocket WebSocket;
        public ConnectionMultiplexer _redis;

        /// <summary>
        /// Initialize WebSocket server with a given IP address and port number
        /// </summary>
        /// <param name="address">IP address</param>
        /// <param name="port">Port number</param>
        public WsServer(IPAddress address, int port) : base(address, port) { WebSocket = new WebSocket(this);
            //start Redis Client up
            var options = ConfigurationOptions.Parse("localhost:6379"); // host1:port1, host2:port2, ...
            //WARNING from Ink230: The password is left as a TEMPORARY placeholder for initial configuration 
            //WARNING from Ink230: Place in secrets and retrieve from an injected configuration instance
            options.Password = "yourredispassword";
            _redis = ConnectionMultiplexer.Connect(options);
     
        }
        /// <summary>
        /// Initialize WebSocket server with a given IP address and port number
        /// </summary>
        /// <param name="address">IP address</param>
        /// <param name="port">Port number</param>
        public WsServer(string address, int port) : base(address, port) { WebSocket = new WebSocket(this); }
        /// <summary>
        /// Initialize WebSocket server with a given IP endpoint
        /// </summary>
        /// <param name="endpoint">IP endpoint</param>
        public WsServer(IPEndPoint endpoint) : base(endpoint) { WebSocket = new WebSocket(this); }

        public virtual bool CloseAll(int status)
        {
            lock (WebSocket.WsSendLock)
            {
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_CLOSE, false, null, 0, 0, status);
                if (!Multicast(WebSocket.WsSendBuffer.ToArray()))
                    return false;

                return base.DisconnectAll();
            }
        }

        public override bool Multicast(byte[] buffer, long offset, long size)
        {
            if (!IsStarted)
                return false;

            if (size == 0)
                return true;

            // Multicast data to all WebSocket sessions
            foreach (var session in Sessions.Values)
            {
                if (session is WsSession wsSession)
                {
                    if (wsSession.WebSocket.WsHandshaked)
                        wsSession.SendAsync(buffer, offset, size);
                }
            }

            return true;
        }

        #region WebSocket multicast text methods

        public bool MulticastText(byte[] buffer, long offset, long size)
        {
            lock (WebSocket.WsSendLock)
            {
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_TEXT, false, buffer, offset, size);
                return Multicast(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public bool MulticastText(string text)
        {
            lock (WebSocket.WsSendLock)
            {
                var data = Encoding.UTF8.GetBytes(text);
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_TEXT, false, data, 0, data.Length);
                return Multicast(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public bool MulticastText(string text, System.Guid user)
        {
            lock (WebSocket.WsSendLock)
            {
                var data = Encoding.UTF8.GetBytes(text);
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_TEXT, false, data, 0, data.Length);
                return Multicast(WebSocket.WsSendBuffer.ToArray(), user);
            }
        }

        #endregion

        #region WebSocket multicast binary methods

        public bool MulticastBinary(byte[] buffer, long offset, long size)
        {
            lock (WebSocket.WsSendLock)
            {
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_BINARY, false, buffer, offset, size);
                return Multicast(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public bool MulticastBinary(string text)
        {
            lock (WebSocket.WsSendLock)
            {
                var data = Encoding.UTF8.GetBytes(text);
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_BINARY, false, data, 0, data.Length);
                return Multicast(WebSocket.WsSendBuffer.ToArray());
            }
        }

        #endregion

        #region WebSocket multicast ping methods

        public bool SendPing(byte[] buffer, long offset, long size)
        {
            lock (WebSocket.WsSendLock)
            {
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_PING, false, buffer, offset, size);
                return Multicast(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public bool SendPing(string text)
        {
            lock (WebSocket.WsSendLock)
            {
                var data = Encoding.UTF8.GetBytes(text);
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_PING, false, data, 0, data.Length);
                return Multicast(WebSocket.WsSendBuffer.ToArray());
            }
        }

        #endregion

        #region WebSocket multicast pong methods

        public bool SendPong(byte[] buffer, long offset, long size)
        {
            lock (WebSocket.WsSendLock)
            {
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_PONG, false, buffer, offset, size);
                return Multicast(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public bool SendPong(string text)
        {
            lock (WebSocket.WsSendLock)
            {
                var data = Encoding.UTF8.GetBytes(text);
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_PONG, false, data, 0, data.Length);
                return Multicast(WebSocket.WsSendBuffer.ToArray());
            }
        }

        #endregion

        protected override TcpSession CreateSession() { return new WsSession(this); }
    }
}
