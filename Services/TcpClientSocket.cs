using Layer4Stack.Models;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Layer4Stack.Services
{

    /// <summary>
    /// Tcp socket client
    /// </summary>
    internal class TcpClientSocket : TcpSocketBase
    {

        /// <summary>
        /// Client
        /// </summary>
        private TcpClientModel _client;


        /// <summary>
        /// Client config
        /// </summary>
        public ClientConfigModel ClientConfig
        {
            get
            {
                return (ClientConfigModel)base._config;
            }
            set
            {
                base._config = value;
            }
        }


        /// <summary>
        /// Fired when client fails to connect to server
        /// </summary>
        public event EventHandler<ClientInfoModel> ClientConnectionFailureEvent;


        /// <summary>
        /// Raises client connection failure event
        /// </summary>
        /// <param name="model"></param>
        protected void RaiseClientConnectionFailureEvent(ClientInfoModel model)
        {
            _logger.Debug(string.Format("Failed to connect to {0} on port {1}.", model.IpAddress, model.Port));
            var eh = ClientConnectionFailureEvent;
            if (eh != null)
            {
                eh(this, model);
            }
        }


        /// <summary>
        /// Connected status
        /// </summary>
        public bool Conneted { get {
            return _client != null && _client.Client.Connected;
        }}


        /// <summary>
        /// Connect to server
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<bool> Connect(CancellationToken ct)
        {

            #pragma warning disable

            // connect client
            TcpClient client = new TcpClient();

            // init model info
            TcpClientModel clientInfo = new TcpClientModel
            {
                Time = DateTime.Now,
                Port = ClientConfig.Port,
                IpAddress = ClientConfig.IpAddress,
                Id = Guid.NewGuid().ToString(),
                Client = client
            };


            try
            {
                await client.ConnectAsync(ClientConfig.IpAddress, ClientConfig.Port);
            } catch(Exception e)
            {
                // client connected failed 
                Task.Run(() => {
                    RaiseClientConnectionFailureEvent(clientInfo);
                });

                // return false
                return false;
            }
             
            // client connected
            Task.Run(() => {
                RaiseClientConnectedEvent(clientInfo);
            });

            // handle client
            Task.Run(() => {
                HandleClient(clientInfo, ct);
            });
          
            // return success
            return true;

            #pragma warning restore

        }


        /// <summary>
        /// Handles a connected client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="ct"></param>
        private void HandleClient(TcpClientModel client, CancellationToken ct)
        {

            // init data processor
            client.DataProcessor = InitDataProcessor();

            // add client to repository
            _client = client;

            // continuously reads data
            ReadData(client, new CancellationToken[] { ct});

            // remove client from repository
            _client = null;

        }


        /// <summary>
        /// Disconnects client
        /// </summary>
        public void Disconnect()
        {
            if(_client != null)
            {
                _client.Client.Close();
            }
        }


        /// <summary>
        /// Sends a message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool Send(byte[] message)
        {

            // client not initialized
            if(_client == null)
            {
                return false;
            }

            DataModel model = new DataModel
            {
                ClientId = _client.Id,
                Payload = message,
                Time = DateTime.Now
            };
            return SendMessage(_client, model);
        }


    }
}
