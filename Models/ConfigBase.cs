namespace Layer4Stack.Models.Base
{

    /// <summary>
    /// Base config model
    /// </summary>
    public abstract class ConfigBase
    {

        /// <summary>
        /// Socket buffer size 
        /// </summary>
        private int _socketBufferSize = 5000;

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
        public int SocketBufferSize { get { return _socketBufferSize;  } set { _socketBufferSize = value; } }

    }
}
