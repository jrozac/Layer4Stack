using System;

namespace Layer4Stack.Models
{

    /// <summary>
    /// Received message model
    /// </summary>
    public class DataContainer
    {
        
        /// <summary>
        /// Message payload 
        /// </summary>
        public byte[] Payload { get; set; }

        /// <summary>
        /// Raw paylod (includes header, footer, etc - depending on data processor)
        /// </summary>
        public byte[] RawPayload { get; set; }

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
