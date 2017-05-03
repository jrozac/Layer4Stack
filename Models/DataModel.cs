using System;

namespace Layer4Stack.Models
{

    /// <summary>
    /// Received message model
    /// </summary>
    public class DataModel
    {
        
        /// <summary>
        /// Message payload 
        /// </summary>
        public byte[] Payload { get; set; }

        /// <summary>
        /// Message received/sent
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Client Id
        /// </summary>
        public string ClientId { get; set; }

    }
}
