namespace Layer4Stack.Services.Interfaces
{

    /// <summary>
    /// Client service 
    /// </summary>
    public interface IClientService
    {

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
