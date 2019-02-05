namespace Layer4Stack.DataProcessors
{
    /// <summary>
    /// Message data processor provider 
    /// </summary>
    public class MessageDataProcessorProvider : DataProcessorProviderBase<MessageDataProcessor, MessageDataProcessorConfig>
    {
        /// <summary>
        /// Cosntructor with injected poperties
        /// </summary>
        /// <param name="config"></param>
        public MessageDataProcessorProvider(MessageDataProcessorConfig config) : base(config)
        {
        }
    }
}
