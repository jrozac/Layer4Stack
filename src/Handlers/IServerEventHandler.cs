using Layer4Stack.Models;

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
        void HandleClientConnected(ClientInfo info);

        /// <summary>
        /// Client disconnected
        /// </summary>
        /// <param name="info"></param>
        void HandleClientDisconnected(ClientInfo info);

        /// <summary>
        /// Message received
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        byte[] HandleReceivedData(DataContainer data);

        /// <summary>
        /// Message sent
        /// </summary>
        /// <param name="data"></param>
        void HandleSentData(DataContainer data);

        /// <summary>
        /// Server started
        /// </summary>
        /// <param name="config"></param>
        void HandleServerStarted(ServerConfig config);

        /// <summary>
        /// Server failed to start
        /// </summary>
        /// <param name="config"></param>
        void HandleServerStartFailure(ServerConfig config);

        /// <summary>
        /// Server stopped
        /// </summary>
        /// <param name="config"></param>
        void HandleServerStopped(ServerConfig config);

    }
}
