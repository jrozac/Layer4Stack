using Layer4Stack.DataProcessors.Base;
using Layer4Stack.DataProcessors.Interfaces;
using System;

namespace Layer4Stack.DataProcessors
{
    /// <summary>
    /// Data processor provider base 
    /// </summary>
    /// <typeparam name="TDataProcessor"></typeparam>
    /// <typeparam name="TDataProcessorConfig"></typeparam>
    public sealed class DataProcessorProvider<TDataProcessor,TDataProcessorConfig> : IDataProcessorProvider
        where TDataProcessor : IDataProcessor
        where TDataProcessorConfig : DataProcessorConfigBase
    {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config"></param>
        public DataProcessorProvider(TDataProcessorConfig config)
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
