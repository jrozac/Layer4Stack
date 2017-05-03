using System;

namespace Layer4Stack.DataProcessors
{

    /// <summary>
    /// Message data processor config 
    /// </summary>
    public class MessageDataProcessorConfig : DataProcessorConfigBase<MessageDataProcessor>, IDataProcessorConfig
    {

        /// <summary>
        /// Message terminator
        /// </summary>
        public byte[] MessageTerminator { get; set; }

    }
}
