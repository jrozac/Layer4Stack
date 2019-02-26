using Layer4Stack.DataProcessors;
using Layer4Stack.Models;
using Layer4Stack.Utils;
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
        /// Contains connected clients
        /// </summary>
        private ConcurrentDictionary<string, TcpClientInfo> _clientRepo = new ConcurrentDictionary<string, TcpClientInfo>();

        /// <summary>
        /// Tcp listener 
        /// </summary>
        private TcpListener _server;

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
        public Action<ServerConfig> ServerStartFailureAction;

        /// <summary>
        /// Server start failure event
        /// </summary>
        public Action<ServerConfig> ServerStartedAction;

        /// <summary>
        /// Server stopped event
        /// </summary>
        public Action<ServerConfig> ServerStoppedAction;

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
            TcpClientInfo client = RemoveClientFromRepository(clientId);
            client?.Dispose();
            return client != null;
        }

        /// <summary>
        /// Stops the server
        /// </summary>
        public void ServerStop()
        {
            // fire servers stop
            var srv = Interlocked.Exchange(ref _server, null);
            try
            {
                srv?.Stop();
            } catch(SocketException)
            {
                _logger.LogError("Socket exception.");
            }
        }

        /// <summary>
        /// Tcp server start task
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> ServerStart()
        {

            // already started 
            if (_server != null)
            {
                _logger.LogInformation("Server already started.");
                return false;
            }

            // clar clients repository
            _clientRepo.Clear();

            // Init TCP listener
            var server = _config.IpAddress == null ? TcpListener.Create(_config.Port) : new TcpListener(IPAddress.Parse(_config.IpAddress), _config.Port);

            // start server 
            bool status = false;
            try
            {
                // Start listening for client requests.
                server.Start();

                // set started 
                status = true;
            }
            catch (Exception e)
            {
                _logger.LogError("Server failed to start on port {port}. Exception: {exception}", _config.Port, e.Message);
            }

            // set server 
            Interlocked.Exchange(ref _server, server);

            // raise status event
            if(status)
            {
                TaskUtil.RunAction(() => ServerStartedAction?.Invoke(_config), _logger);
            } else
            {
                TaskUtil.RunAction(() => ServerStartFailureAction?.Invoke(_config), _logger);
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
                while (true)
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
                    };

                    // Handle client in background
                    HandleClient(clientModel);

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
            } catch(InvalidOperationException)
            {
                _logger.LogWarning("Invalid operation exception.");
            }
            finally
            {

                // stop server
                try
                {
                    TcpListener tcpListener = null;
                    var srv = Interlocked.Exchange(ref _server, tcpListener);
                    srv?.Stop();
                } catch(SocketException)
                {
                }

                // disconnect client 
                var clients = _clientRepo.Values.ToList();
                _clientRepo.Clear();
                clients.ForEach(c => {
                    try
                    {
                        c.Dispose();
                    } catch(Exception)
                    {
                    }
                });
 
                // log stop
                _logger.LogInformation("Server stopped.");
            }

            // server stopped
            TaskUtil.RunAction(() => ServerStoppedAction?.Invoke(_config), _logger);

        }

        /// <summary>
        /// Handles a connected client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="clientInfo"></param>
        private async Task HandleClient(TcpClientInfo client)
        {

            // trigger connected event
            TaskUtil.RunAction(() => ClientConnectedAction?.Invoke(client.Info), _logger);

            // add client to repository
            PutClientToRepository(client);

            // continuously reads data (stops here until cancelled
            await ReadData(client);

            // remove client from repository
            RemoveClientFromRepository(client.Info.Id);

            // raise clinet disconnected
            TaskUtil.RunAction(() => ClientDisconnectedAction?.Invoke(client.Info), _logger);

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
        /// <returns></returns>
        private TcpClientInfo RemoveClientFromRepository(string clientId)
        {
            TcpClientInfo client = null;
            _clientRepo.TryRemove(clientId, out client);
            return client;
        }

    }

}