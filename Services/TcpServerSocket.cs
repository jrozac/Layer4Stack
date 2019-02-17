using Layer4Stack.DataProcessors;
using Layer4Stack.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Layer4Stack.Services
{

    /// <summary>
    /// Socket server
    /// </summary>
    internal class TcpServerSocket : TcpSocketBase<ServerConfig>
    {

        #region vars

        /// <summary>
        /// Locker
        /// </summary>
        private readonly object _locker = new object();

        /// <summary>
        /// Contains connected clients
        /// </summary>
        private ConcurrentDictionary<string, TcpClientInfo> _clientRepo = new ConcurrentDictionary<string, TcpClientInfo>();

        /// <summary>
        /// Global cancellation token source
        /// </summary>
        private CancellationTokenSource _serverCancellationTokenSource;

        /// <summary>
        /// Tcp listener 
        /// </summary>
        private TcpListener _server { get; set; }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="createDataProcessorFunc"></param>
        /// <param name="loggerFactory"></param>
        public TcpServerSocket(Func<IDataProcessor> createDataProcessorFunc, ServerConfig serverConfig, 
            ILoggerFactory loggerFactory) : base(serverConfig, createDataProcessorFunc, loggerFactory)
        {
        }

        #region events

        /// <summary>
        /// Server start failure event
        /// </summary>
        public event EventHandler ServerStartFailureEvent;

        /// <summary>
        /// Server start failure event
        /// </summary>
        public event EventHandler ServerStartedEvent;

        /// <summary>
        /// Server stopped event
        /// </summary>
        public event EventHandler ServerStoppedEvent;

        /// <summary>
        /// Raises server start failure
        /// </summary>
        /// <param name="model"></param>
        private async Task<bool> RaiseServerStartFailureEvent()
        {
            _logger.LogDebug("Server failed to start on port {port}.", _config.Port);
            ServerStartFailureEvent?.Invoke(this, null);
            return await Task.FromResult(true);
        }

        /// <summary>
        /// Raises server started
        /// </summary>
        /// <param name="model"></param>
        private async Task<bool> RaiseServerStartedEvent()
        {
            _logger.LogDebug("Server started on port {port}.", _config.Port);
            ServerStartedEvent?.Invoke(this, null);
            return await Task.FromResult(true);
        }

        /// <summary>
        /// Raises server stopped
        /// </summary>
        /// <param name="model"></param>
        private async Task<bool> RaiseServerStoppedEvent()
        {
            _logger.LogDebug("Server stopped on port {port}.", _config.Port);
            ServerStoppedEvent?.Invoke(this, null);
            return await Task.FromResult(true);
        }

        #endregion

        /// <summary>
        /// Server started status
        /// </summary>
        public bool Started => _server != null;

        /// <summary>
        /// Gets clients 
        /// </summary>
        public IList<ClientInfo> Clients => _clientRepo?.Values?.Select(v => v.Info).ToList();

        /// <summary>
        /// Sends data to client
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> SendMessageToClient(string clientId, byte[] data)
        {
            TcpClientInfo client = GetClientFromRepository(clientId);
            if(client != null)
            {
                DataContainer message = new DataContainer
                {
                    ClientId = clientId,
                    Payload = data,
                    Time = DateTime.Now
                };
                return await SendMessage(client, message);
            }

            return false;
        }

        /// <summary>
        /// Send a message to all clients
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<int> SendToAll(byte[] data) {

            int count = 0;
            foreach(string id in _clientRepo.Keys.ToList())
            {
                if(await SendMessageToClient(id, data))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Disconnects a client
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public bool DisconnectClient(string clientId)
        {
            TcpClientInfo client = GetClientFromRepository(clientId);
            if(client != null)
            {
                client.ClientHandlerTokenSource.Cancel();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Stops the server
        /// </summary>
        public void ServerStop()
        {
            // fire servers stop
            _serverCancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Tcp server start task
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> ServerStart()
        {

            // start status 
            bool status = false;

            // try to start server 
            lock (_locker)
            {

                // already started 
                if (_server != null)
                {
                    _logger.LogInformation("Server already started.");
                    return true;
                } else
                {

                    // Init TCP listener
                    _server = _config.IpAddress == null ? TcpListener.Create(_config.Port) : new TcpListener(IPAddress.Parse(_config.IpAddress), _config.Port);

                    // create global cancellation token 
                    _serverCancellationTokenSource = new CancellationTokenSource();

                    // clear clients
                    _clientRepo.Clear();

                    // start server 
                    try
                    {
                        // Start listening for client requests.
                        _server.Start();

                        // set started 
                        status = true;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Server failed to start on port {port}. Exception: {exception}", _config.Port, e.Message);

                        // cleanup
                        _server.Stop();
                        _server = null;
                        _serverCancellationTokenSource?.Cancel();
                    }

                }
            }

            // raise status event
            if(status)
            {
                RaiseServerStartedEvent();
            } else
            {
                RaiseServerStartFailureEvent();
            }

            // wait for clients (keep it run in background)
            if(status)
            {
                WaitForClientsLoop();
            }

            // finished
            return await Task.FromResult(status);
        }

        /// <summary>
        /// Wait for clients loop
        /// </summary>
        /// <returns></returns>
        private async Task WaitForClientsLoop()
        {

            try
            {

                // Enter the listening loop.
                while (!_serverCancellationTokenSource.Token.IsCancellationRequested)
                {

                    // Wait for client to connect.
                    TcpClient client = await _server.AcceptTcpClientAsync();

                    // set client info
                    TcpClientInfo clientModel = new TcpClientInfo
                    {
                        Info = new ClientInfo
                        {
                            Time = DateTime.Now,
                            Port = _config.Port,
                            IpAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString(),
                            Id = Guid.NewGuid().ToString()
                        },
                        Client = client,
                        DataProcessor = _createDataProcessorFunc(),
                        ClientHandlerTokenSource = new CancellationTokenSource()
                    };

                    // Handle client in background
                    HandleClient(clientModel, _serverCancellationTokenSource.Token);

                }
            }
            // socket errro
            catch (SocketException)
            {
                _logger.LogWarning("Server stopped. SocketException exception occured.");
            }
            // Listener was stopped.
            catch (ObjectDisposedException)
            {
                _logger.LogWarning("Server stopped. ObjectDisposedException exception occured.");
            }
            finally
            {
                lock (_locker)
                {
                    // disconnect client 
                    _clientRepo.Values.ToList().ForEach(client =>
                    {
                        client.Client.Close();
                    });
                    _clientRepo.Clear();

                    // stop server
                    _server?.Stop();
                    _server = null;
                }

                // log stop
                _logger.LogInformation("Server stopped.");
            }

            // server stopped
            RaiseServerStoppedEvent();

        }

        /// <summary>
        /// Handles a connected client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="clientInfo"></param>
        /// <param name="ct"></param>
        private async Task HandleClient(TcpClientInfo client, CancellationToken ct)
        {

            // trigger connected event
            RaiseClientConnectedEvent(client.Info);

            // add client to repository
            PutClientToRepository(client);

            // continuously reads data (stops here until cancelled
            await ReadData(client);

            // remove client from repository
            RemoveClientFromRepository(client.Info.Id);

            // raise clinet disconnected
            RaiseClientDisconnectedEvent(client.Info);

        }

        /// <summary>
        /// Gets client from local repository
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        private TcpClientInfo GetClientFromRepository(string clientId)
        {
            TcpClientInfo client = null;
            _clientRepo.TryGetValue(clientId, out client);
            return client;
        }

        /// <summary>
        /// Puts client to local repository.
        /// </summary>
        /// <param name="client"></param>
        private void PutClientToRepository(TcpClientInfo client)
        {
            _clientRepo.TryAdd(client.Info.Id, client);
        }

        /// <summary>
        /// Removes client from local repository.
        /// </summary>
        /// <param name="clientId"></param>
        private void RemoveClientFromRepository(string clientId)
        {
            TcpClientInfo client = null;
            _clientRepo.TryRemove(clientId, out client);
        }

    }

}
