namespace Layer4Stack.DataProcessors.Base
{

    /// <summary>
    /// Data processor base class
    /// </summary>
    public class DataProcessorBase<TDataProcessorConfig> where TDataProcessorConfig : DataProcessorConfigBase
    {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config"></param>
        protected DataProcessorBase(TDataProcessorConfig config)
        {
            Config = config;
        }

        /// <summary>
        /// Data processor config
        /// </summary>
        protected TDataProcessorConfig Config { get; set; }

    }
}
