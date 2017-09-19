using Layer4Stack.DataProcessors.Interfaces;
using Layer4Stack.Models;
using Layer4Stack.Services.Base;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Layer4Stack.Services
{

    /// <summary>
    /// Tcp socket client
    /// </summary>
    internal class TcpClientSocket : TcpSocketBase<ClientConfig>
    {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dataProcessor"></param>
        public TcpClientSocket(IDataProcessorProvider dataProcessorProvider, ClientConfig clientConfig) : base(dataProcessorProvider)
        {
            Config = clientConfig;
        }

        /// <summary>
        /// Client
        /// </summary>
        private TcpClientInfo _client;


        /// <summary>
        /// Fired when client fails to connect to server
        /// </summary>
        public event EventHandler<ClientInfo> ClientConnectionFailureEvent;


        /// <summary>
        /// Raises client connection failure event
        /// </summary>
        /// <param name="model"></param>
        protected void RaiseClientConnectionFailureEvent(ClientInfo model)
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
            TcpClientInfo clientInfo = new TcpClientInfo
            {
                Time = DateTime.Now,
                Port = Config.Port,
                IpAddress = Config.IpAddress,
                Id = Guid.NewGuid().ToString(),
                Client = client,
                DataProcessor = DataProcessorProvider.New
            };


            try
            {
                await client.ConnectAsync(Config.IpAddress, Config.Port);
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
        private void HandleClient(TcpClientInfo client, CancellationToken ct)
        {

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

            // create model to send 
            DataContainer model = new DataContainer
            {
                ClientId = _client.Id,
                Payload = message,
                Time = DateTime.Now
            };

            // send
            return SendMessage(_client, model);
        }


    }
}
