using System;
using System.Threading.Tasks;

namespace Layer4Stack.Services
{

    /// <summary>
    /// Client service 
    /// </summary>
    public interface IClientService : IDisposable
    {

        /// <summary>
        /// Connect to server
        /// </summary>
        Task<bool> Connect();

        /// <summary>
        /// Disconnects from server
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Send data to server 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        Task<bool> Send(byte[] data);

        /// <summary>
        /// Remote procedure call
        /// </summary>
        /// <param name="req"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        Task<byte[]> Rpc(byte[] req, int timeout);

        /// <summary>
        /// Connected status
        /// </summary>
        bool Connected { get; }

    }
}
