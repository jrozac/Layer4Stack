using Layer4Stack.Models;
using Layer4Stack.Services;

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
        /// <param name="senderObj"></param>
        /// <param name="info"></param>
        void HandleClientConnected(IServerService senderObj, ClientInfo info);


        /// <summary>
        /// Client disconnected
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="info"></param>
        void HandleClientDisconnected(IServerService senderObj, ClientInfo info);


        /// <summary>
        /// Received message 
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="data"></param>
        void HandleReceivedData(IServerService senderObj, DataContainer data);


        /// <summary>
        /// Message sent
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="data"></param>
        void HandleSentData(IServerService senderObj, DataContainer data);


        /// <summary>
        /// Server started
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="config"></param>
        void HandleServerStarted(IServerService senderObj, ServerConfig config);


        /// <summary>
        /// Server failed to start
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="config"></param>
        void HandleServerStartFailure(IServerService senderObj, ServerConfig config);


        /// <summary>
        /// Server stopped
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="config"></param>
        void HandleServerStopped(IServerService senderObj, ServerConfig config);

    }
}
