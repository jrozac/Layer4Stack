using Layer4Stack.Handlers;
using Layer4Stack.Models;
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
        /// Socket client
        /// </summary>
        private TcpClientSocket _socketClient;


        /// <summary>
        /// Client config
        /// </summary>
        public ClientConfigModel ClientConfig { get; set; }


        /// <summary>
        /// Message handler
        /// </summary>
        public IClientEventHandler EventHandler { get; set; }


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
        /// Connect to server
        /// </summary>
        public void Connect()
        {

            // set cancellation token store
            _cancellationTokenSource = new CancellationTokenSource();

            // setup socket server
            _socketClient = new TcpClientSocket {
                ClientConfig = ClientConfig,
                DataProcessorConfig = DataProcessorConfig
            };
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
                _socketClient.Connect(_cancellationTokenSource.Token);
            });
            #pragma warning restore 

        }


        /// <summary>
        /// Disconnects client
        /// </summary>
        public void Disconnect()
        {

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
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

    }
}
