using Layer4Stack.DataProcessors.Interfaces;
using Layer4Stack.Models;
using Layer4Stack.Services.Base;
using System;
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

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dataProcessor"></param>
        public TcpServerSocket(IDataProcessorProvider dataProcessorProvider, ServerConfig serverConfig) : base(dataProcessorProvider)
        {
            Config = serverConfig;
        }


        /// <summary>
        /// Global cancellation token source
        /// </summary>
        private CancellationTokenSource _globalCancellationTokenSource;


        /// <summary>
        /// Contains connected clients
        /// </summary>
        private Dictionary<string, TcpClientInfo> _clientRepo = new Dictionary<string, TcpClientInfo>();


        /// <summary>
        /// Tcp listener 
        /// </summary>
        private TcpListener _server { get; set; }


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
        protected void RaiseServerStartFailureEvent()
        {
            _logger.Debug(string.Format("Server failed to start on port {0}.", Config.Port));
            var eh = ServerStartFailureEvent;
            if (eh != null)
            {
                eh(this, null);
            }
        }


        /// <summary>
        /// Raises server started
        /// </summary>
        /// <param name="model"></param>
        protected void RaiseServerStartedEvent()
        {
            _logger.Debug(string.Format("Server started on port {0}.", Config.Port));
            var eh = ServerStartedEvent;
            if (eh != null)
            {
                eh(this, null);
            }
        }


        /// <summary>
        /// Raises server stopped
        /// </summary>
        /// <param name="model"></param>
        protected void RaiseServerStoppedEvent()
        {
            _logger.Debug(string.Format("Server stopped on port {0}.", Config.Port));
            var eh = ServerStoppedEvent;
            if (eh != null)
            {
                eh(this, null);
            }
        }


        /// <summary>
        /// Stops the server
        /// </summary>
        public void ServerStop()
        {
            if (_server != null)
            {

                // disconnect existing clients
                if (_clientRepo != null)
                {
                    foreach(var client in _clientRepo.ToList())
                    {
                        client.Value.Client.Close();
                    }
                    _clientRepo = null;
                }

                // cancell all client handlers (for sure)
                _globalCancellationTokenSource.Cancel();

                // stop server
                _server.Stop();
                _server = null;
            }
        }


        /// <summary>
        /// Server started status
        /// </summary>
        public bool Started { get {
            return _server != null;
        } }


        /// <summary>
        /// Sends data to client
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool SendMessageToClient(string clientId, byte[] data)
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
                return SendMessage(client, message);
            }

            return false;
        }


        /// <summary>
        /// Send a message to all clients
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public int SendToAll(byte[] data) {

            int count = 0;
            foreach(string id in _clientRepo.Keys.ToList())
            {
                if(SendMessageToClient(id, data))
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
        /// Tcp server start task
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> ServerStart(CancellationTokenSource cts)
        {

            // start server 
            try
            {
                // Init TCP listener.
                _server = new TcpListener(IPAddress.Parse(Config.IpAddress ?? "127.0.0.1"), Config.Port);

                // clear clients
                _clientRepo = new Dictionary<string, TcpClientInfo>();

                // Start listening for client requests.
                _server.Start();

                // keep canellation token
                _globalCancellationTokenSource = cts;

            } catch(Exception e)
            {
                _logger.Error(string.Format("Server failed to start on port {0}.", Config.Port), e);

                // raise server failed to start
                #pragma warning disable
                Task.Run(() => {
                    RaiseServerStartFailureEvent();
                });
                #pragma warning restore

                // set server to null and return
                _server = null;
                return false;
            }

            try
            {
                #pragma warning disable
                // raise server started
                Task.Run(() => {
                    RaiseServerStartedEvent();
                });
                #pragma warning restore

                // Enter the listening loop.
                while (true)
                {
                   
                    // Wait for client to connect.
                    TcpClient client = await _server.AcceptTcpClientAsync();

                    // set client info
                    TcpClientInfo clientModel = new TcpClientInfo
                    {
                        Time = DateTime.Now,
                        Port = Config.Port,
                        IpAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString(),
                        Id = Guid.NewGuid().ToString(),
                        Client = client,
                        DataProcessor = DataProcessorProvider.New,
                        ClientHandlerTokenSource = new CancellationTokenSource()
                    };

                    #pragma warning disable
                    
                    // trigger connected event
                    Task.Run(() => {
                        RaiseClientConnectedEvent(clientModel);
                    });

                    // Handle client as a separate task
                    Task.Run(() => {
                        HandleClient(clientModel, cts.Token);
                    });

                    #pragma warning restore

                }
            }
            // socket errro
            catch (SocketException)
            {
                _logger.Debug("Server stopped. SocketException exception occured.");
            }
            // Listener was stopped.
            catch(ObjectDisposedException)
            {
                _logger.Debug("Server stopped. ObjectDisposedException exception occured.");
            }
            finally
            {
                if(_server != null)
                {
                    _server.Stop();
                }
            }

            #pragma warning disable
            // server stopped
            Task.Run(() => {
                RaiseServerStoppedEvent();
            });
            #pragma warning restore

            // finished
            return true;
        }


        /// <summary>
        /// Handles a connected client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="clientInfo"></param>
        /// <param name="ct"></param>
        private void HandleClient(TcpClientInfo client, CancellationToken ct)
        {
  
            // add client to repository
            PutClientToRepository(client);

            // continuously reads data (stops here until cancelled
            ReadData(client, new CancellationToken[] { ct, client.ClientHandlerTokenSource.Token });

            // remove client from repository
            RemoveClientFromRepository(client.Id);

        }


        /// <summary>
        /// Gets client from local repository
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        private TcpClientInfo GetClientFromRepository(string clientId)
        {
            return _clientRepo.ContainsKey(clientId) ? _clientRepo[clientId] : null;
        }


        /// <summary>
        /// Puts client to local repository.
        /// </summary>
        /// <param name="client"></param>
        private void PutClientToRepository(TcpClientInfo client)
        {
            if (_clientRepo != null && !_clientRepo.ContainsKey(client.Id))
            {
                _clientRepo.Add(client.Id, client);
            }
        }


        /// <summary>
        /// Removes client from local repository.
        /// </summary>
        /// <param name="clientId"></param>
        private void RemoveClientFromRepository(string clientId)
        {
            if (_clientRepo != null && _clientRepo.ContainsKey(clientId))
            {
                _clientRepo.Remove(clientId);
            }
        }


    }

}
