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
        /// <param name="bufferSize"></param>
        protected ConfigBase(string address, int port, int? bufferSize = null)
        {
            Port = port;
            IpAddress = address;
            if(bufferSize.HasValue)
            {
                SocketBufferSize = bufferSize.Value;
            }
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
