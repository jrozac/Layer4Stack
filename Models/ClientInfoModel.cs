using System;

namespace Layer4Stack.Models
{

    /// <summary>
    /// Defines client information
    /// </summary>
    public class ClientInfoModel
    {
        /// <summary>
        /// Client id 
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Client/server address IP
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// Client port 
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Connected time
        /// </summary>
        public DateTime Time { get; set; }
    }
}
