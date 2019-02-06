using Layer4Stack.DataProcessors;
using Layer4Stack.Handlers;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace Layer4Stack.Services
{

    /// <summary>
    /// Tcp Client service base
    /// </summary>
    public abstract class TcpServiceBase
    {

        /// <summary>
        /// Logger factory 
        /// </summary>
        protected ILoggerFactory LoggerFactory;

        /// <summary>
        /// Logger
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// Data sync
        /// </summary>
        protected readonly DataSynchronizator DataSynchronizator;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="dataProcessor"></param>
        protected TcpServiceBase(IDataProcessorProvider dataProcessorProvider, ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory;
            Logger = loggerFactory.CreateLogger(GetType());
            DataProcessorProvider = dataProcessorProvider;
            DataProcessor = dataProcessorProvider.New;
            DataSynchronizator = new DataSynchronizator();
        }

        /// <summary>
        /// Data processor
        /// </summary>
        protected IDataProcessor DataProcessor { get; set; }

        /// <summary>
        /// Data processor provider
        /// </summary>
        protected IDataProcessorProvider DataProcessorProvider { get; set; }


        /// <summary>
        /// Global cancellation token source
        /// </summary>
        protected CancellationTokenSource CancellationTokenSource { get; set; }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            DataSynchronizator.Dispose();
        }
    }
}
