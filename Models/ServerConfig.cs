namespace Layer4Stack.Models
{

    /// <summary>
    /// Server config model
    /// </summary>
    public class ServerConfig : ConfigBase
    {
        /// <summary>
        /// Constructor with properties
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public ServerConfig(string address, int port) : base(address, port)
        {
        }
    }

}
