using Layer4Stack.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Layer4Stack.Services
{

    /// <summary>
    /// Server service
    /// </summary>
    public interface IServerService : IDisposable
    {

        /// <summary>
        /// Disconnects client
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        bool DisconnectClient(string clientId);

        /// <summary>
        /// Send data to client async
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        Task<bool> SendToClientAsync(string clientId, byte[] data);

        /// <summary>
        /// Send data to client 
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        bool SendToClient(string clientId, byte[] data);

        /// <summary>
        /// Start status 
        /// </summary>
        bool Started { get; }

        /// <summary>
        /// Start server async
        /// </summary>
        /// <returns></returns>
        Task<bool> StartAsync();

        /// <summary>
        /// Start server
        /// </summary>
        /// <returns></returns>
        bool Start();

        /// <summary>
        /// Stop server
        /// </summary>
        /// <returns></returns>
        void Stop();

        /// <summary>
        /// Send data to all clients async
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        Task<int> SendToAllAsync(byte[] data);

        /// <summary>
        /// Send data to all clients
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        int SendToAll(byte[] data);

        /// <summary>
        /// Clients
        /// </summary>
        IList<ClientInfo> Clients { get; }

    }
}
