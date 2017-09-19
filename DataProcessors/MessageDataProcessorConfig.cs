using System;
using Layer4Stack.DataProcessors.Base;

namespace Layer4Stack.DataProcessors
{

    /// <summary>
    /// Message data processor config 
    /// </summary>
    public class MessageDataProcessorConfig : DataProcessorConfigBase
    {
        /// <summary>
        /// Message terminator
        /// </summary>
        public byte[] MessageTerminator { get; set; }
    }
}
