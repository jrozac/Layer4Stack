using Layer4Stack.DataProcessors;
using Layer4Stack.Handlers.Interfaces;
using Layer4Stack.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Layer4Stack.Services
{

    /// <summary>
    /// Tcp server service
    /// </summary>
    public class TcpServerService : TcpServiceBase, IServerService
    {

        #region vars

        /// <summary>
        /// Locker
        /// </summary>
        private readonly object _locker = new object();

        /// <summary>
        /// Logger factory 
        /// </summary>
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<TcpServerService> _logger;

        /// <summary>
        /// Event handler
        /// </summary>
        private readonly IServerEventHandler _eventHandler;

        /// <summary>
        /// Server config 
        /// </summary>
        private readonly ServerConfig _serverConfig;

        /// <summary>
        /// Data processor creator
        /// </summary>
        private readonly Func<IDataProcessor> _createDataProcessorFunc;

        /// <summary>
        /// Socket server
        /// </summary>
        private TcpServerSocket _socketServer;

        #endregion

        public TcpServerService(IServerEventHandler eventHandler, ServerConfig serverConfig,
            ILoggerFactory loggerFactory, EnumDataProcessorType dataProcessorType, 
            Func<byte[], byte[]> getIdFunc = null)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<TcpServerService>();
            _serverConfig = serverConfig;
            _eventHandler = eventHandler;

            // define data processor
            _createDataProcessorFunc = CreateDataProcesorFunc(serverConfig, loggerFactory, dataProcessorType, getIdFunc);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dataProcessor"></param>
        public TcpServerService(IServerEventHandler eventHandler, ServerConfig serverConfig, 
            ILoggerFactory loggerFactory, Func<IDataProcessor> createDataProcessorFunc)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<TcpServerService>();
            _serverConfig = serverConfig;
            _eventHandler = eventHandler;

            // define data processor
            _createDataProcessorFunc = createDataProcessorFunc;
        }

        /// <summary>
        /// Server start status
        /// </summary>
        public bool Started => _socketServer?.Started == true;

        /// <summary>
        /// Clients 
        /// </summary>
        public IList<ClientInfo> Clients => _socketServer?.Clients;

        /// <summary>
        /// Disconnect client
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public bool DisconnectClient(string clientId)
        {
            return _socketServer?.DisconnectClient(clientId) == true;
        }

        /// <summary>
        /// Send data to all clients
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<int> SendToAll(byte[] data)
        {
            return await (_socketServer?.SendToAll(data) ?? Task.FromResult(0));
        }

        /// <summary>
        /// Send data to selected client
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> SendToClient(string clientId, byte[] data)
        {
            return await (_socketServer?.SendMessageToClient(clientId, data) ?? Task.FromResult(false));
        }

        /// <summary>
        /// Starts server
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Start()
        {
            lock (_locker)
            {

                // do nothing if already started 
                if (_socketServer != null)
                {
                    _logger.LogInformation("Server already started.");
                    return true;
                }
                else
                {
                    // init socket
                    _socketServer = new TcpServerSocket(_createDataProcessorFunc, _serverConfig, _loggerFactory);
                }

            }

            // bind events
            if (_eventHandler != null)
            {

                // client connected
                _socketServer.ClientConnectedEvent += (sender, client) =>
                {
                    _eventHandler.HandleClientConnected(this, client);
                };

                // client disconnected
                _socketServer.ClientDisconnectedEvent += (sender, client) =>
                {
                    _eventHandler.HandleClientDisconnected(this, client);
                };

                // server started
                _socketServer.ServerStartedEvent += (sender, msg) =>
                {
                    _eventHandler.HandleServerStarted(this, _serverConfig);
                };

                // server stopped
                _socketServer.ServerStoppedEvent += (sender, msg) =>
                {
                    _eventHandler.HandleServerStopped(this, _serverConfig);
                };

                // server failed to start
                _socketServer.ServerStartFailureEvent += (sender, msg) =>
                {
                    _eventHandler.HandleServerStartFailure(this, _serverConfig);
                };

                // message received
                _socketServer.MsgReceivedEvent += (sender, msg) =>
                {
                    _eventHandler.HandleReceivedData(this, msg);
                };

                // message sent
                _socketServer.MsgSentEvent += (sender, msg) =>
                {
                    _eventHandler.HandleSentData(this, msg);
                };

            }

            // start server 
            bool status = await _socketServer.ServerStart();

            // clean up if not started
            if(!status)
            {
                Stop();
            }

            // return 
            return status;
 
        }

        /// <summary>
        /// Stops server
        /// </summary>
        /// <returns></returns>
        public void Stop()
        {
            lock(_locker)
            {
                _socketServer?.ServerStop();
                _socketServer = null;
            }
        }

        /// <summary>
        /// Dispose service
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

    }
}
