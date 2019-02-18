using Layer4Stack.DataProcessors;
using Layer4Stack.Handlers;
using Layer4Stack.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Layer4Stack.Services
{

    /// <summary>
    /// Tcp client service
    /// </summary>
    public class TcpClientService : TcpServiceBase, IClientService
    {

        #region vars

        /// <summary>
        /// Logger factory 
        /// </summary>
        protected ILoggerFactory _loggerFactory;

        /// <summary>
        /// Logger
        /// </summary>
        protected readonly ILogger<TcpClientService> _logger;

        /// <summary>
        /// Data sync
        /// </summary>
        private readonly DataSynchronizator _dataSynchronizator;

        /// <summary>
        /// Client config
        /// </summary>
        private readonly ClientConfig _clientConfig;

        /// <summary>
        /// Event handler
        /// </summary>
        private readonly IClientEventHandler _eventHandler;

        /// <summary>
        /// Data processor
        /// </summary>
        private readonly IDataProcessor _dataProcessor;

        /// <summary>
        /// Data processor creator
        /// </summary>
        protected readonly Func<IDataProcessor> _createDataProcessorFunc;

        /// <summary>
        /// Socket client
        /// </summary>
        private TcpClientSocket _socketClient;

        /// <summary>
        /// Allow auto reconnect
        /// </summary>
        private bool _allowAutoReconnect = false;

        /// <summary>
        /// Connecting
        /// </summary>
        private object _connecting = null;

        /// <summary>
        /// Timer for autoconnect
        /// </summary>
        private Timer _timer;

        #endregion

        /// <summary>
        /// Construstor with 
        /// </summary>
        /// <param name="eventHandler"></param>
        /// <param name="clientConfig"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="dataProcessorType"></param>
        public TcpClientService(IClientEventHandler eventHandler,
                ClientConfig clientConfig, ILoggerFactory loggerFactory,
                EnumDataProcessorType dataProcessorType, Func<byte[],byte[]> getIdFunc = null)
        {

            _createDataProcessorFunc = CreateDataProcesorFunc(clientConfig, loggerFactory, dataProcessorType, getIdFunc);
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<TcpClientService>();
            _clientConfig = clientConfig;
            _eventHandler = eventHandler;
            _dataSynchronizator = new DataSynchronizator(loggerFactory.CreateLogger<DataSynchronizator>());
            _dataProcessor = _createDataProcessorFunc();

            ManageAutoConnect();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dataProcessor"></param>
        public TcpClientService(IClientEventHandler eventHandler, 
                ClientConfig clientConfig, ILoggerFactory loggerFactory, 
                Func<IDataProcessor> createDataProcessorFunc) 
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<TcpClientService>();
            _clientConfig = clientConfig;
            _eventHandler = eventHandler;
            _dataSynchronizator = new DataSynchronizator(loggerFactory.CreateLogger<DataSynchronizator>());
            _dataProcessor = createDataProcessorFunc();
            _createDataProcessorFunc = createDataProcessorFunc;

            ManageAutoConnect();
        }
 
        /// <summary>
        /// Sends data
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<bool> Send(byte[] msg)
        {

            return await(_socketClient?.Send(msg) ?? Task.FromResult(false));
        }

        /// <summary>
        /// Remote procedure
        /// </summary>
        /// <param name="req"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<byte[]> Rpc(byte[] req, int timeout)
        {
            // get identifier
            var id = _dataProcessor.GetIdentifier(req);
            if(id == null)
            {
                _logger.LogError("Identifier not found.");
                return null;
            }

            // send
            return await _dataSynchronizator.ExecuteAction(id, timeout, () =>
            {
                bool status = _socketClient?.Send(req).GetAwaiter().GetResult() ?? false;
                if (!status)
                {
                    _logger.LogError("Failed to sent message.");
                }
                return status;
            });
        }

        /// <summary>
        /// Connect to server
        /// </summary>
        public async Task<bool> Connect()
        {
            Interlocked.Exchange(ref _connecting, new object());

            // create new client
            var socket = new TcpClientSocket(_createDataProcessorFunc, _clientConfig, _loggerFactory);
            socket.ClientDisconnectedAction = new Func<ClientInfo, Task>(async (info) => {
                Interlocked.Exchange(ref _socketClient, null)?.Disconnect();
                if (_eventHandler != null)
                {
                    await _eventHandler.HandleClientDisconnected(info);
                }
            });

            // replace with previous client 
            Interlocked.Exchange(ref _socketClient, socket)?.Disconnect();
 
            // reset utils
            _dataSynchronizator.Reset();
            _dataProcessor.Reset();
            _allowAutoReconnect = true;

            // add support for rpc
            socket.MsgReceivedAction = new Func<DataContainer, Task>(async (msg) => {
                var id = _dataProcessor.GetIdentifier(msg.Payload);
                if (id != null)
                {
                    _dataSynchronizator.NotifyResult(id, msg.Payload);
                }
                if(_eventHandler != null)
                {
                    await _eventHandler.HandleReceivedData(msg);
                }
            });

            // bind other events
            if (_eventHandler != null) {
                socket.ClientConnectedAction = _eventHandler.HandleClientConnected;
                socket.ClientConnectionFailureAction = _eventHandler.HandleClientConnectionFailure;
                socket.MsgSentAction = _eventHandler.HandleSentData;
            }

            // connect
            bool status =  await socket.Connect();

            // cleanup on failure
            if(!status)
            {
                Interlocked.Exchange(ref _socketClient, null)?.Disconnect();
            }

            // return status 
            Interlocked.Exchange(ref _connecting, null);
            return status;
            
        }

        /// <summary>
        /// Disconnects client
        /// </summary>
        public void Disconnect()
        {
            _allowAutoReconnect = false;
            var prevSocket = Interlocked.Exchange(ref _socketClient, null);
            prevSocket?.Disconnect();
        }

        /// <summary>
        /// Client connected status
        /// </summary>
        public bool Connected => _socketClient?.Conneted == true;

        /// <summary>
        /// Dispose service
        /// </summary>
        public void Dispose()
        {
            Disconnect();
            _dataSynchronizator.Dispose();
            _timer.Dispose();
        }

        /// <summary>
        /// Manage autoconnect 
        /// </summary>
        private void ManageAutoConnect()
        {
            if(_clientConfig.EnableAutoConnect)
            {
                _timer = new Timer((o) => {
                    if (_connecting == null && _allowAutoReconnect && _socketClient == null)
                    {
                        Connect().GetAwaiter().GetResult();
                    }
                }, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5));
            }
        }
    }
}
