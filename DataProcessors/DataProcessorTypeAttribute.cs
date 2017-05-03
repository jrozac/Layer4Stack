using System;

namespace Layer4Stack.DataProcessors
{

    /// <summary>
    /// Data processor type attributre
    /// </summary>
    public class DataProcessorTypeAttribute : Attribute
    {
        /// <summary>
        /// Processor type 
        /// </summary>
        public Type ProcessorType { get; set; }
    }
}
