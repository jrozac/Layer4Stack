using System.Collections.Generic;

namespace Layer4Stack.DataProcessors
{

    /// <summary>
    /// Data reader and writer interface
    /// </summary>
    public interface IDataProcessor
    {

        /// <summary>
        /// Handles received raw data
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        IEnumerable<byte[]> ProcessReceivedRawData(byte[] recieved, int length);

        /// <summary>
        /// Filters outgoing data
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        byte[] FilterSendData(byte[] msg);

        /// <summary>
        /// Gets identifier
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        byte[] GetIdentifier(byte[] msg);

        /// <summary>
        /// Resets data 
        /// </summary>
        void Reset();

    }
}
