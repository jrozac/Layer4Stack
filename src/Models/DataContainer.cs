﻿using System;

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
        public byte[] Payload { get; internal set; }

        /// <summary>
        /// Message received/sent
        /// </summary>
        public DateTime Time { get; internal set; }

        /// <summary>
        /// Client Id
        /// </summary>
        public string ClientId { get; internal set; }

    }
}
