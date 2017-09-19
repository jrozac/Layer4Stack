namespace Layer4Stack.Services.Interfaces
{

    /// <summary>
    /// Server service
    /// </summary>
    public interface IServerService : IServiceBase
    {

        /// <summary>
        /// Disconnects client
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        bool DisconnectClient(string clientId);


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
        /// Start server
        /// </summary>
        /// <returns></returns>
        bool Start();


        /// <summary>
        /// Stop server
        /// </summary>
        /// <returns></returns>
        bool Stop();


        /// <summary>
        /// Send data to all client
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        int SendToAll(byte[] data);

    }
}
