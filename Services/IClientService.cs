using Layer4Stack.DataProcessors;
using Layer4Stack.Models;

namespace Layer4Stack.Services
{

    /// <summary>
    /// Client service 
    /// </summary>
    public interface IClientService
    {

        /// <summary>
        /// Data processor config
        /// </summary>
        IDataProcessorConfig DataProcessorConfig { get; set; }


        /// <summary>
        /// Client config 
        /// </summary>
        ClientConfigModel ClientConfig { get; set; }


        /// <summary>
        /// Connect to server
        /// </summary>
        void Connect();


        /// <summary>
        /// Disconnects from server
        /// </summary>
        void Disconnect();


        /// <summary>
        /// Send data to server 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        bool Send(byte[] data);


        /// <summary>
        /// Connected status
        /// </summary>
        bool Connected { get; }

    }
}
