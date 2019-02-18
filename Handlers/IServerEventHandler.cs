using Layer4Stack.Models;
using Layer4Stack.Services;
using System.Threading.Tasks;

namespace Layer4Stack.Handlers.Interfaces
{

    /// <summary>
    /// Server event handler interface
    /// </summary>
    public interface IServerEventHandler
    {

        /// <summary>
        /// Client connected
        /// </summary>
        /// <param name="info"></param>
        Task HandleClientConnected(ClientInfo info);

        /// <summary>
        /// Client disconnected
        /// </summary>
        /// <param name="info"></param>
        Task HandleClientDisconnected(ClientInfo info);

        /// <summary>
        /// Received message 
        /// </summary>
        /// <param name="data"></param>
        Task HandleReceivedData(DataContainer data);

        /// <summary>
        /// Message sent
        /// </summary>
        /// <param name="data"></param>
        Task HandleSentData(DataContainer data);

        /// <summary>
        /// Server started
        /// </summary>
        /// <param name="config"></param>
        Task HandleServerStarted(ServerConfig config);

        /// <summary>
        /// Server failed to start
        /// </summary>
        /// <param name="config"></param>
        Task HandleServerStartFailure(ServerConfig config);

        /// <summary>
        /// Server stopped
        /// </summary>
        /// <param name="config"></param>
        Task HandleServerStopped(ServerConfig config);

    }
}
