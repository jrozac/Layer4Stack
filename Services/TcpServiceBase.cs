using Layer4Stack.DataProcessors;
using Microsoft.Extensions.Logging;
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
        /// Default constructor
        /// </summary>
        /// <param name="dataProcessor"></param>
        protected TcpServiceBase(IDataProcessorProvider dataProcessorProvider, ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory;
            DataProcessorProvider = dataProcessorProvider;
        }


        /// <summary>
        /// Data processor provider
        /// </summary>
        protected IDataProcessorProvider DataProcessorProvider { get; set; }


        /// <summary>
        /// Global cancellation token source
        /// </summary>
        protected CancellationTokenSource CancellationTokenSource { get; set; }

    }
}
