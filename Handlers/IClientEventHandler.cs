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
        void HandleClientConnected(IClientService senderObj, ClientInfoModel info);


        /// <summary>
        /// Client connection failure
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="info"></param>
        void HandleClientConnectionFailure(IClientService senderObj, ClientInfoModel info);


        /// <summary>
        /// Client disconnected
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="info"></param>
        void HandleClientDisconnected(IClientService senderObj, ClientInfoModel info);


        /// <summary>
        /// Message received
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="data"></param>
        void HandleReceivedData(IClientService senderObj, DataModel data);


        /// <summary>
        /// Message sent
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="data"></param>
        void HandleSentData(IClientService senderObj, DataModel data);


    }
}
