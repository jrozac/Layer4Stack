using Layer4Stack.Models;
using System.Threading.Tasks;

namespace Layer4Stack.Handlers
{

    /// <summary>
    /// Client event handler interface
    /// </summary>
    public interface IClientEventHandler
    {

        /// <summary>
        /// Client connected
        /// </summary>
        /// <param name="info"></param>
        Task HandleClientConnected(ClientInfo info);

        /// <summary>
        /// Client connection failure
        /// </summary>
        /// <param name="info"></param>
        Task HandleClientConnectionFailure(ClientInfo info);

        /// <summary>
        /// Client disconnected
        /// </summary>
        /// <param name="info"></param>
        Task HandleClientDisconnected(ClientInfo info);

        /// <summary>
        /// Message received
        /// </summary>
        /// <param name="data"></param>
        Task HandleReceivedData(DataContainer data);

        /// <summary>
        /// Message sent
        /// </summary>
        /// <param name="data"></param>
        Task HandleSentData(DataContainer data);


    }
}
