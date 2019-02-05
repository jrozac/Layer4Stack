using Layer4Stack.Models;
using Layer4Stack.Services;

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
        /// <param name="senderObj"></param>
        /// <param name="info"></param>
        void HandleClientConnected(IClientService senderObj, ClientInfo info);


        /// <summary>
        /// Client connection failure
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="info"></param>
        void HandleClientConnectionFailure(IClientService senderObj, ClientInfo info);


        /// <summary>
        /// Client disconnected
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="info"></param>
        void HandleClientDisconnected(IClientService senderObj, ClientInfo info);


        /// <summary>
        /// Message received
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="data"></param>
        void HandleReceivedData(IClientService senderObj, DataContainer data);


        /// <summary>
        /// Message sent
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="data"></param>
        void HandleSentData(IClientService senderObj, DataContainer data);


    }
}
