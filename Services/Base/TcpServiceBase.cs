using Layer4Stack.DataProcessors.Interfaces;
using System.Threading;

namespace Layer4Stack.Services.Base
{

    /// <summary>
    /// Tcp Client service base
    /// </summary>
    public abstract class TcpServiceBase
    {


        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="dataProcessor"></param>
        protected TcpServiceBase(IDataProcessorProvider dataProcessorProvider)
        {
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
