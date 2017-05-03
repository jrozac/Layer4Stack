namespace Layer4Stack.Models
{

    /// <summary>
    /// Base config model
    /// </summary>
    public abstract class ConfigBaseModel
    {

        /// <summary>
        /// Server port
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// IP address to start instance on.
        /// </summary>
        public string IpAddress { get; set; }


    }
}
