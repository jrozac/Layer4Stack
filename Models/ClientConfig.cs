namespace Layer4Stack.Models
{

    /// <summary>
    /// Client config model
    /// </summary>
    public class ClientConfig : ConfigBase
    {
        /// <summary>
        /// Constructor with properties
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="enableAutoConnect"></param>
        /// <param name="bufferSize"></param>
        public ClientConfig(string address, int port, bool enableAutoConnect = false, int? bufferSize = null) : 
            base(address, port, bufferSize)
        {
            EnableAutoConnect = enableAutoConnect;
        }

        /// <summary>
        /// Enable auto connect. If set to true, client will reconnect on disconnected
        /// </summary>
        public bool EnableAutoConnect { get; set; }
    }
}
