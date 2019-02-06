using Layer4Stack.DataProcessors;
using Layer4Stack.Handlers;
using Layer4Stack.Models;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Layer4Stack.Services
{

    /// <summary>
    /// Tcp client service
    /// </summary>
    public class TcpClientService : TcpServiceBase, IClientService
    {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dataProcessor"></param>
        public TcpClientService(IDataProcessorProvider dataProcessorProvider, IClientEventHandler eventHandler, 
            ClientConfig clientConfig, ILoggerFactory loggerFactory) : base(dataProcessorProvider, loggerFactory)
        {
            ClientConfig = clientConfig;
            EventHandler = eventHandler;
        }


        /// <summary>
        /// Socket client
        /// </summary>
        private TcpClientSocket _socketClient;


        /// <summary>
        /// Client config
        /// </summary>
        protected ClientConfig ClientConfig { get; set; }


        /// <summary>
        /// Message handler
        /// </summary>
        protected IClientEventHandler EventHandler { get; set; }


        /// <summary>
        /// Sends data
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool Send(byte[] msg)
        {
            return _socketClient != null ? _socketClient.Send(msg) : false;
        }

        /// <summary>
        /// Remote procedure
        /// </summary>
        /// <param name="req"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public byte[] Rpc(byte[] req, int timeout)
        {
            // get identifier
            var id = DataProcessor.GetIdentifier(req);
            if(id == null)
            {
                Logger.LogError("Identifier not found.");
                return null;
            }

            // send 
            var rsp = DataSynchronizator.ExecuteAction(id, timeout, () => {
                bool status = _socketClient != null ? _socketClient.Send(req) : false;
                if (!status)
                {
                    Logger.LogError("Failed to sent message.");
                }
                return status;
            });

            // return response
            return rsp;
        }

        /// <summary>
        /// Connect to server
        /// </summary>
        public void Connect()
        {

            // set cancellation token store
            CancellationTokenSource = new CancellationTokenSource();

            // setup socket server
            _socketClient = new TcpClientSocket(DataProcessorProvider, ClientConfig, LoggerFactory);

            // bind event handling 
            if(EventHandler != null) { 

                // client connected
                _socketClient.ClientConnectedEvent += (sender, client) => {
                    EventHandler.HandleClientConnected(this, client);
                };

                // client disconnected
                _socketClient.ClientDisconnectedEvent += (sender, client) =>
                {
                    EventHandler.HandleClientDisconnected(this, client);
                };

                // client connect failure
                _socketClient.ClientConnectionFailureEvent += (sender, msg) =>
                {
                    EventHandler.HandleClientConnectionFailure(this, msg);
                };

                // message received
                _socketClient.MsgReceivedEvent += (sender, msg) =>
                {
                    var id = DataProcessor.GetIdentifier(msg.Payload);
                    if(id != null)
                    {
                        DataSynchronizator.NotifyResult(id, msg.Payload);
                    }
                    EventHandler.HandleReceivedData(this, msg);
                };

                // message sent
                _socketClient.MsgSentEvent += (sender, msg) =>
                {
                    EventHandler.HandleSentData(this, msg);
                };

            }

            // connect
            #pragma warning disable
            Task.Run(() => {
                _socketClient.Connect(CancellationTokenSource.Token);
            });
            #pragma warning restore 

        }


        /// <summary>
        /// Disconnects client
        /// </summary>
        public void Disconnect()
        {

            if (CancellationTokenSource != null)
            {
                CancellationTokenSource.Cancel();
            }
            if(_socketClient != null)
            {
                _socketClient.Disconnect();
            }

        }


        /// <summary>
        /// Client connected status
        /// </summary>
        public bool Connected { get {

            return _socketClient != null ? _socketClient.Conneted : false;
        } }

        /// <summary>
        /// Dispose service
        /// </summary>
        public new void Dispose()
        {
            Disconnect();
            base.Dispose();
        }
    }
}
