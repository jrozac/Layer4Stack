using Layer4Stack.DataProcessors.Interfaces;
using Layer4Stack.Handlers.Interfaces;
using Layer4Stack.Models;
using Layer4Stack.Services.Base;
using Layer4Stack.Services.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace Layer4Stack.Services
{

    /// <summary>
    /// Tcp server service
    /// </summary>
    public class TcpServerService : TcpServiceBase, IServerService
    {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dataProcessor"></param>
        public TcpServerService(IDataProcessorProvider dataProcessorProvider, IServerEventHandler eventHandler, ServerConfig serverConfig) : base(dataProcessorProvider)
        {
            ServerConfig = serverConfig;
            EventHandler = eventHandler;
        }

        /// <summary>
        /// Socket server
        /// </summary>
        private TcpServerSocket _socketServer;


        /// <summary>
        /// Event handler
        /// </summary>
        protected IServerEventHandler EventHandler { get; set; }


        /// <summary>
        /// Server config 
        /// </summary>
        protected ServerConfig ServerConfig { get; set; }

        
        /// <summary>
        /// Server start status
        /// </summary>
        public bool Started
        {
            get
            {
                return _socketServer != null ? _socketServer.Started : false;
            }
        }


        /// <summary>
        /// Disconnect client
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public bool DisconnectClient(string clientId)
        {
            return _socketServer != null ? _socketServer.DisconnectClient(clientId) : false;
        }


        /// <summary>
        /// Send data to all clients
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public int SendToAll(byte[] data)
        {
            return _socketServer != null ? _socketServer.SendToAll(data) : 0;

        }


        /// <summary>
        /// Send data to selected client
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool SendToClient(string clientId, byte[] data)
        {
            return _socketServer != null ? _socketServer.SendMessageToClient(clientId, data) : false;
        }


        /// <summary>
        /// Starts server
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            if(_socketServer == null || !_socketServer.Started)
            {
                // set cancellation token source
                CancellationTokenSource = new CancellationTokenSource();

                // init socket
                _socketServer = new TcpServerSocket(DataProcessorProvider, ServerConfig);

                // bind started/failed to start to get start status feedback
                bool startStatus = false;
                bool startExecuted = false;
                _socketServer.ServerStartedEvent += (sender, msg) =>
                {
                    startStatus = true;
                    startExecuted = true;
                };
                _socketServer.ServerStartFailureEvent += (sender, msg) =>
                {
                    startExecuted = true;
                };

                // bind events
                if (EventHandler != null)
                {

                    // client connected
                    _socketServer.ClientConnectedEvent += (sender, client) => {
                        EventHandler.HandleClientConnected(this, client);
                    };

                    // client disconnected
                    _socketServer.ClientDisconnectedEvent += (sender, client) =>
                    {
                        EventHandler.HandleClientDisconnected(this, client);
                    };

                    // server started
                    _socketServer.ServerStartedEvent += (sender, msg) =>
                    {
                        EventHandler.HandleServerStarted(this, ServerConfig);
                    };

                    // server stopped
                    _socketServer.ServerStoppedEvent += (sender, msg) =>
                    {
                        EventHandler.HandleServerStopped(this, ServerConfig);
                    };

                    // server failed to start
                    _socketServer.ServerStartFailureEvent += (sender, msg) =>
                    {
                        EventHandler.HandleServerStartFailure(this, ServerConfig);
                    };

                    // message received
                    _socketServer.MsgReceivedEvent += (sender, msg) =>
                    {
                        EventHandler.HandleReceivedData(this, msg);
                    };

                    // message sent
                    _socketServer.MsgSentEvent += (sender, msg) =>
                    {
                        EventHandler.HandleSentData(this, msg);
                    };

                }

                // try to start the server
                #pragma warning disable
                // start on socket
                Task.Run(() => {
                    _socketServer.ServerStart(CancellationTokenSource);
                });
                #pragma warning restore

                // wait for start execution to complete 
                Task.Run(() => { while (!startExecuted) ; }).Wait();

                // return start status 
                return startStatus;

            }

            // not started 
            return false;

        }


        /// <summary>
        /// Stops server
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            if(_socketServer != null)
            {
                bool started = _socketServer.Started;
                _socketServer.ServerStop();
                _socketServer = null;
                return started;
            }

            return false;
        }


    }
}
