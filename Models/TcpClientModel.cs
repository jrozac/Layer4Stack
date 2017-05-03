using Layer4Stack.DataProcessors;
using System.Net.Sockets;
using System.Threading;

namespace Layer4Stack.Models
{

    /// <summary>
    /// Model used to store data of a client connected to tcp server. 
    /// </summary>
    public class TcpClientModel : ClientInfoModel
    {


        /// <summary>
        /// TCP client object
        /// </summary>
        public TcpClient Client { get; set; }


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
