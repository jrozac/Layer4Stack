using Layer4Stack.DataProcessors.Base.Base;
using Layer4Stack.DataProcessors.Interfaces;

namespace Layer4Stack.DataProcessors
{

    /// <summary>
    /// Data Processor provider
    /// </summary>
    /// <typeparam name="TDataProcessor"></typeparam>
    public class MessageDataProcessorProvider : DataProcessorProviderBase<MessageDataProcessor, MessageDataProcessorConfig>, IDataProcessorProvider
    {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config"></param>
        public MessageDataProcessorProvider(MessageDataProcessorConfig config) : base(config)
        {
            Config = config;
        }


    }
}
