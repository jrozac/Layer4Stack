using Layer4Stack.DataProcessors;
using Layer4Stack.Handlers.Interfaces;
using Layer4Stack.Models;
using Layer4Stack.Services;
using Microsoft.Extensions.Logging;
using System;

namespace Layer4Stack.Api
{

    /// <summary>
    /// Server api
    /// </summary>
    /// <typeparam name="TDataProcessorProvider"></typeparam>
    /// <typeparam name="TDataProcessor"></typeparam>
    public class ServerApi<TDataProcessorProvider, TDataProcessor> : IServerApi
        where TDataProcessorProvider : IDataProcessorProvider
        where TDataProcessor : DataProcessorConfigBase
    {

        /// <summary>
        /// Logger factory 
        /// </summary>
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Inits server api
        /// </summary>
        /// <param name="serverConfig"></param>
        /// <param name="dataProcessorConfig"></param>
        /// <param name="eventHandler"></param>
        public ServerApi(ServerConfig serverConfig, TDataProcessor dataProcessorConfig, 
            IServerEventHandler eventHandler, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;

            // create data processor provider 
            IDataProcessorProvider dataProcessorProvider = (TDataProcessorProvider)Activator.CreateInstance(typeof(TDataProcessorProvider), new object[] { dataProcessorConfig });

            // create client 
            Server = new TcpServerService(dataProcessorProvider, eventHandler, serverConfig, loggerFactory);
        }


        /// <summary>
        /// Client
        /// </summary>
        protected TcpServerService Server { get; set; }


        /// <summary>
        /// Started status
        /// </summary>
        public bool Started
        {
            get
            {
                return Server.Started;
            }
        }


        /// <summary>
        /// Disconnects client 
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public bool DisconnectClient(string clientId)
        {
            return Server.DisconnectClient(clientId);
        }


        /// <summary>
        /// Sends data to all clients 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public int SendToAll(byte[] data)
        {
            return Server.SendToAll(data);
        }


        /// <summary>
        /// Sends data to client 
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool SendToClient(string clientId, byte[] data)
        {
            return Server.SendToClient(clientId, data);
        }


        /// <summary>
        /// Starts server 
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            return Server.Start();
        }


        /// <summary>
        /// Stops server 
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            return Server.Stop();
        }
    }
}
