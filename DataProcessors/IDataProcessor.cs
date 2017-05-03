﻿namespace Layer4Stack.DataProcessors
{

    /// <summary>
    /// Data reader and writer interface
    /// </summary>
    public interface IDataProcessor
    {

        /// <summary>
        /// Data processor config
        /// </summary>
        IDataProcessorConfig Config { get; set; }


        /// <summary>
        /// Gets last received data time.
        /// </summary>
        /// <returns></returns>
        byte[] GetNewData();


        /// <summary>
        /// Handles received raw data
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        void ProcessReceivedRawData(byte[] buffer, int length);


        /// <summary>
        /// Filters outgoing data
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        byte[] FilterSendData(byte[] msg);

    }
}
