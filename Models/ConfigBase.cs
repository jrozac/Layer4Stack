namespace Layer4Stack.Models
{

    /// <summary>
    /// Base config model
    /// </summary>
    public abstract class ConfigBase
    {

        /// <summary>
        /// Server port
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// IP address to start instance on.
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// Socket buffer size
        /// </summary>
        public int SocketBufferSize { get; set; } = 5000;
    }
}
