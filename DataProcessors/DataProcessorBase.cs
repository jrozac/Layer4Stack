using Microsoft.Extensions.Logging;

namespace Layer4Stack.DataProcessors
{

    /// <summary>
    /// Data processor base class
    /// </summary>
    public abstract class DataProcessorBase<TDataProcessorConfig> where TDataProcessorConfig : DataProcessorConfigBase
    {

        /// <summary>
        /// Logger
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config"></param>
        protected DataProcessorBase(TDataProcessorConfig config, ILogger logger)
        {
            Logger = logger;
            Config = config;
        }

        /// <summary>
        /// Data processor config
        /// </summary>
        protected TDataProcessorConfig Config { get; set; }

    }
}
