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

        /// <summary>
        /// Message lenght prefix (first two chars ascii(char1)*265 + ascii(char2))
        /// </summary>
        public bool UseLengthHeader { get; set; }
    }
}
