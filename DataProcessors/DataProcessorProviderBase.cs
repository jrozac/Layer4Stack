using System;

namespace Layer4Stack.DataProcessors
{
    /// <summary>
    /// Data processor provider base 
    /// </summary>
    /// <typeparam name="TDataProcessor"></typeparam>
    /// <typeparam name="TDataProcessorConfig"></typeparam>
    public abstract class DataProcessorProviderBase<TDataProcessor,TDataProcessorConfig> : IDataProcessorProvider
        where TDataProcessor : IDataProcessor
        where TDataProcessorConfig : DataProcessorConfigBase
    {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config"></param>
        public DataProcessorProviderBase(TDataProcessorConfig config)
        {
            Config = config;
        }

        /// <summary>
        /// Configuration
        /// </summary>
        private TDataProcessorConfig Config { get; set; }

        /// <summary>
        /// Get new instance 
        /// </summary>
        public IDataProcessor New
        {
            get {
                return (TDataProcessor)Activator.CreateInstance(typeof(TDataProcessor), new object[] { Config });
            }
        }


    }
}
