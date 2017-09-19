using Layer4Stack.DataProcessors.Interfaces;
using System;

namespace Layer4Stack.DataProcessors.Base.Base
{
    /// <summary>
    /// Data processor provider base 
    /// </summary>
    /// <typeparam name="TDataProcessor"></typeparam>
    /// <typeparam name="TDataProcessorConfig"></typeparam>
    public abstract class DataProcessorProviderBase<TDataProcessor,TDataProcessorConfig> 
        where TDataProcessor : IDataProcessor
        where TDataProcessorConfig : DataProcessorConfigBase
    {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config"></param>
        protected DataProcessorProviderBase(TDataProcessorConfig config)
        {
            Config = config;
        }

        /// <summary>
        /// Configuration
        /// </summary>
        protected TDataProcessorConfig Config { get; set; }


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
