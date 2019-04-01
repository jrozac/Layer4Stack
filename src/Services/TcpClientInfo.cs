using Layer4Stack.DataProcessors;
using Layer4Stack.Models;
using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Layer4Stack.Services
{

    /// <summary>
    /// Model used to store data of a client connected to tcp server. 
    /// </summary>
    internal class TcpClientInfo : IDisposable
    {

        /// <summary>
        /// TCP client object
        /// </summary>
        public TcpClient Client { get; set; }

        /// <summary>
        /// Client info 
        /// </summary>
        public ClientInfo Info {get;set;}

        /// <summary>
        /// Data Processor
        /// </summary>
        public IDataProcessor DataProcessor { get; set; }

        /// <summary>
        /// Dispose 
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Dispose()
        {
            if (Client != null)
            {
                try
                {
                    Client.GetStream().Close();
                } catch(Exception) { }
                Client.Close();
                Client.Dispose();
                Client = null;
            }
        }

    }
}
