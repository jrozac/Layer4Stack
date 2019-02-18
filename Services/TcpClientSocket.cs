using Layer4Stack.DataProcessors;
using Layer4Stack.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Layer4Stack.Services
{

    /// <summary>
    /// Tcp socket client
    /// </summary>
    internal class TcpClientSocket : TcpSocketBase<ClientConfig>
    {

        /// <summary>
        /// Locker
        /// </summary>
        private readonly object _locker = new object();

        /// <summary>
        /// Client
        /// </summary>
        private TcpClientInfo _client;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dataProcessor"></param>
        public TcpClientSocket(Func<IDataProcessor> createDataProcessorFunc, ClientConfig clientConfig, ILoggerFactory loggerFactory) : 
            base(clientConfig, createDataProcessorFunc, loggerFactory)
        {
 
        }

        #region Events

        /// <summary>
        /// Fired when client fails to connect to server
        /// </summary>
        public event EventHandler<ClientInfo> ClientConnectionFailureEvent;

        /// <summary>
        /// Raises client connection failure event
        /// </summary>
        /// <param name="model"></param>
        protected async Task<bool> RaiseClientConnectionFailureEvent(ClientInfo model)
        {
            _logger.LogDebug("Failed to connect to {ip} on port {port}.", model.IpAddress, model.Port);
            ClientConnectionFailureEvent?.Invoke(this, model);
            return await Task.FromResult(true);
        }

        #endregion

        /// <summary>
        /// Connected status
        /// </summary>
        public bool Conneted => _client?.Client?.Connected == true;

        /// <summary>
        /// Connect to server
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<bool> Connect()
        {

            // create client or return 
            lock (_locker)
            {
                if(_client != null)
                {
                    return true;
                } else
                {
                    // init model info
                    _client = new TcpClientInfo
                    {
                        Info = new ClientInfo()
                        {
                            Time = DateTime.Now,
                            Port = _config.Port,
                            IpAddress = _config.IpAddress,
                            Id = Guid.NewGuid().ToString()
                        },
                        Client = new TcpClient(),
                        DataProcessor = _createDataProcessorFunc()
                    };
                }
            }

            // connect 
            try
            {
                await _client.Client.ConnectAsync(_config.IpAddress, _config.Port);
            }
            catch (Exception e)
            {
                _logger.LogError("Connection failed with error: {message}.", e.Message);

                // client connected failed 
                RaiseClientConnectionFailureEvent(_client.Info);

                // failed clean up
                lock(_locker)
                {
                    _client = null;
                }

                // return false
                return false;
            }

            // handle client
            HandleClient();

            // return success
            return true;

        }

        /// <summary>
        /// Disconnects client
        /// </summary>
        public void Disconnect()
        {
            _client?.Client.Close();
        }

        /// <summary>
        /// Sends a message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> Send(byte[] message)
        {

            // client not initialized
            if (_client == null)
            {
                return false;
            }

            // create model to send 
            DataContainer model = new DataContainer
            {
                ClientId = _client.Info.Id,
                Payload = message,
                Time = DateTime.Now
            };

            // send
            return await SendMessage(_client, model);
        }

        /// <summary>
        /// Handles a connected client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="ct"></param>
        private async Task HandleClient()
        {

            // client connected
            RaiseClientConnectedEvent(_client.Info);

            // continuously reads data
            await ReadData(_client);

            // remove client from repository
            lock(_locker)
            {
                _client?.Client?.Close();
                _client = null;
            }

            // trigger diconnected event
            RaiseClientDisconnectedEvent(_client.Info);

        }

    }
}
