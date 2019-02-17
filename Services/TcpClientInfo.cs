using Layer4Stack.DataProcessors;
using Layer4Stack.Models;
using System.Net.Sockets;
using System.Threading;

namespace Layer4Stack.Services
{

    /// <summary>
    /// Model used to store data of a client connected to tcp server. 
    /// </summary>
    internal class TcpClientInfo
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
        /// Canellation token source to stop client handler and disconnect client from server.
        /// </summary>
        public CancellationTokenSource ClientHandlerTokenSource { get; set; }


        /// <summary>
        /// Data Processor
        /// </summary>
        public IDataProcessor DataProcessor { get; set; }


    }
}
