using Layer4Stack.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Layer4Stack.DataProcessors
{

    /// <summary>
    /// Simple message data processor
    /// </summary>
    public class SimpleMessageDataProcessor : IDataProcessor
    {

        /// <summary>
        /// Create hsm processor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="bufferLength"></param>
        /// <returns></returns>
        public static SimpleMessageDataProcessor CreateHsmProcessor(ILogger<SimpleMessageDataProcessor> logger, int bufferLength = 500) =>
            new SimpleMessageDataProcessor(logger, bufferLength, (msg) => msg.Length >= 6 ? msg.ToList().Take(4).ToArray() : null);
 
        /// <summary>
        /// Id get function
        /// </summary>
        private readonly Func<byte[], byte[]> _getIdFunc;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<SimpleMessageDataProcessor> _logger;

        /// <summary>
        /// Recieved buffer
        /// </summary>
        private readonly byte[] _buffer;

        /// <summary>
        /// Buffer current length
        /// </summary>
        private int _bufferLength;

        /// <summary>
        /// Constructor with get id func
        /// </summary>
        /// <param name="getIdFunc"></param>
        public SimpleMessageDataProcessor(ILogger<SimpleMessageDataProcessor> logger, int bufferLength, Func<byte[], byte[]> getIdFunc = null)
        {
            _logger = logger;
            _getIdFunc = getIdFunc;
            _buffer = new byte[bufferLength];
        }

        /// <summary>
        /// Filter send data 
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public byte[] FilterSendData(byte[] msg)
        {
            // length check
            if(msg.Length > 256)
            {
                _logger.LogError("Message is too long.");
                return null;
            }

            // pack and return
            var ret = new byte[msg.Length + 2];
            ret[0] = 0;
            ret[1] = (byte)msg.Length;
            Buffer.BlockCopy(msg, 0, ret, 2, msg.Length);
            return ret;
        }

        /// <summary>
        /// Get identifier
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public byte[] GetIdentifier(byte[] msg)
        {
            return _getIdFunc?.Invoke(msg);
        }

        /// <summary>
        /// Process buffer
        /// </summary>
        /// <param name="recieved"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public IEnumerable<byte[]> ProcessReceivedRawData(byte[] recieved, int length)
        {

            // check package length
            if(length > _buffer.Length)
            {
                _logger.LogError("Recieved is too long.");
                return null;
            }

            // check buffer lenght 
            else if(_bufferLength + length > _buffer.Length)
            {
                _logger.LogError("Buffer overdraw. Deleting buffer.");
                _bufferLength = 0;
                return null;
            }

            // copy message to buffer
            Buffer.BlockCopy(recieved, 0, _buffer, _bufferLength, length);
            _bufferLength = _bufferLength + length;

            // find starts of messages 
            var starts = _buffer.FindOccurrences(new byte[1] { 0 }, _bufferLength).
                Where(pos => _bufferLength > pos + 1 && _bufferLength > pos + 1 + _buffer[pos + 1]);
            if (!starts.Any())
            {
                return new List<byte[]>();
            }

            // get intervals 
            var intervals = starts.Select(pos => new Tuple<int, int>(pos + 2, _buffer[pos + 1])).ToArray();

            // get messages 
            var msgs = _buffer.SliceMulti(intervals);

            // move buffer
            var start = starts.Max() + 2 + _buffer[starts.Max() + 1];
            if(start >= _bufferLength)
            {
                _bufferLength = 0;
            } else
            {
                var len = _bufferLength - start;
                _bufferLength = _buffer.ReplaceWith(_buffer, start, 0, len);
            }

            // return 
            return msgs;
        }

        /// <summary>
        /// Reset 
        /// </summary>
        public void Reset()
        {
            _bufferLength = 0;
        }
    }
}
