using Layer4Stack.Models;
using Layer4Stack.Services;

namespace Layer4Stack.Handlers
{

    /// <summary>
    /// Event handler base interface
    /// </summary>
    public interface IEventHandlerBase<T> where T : IServiceBase
    {

        /// <summary>
        /// Handles client connected
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="info"></param>
        void HandleClientConnected(T senderObj, ClientInfoModel info);


        /// <summary>
        /// Handles client disconeccted
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="info"></param>
        void HandleClientDisconnected(T senderObj, ClientInfoModel info);


        /// <summary>
        /// Handles when a message is received
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="data"></param>
        void HandleReceivedData(T senderObj, DataModel data);


        /// <summary>
        /// Handles when a message is sent
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="data"></param>
        void HandleSentData(T senderObj, DataModel data);


    }
}
