using Layer4Stack.DataProcessors;
using System.Threading;

namespace Layer4Stack.Services
{

    /// <summary>
    /// Tcp Client service base
    /// </summary>
    public abstract class TcpServiceBase
    {

        /// <summary>
        /// Global cancellation token source
        /// </summary>
        protected CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Data processor config
        /// </summary>
        public IDataProcessorConfig DataProcessorConfig { get; set; }

    }
}
