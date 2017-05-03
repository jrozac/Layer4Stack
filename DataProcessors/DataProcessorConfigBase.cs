using System;

namespace Layer4Stack.DataProcessors
{

    /// <summary>
    /// Data processor config base
    /// </summary>
    public abstract class DataProcessorConfigBase<T> : IDataProcessorConfig where T : DataProcessorBase
    {

        /// <summary>
        /// Buffer size default
        /// </summary>
        private int _bufferSize = 200;


        /// <summary>
        /// Processor type
        /// </summary>
        public Type ProcessorType {
            get {

                return typeof(T);
            }
        }


        /// <summary>
        /// Buffer size 
        /// </summary>
        public int BufferSize {
            get {
                return _bufferSize;
            }
            set {
                _bufferSize = value;
            }
        }


    }
}
