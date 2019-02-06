using Layer4Stack.DataProcessors;
using Layer4Stack.Handlers;
using Layer4Stack.Models;
using Layer4Stack.Services;
using Microsoft.Extensions.Logging;
using System;

namespace Layer4Stack.Api
{

    /// <summary>
    /// Client api
    /// </summary>
    /// <typeparam name="TDataProcessorConfig"></typeparam>
    public class ClientApi<TDataProcessorProvider, TDataProcessor>  : IClientApi
        where TDataProcessorProvider : IDataProcessorProvider
        where TDataProcessor : DataProcessorConfigBase
    {

        /// <summary>
        /// Logger factory 
        /// </summary>
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Inits client api
        /// </summary>
        /// <param name="clientConfig"></param>
        /// <param name="dataProcessorConfig"></param>
        /// <param name="eventHandler"></param>
        public ClientApi(ClientConfig clientConfig, TDataProcessor dataProcessorConfig, 
            IClientEventHandler eventHandler, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;

            // create data processor provider 
            IDataProcessorProvider dataProcessorProvider = (TDataProcessorProvider) Activator.CreateInstance(typeof(TDataProcessorProvider), new object[] { dataProcessorConfig });

            // create client 
            Client = new TcpClientService(dataProcessorProvider, eventHandler, clientConfig, _loggerFactory);

        }


        /// <summary>
        /// Client
        /// </summary>
        protected TcpClientService Client { get; set; }


        /// <summary>
        /// Connecto
        /// </summary>
        public void Connect()
        {
            Client.Connect();
        }


        /// <summary>
        /// Disconnect
        /// </summary>
        public void Disconnect()
        {
            Client.Disconnect();
        }


        /// <summary>
        /// Send data 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool Send(byte[] data)
        {
            return Client.Send(data);
        }

        /// <summary>
        /// Call rpc
        /// </summary>
        /// <param name="req"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public byte[] Rpc(byte[] req, int timeout)
        {
            return Client.Rpc(req, timeout);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Client.Dispose();
        }

        /// <summary>
        /// Connection status 
        /// </summary>
        public bool Connected
        {
            get
            {
                return Client.Connected;
            }
        }

    }
}
