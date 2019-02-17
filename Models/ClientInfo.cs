using System;

namespace Layer4Stack.Models
{

    /// <summary>
    /// Defines client information
    /// </summary>
    public class ClientInfo
    {
        /// <summary>
        /// Client id 
        /// </summary>
        public string Id { get; internal set; }

        /// <summary>
        /// Client/server address IP
        /// </summary>
        public string IpAddress { get; internal set; }

        /// <summary>
        /// Client port 
        /// </summary>
        public int Port { get; internal set; }

        /// <summary>
        /// Connected time
        /// </summary>
        public DateTime Time { get; internal set; }
    }
}
