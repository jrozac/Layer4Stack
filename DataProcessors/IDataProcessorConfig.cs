using System;

namespace Layer4Stack.DataProcessors
{

    /// <summary>
    /// data processor config interface
    /// </summary>
    public interface IDataProcessorConfig
    {
        /// <summary>
        /// Buffer size
        /// </summary>
        int BufferSize { get; set; }

        /// <summary>
        /// Gets processor type 
        /// </summary>
        Type ProcessorType { get; }
    }
}
