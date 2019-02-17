namespace Layer4Stack.Models
{

    /// <summary>
    /// Base config model
    /// </summary>
    public abstract class ConfigBase
    {

        /// <summary>
        /// Constructor with properties 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        protected ConfigBase(string address, int port)
        {
            Port = port;
            IpAddress = address;
        }

        /// <summary>
        /// Server port
        /// </summary>
        public int Port { get; protected set; }

        /// <summary>
        /// IP address to start instance on.
        /// </summary>
        public string IpAddress { get; protected set; }

        /// <summary>
        /// Socket buffer size
        /// </summary>
        public int SocketBufferSize { get; protected set; } = 5000;

    }
}
