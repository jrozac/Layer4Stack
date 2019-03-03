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
        public static SimpleMessageDataProcessor CreateProcessor(
            ILogger<SimpleMessageDataProcessor> logger, 
            int bufferLength = 500, int headerLength = 1, 
            Func<byte[], byte[]> func = null) =>
            new SimpleMessageDataProcessor(logger, bufferLength, new byte[1] { 0 }, headerLength, func ?? ((msg) => msg.Length >= 5 ? msg.ToList().Take(4).ToArray() : null));

        /// <summary>
        /// Delimiter
        /// </summary>
        private readonly byte[] _delimiter;

        /// <summary>
        /// Length header size 
        /// </summary>
        private readonly int _headerSize;

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
        /// Constructor with injected properties 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="bufferLength"></param>
        /// <param name="delimiter"></param>
        /// <param name="headerSize"></param>
        /// <param name="getIdFunc">Id get function. Used to extract the if from raw message.</param>
        public SimpleMessageDataProcessor(ILogger<SimpleMessageDataProcessor> logger, int bufferLength, byte[] delimiter, int headerSize, Func<byte[], byte[]> getIdFunc = null)
        {

            // check arguments
            if(_delimiter?.Length == 0 || (_headerSize > 3 && _headerSize <= 0) && _bufferLength < _delimiter.Length + headerSize + 1)
            {
                throw new ArgumentException("Invalid setup");
            }

            _delimiter = delimiter;
            _headerSize = headerSize;
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
            if(msg.Length > MaxSize)
            {
                _logger.LogError("Message is too long.");
                return null;
            }

            // set length header 
            var header = DataProcessorUtil.LengthToHeader(msg.Length, _headerSize);
            var ret = new byte[msg.Length + 1 + _headerSize];
            ret[0] = 0;
            Buffer.BlockCopy(header, 0, ret, 1, _headerSize);
            Buffer.BlockCopy(msg, 0, ret, 1 + _headerSize, msg.Length);
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
            var starts = _buffer.FindOccurrences(_delimiter, _bufferLength, ContainerSize);
            if (!starts.Any())
            {
                return new List<byte[]>();
            }

            // get intervals 
            var intervals = starts.Where(pos => _bufferLength >= pos + ContainerSize &&
                _bufferLength >= pos + ContainerSize + DataProcessorUtil.HeaderToLength(_buffer.Slice(pos + 1, _headerSize))).
                Select(pos => new Tuple<int, int>(pos + ContainerSize,
                    (int)DataProcessorUtil.HeaderToLength(_buffer.Slice(pos + 1, _headerSize)))).
                ToArray();

            // no intervals found
            if (!intervals.Any())
            {
                return new List<byte[]>();
            }

            // fix intervals to not overlap 
            intervals = Enumerable.Range(0, intervals.Length).Select(i => {
                var cur = intervals[i];
                if (i + 1 >= intervals.Length)
                {
                    return cur;
                }
                var next = intervals[i + 1];
                var endPos = cur.Item1 + cur.Item2 - 1;
                if (endPos >= next.Item1)
                {
                    _logger.LogError("Overlapping messages");
                    return new Tuple<int, int>(cur.Item1, next.Item1 - cur.Item1);
                }
                return cur;
            }).ToArray();

            // get messages 
            var msgs = _buffer.SliceMulti(intervals);

            // move buffer
            var start = intervals.Last().Item1 + intervals.Last().Item2 + 1;
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

        /// <summary>
        /// Container size 
        /// </summary>
        private int ContainerSize => _headerSize + _delimiter.Length;

        /// <summary>
        /// Max message size 
        /// </summary>
        private int MaxSize => (int) Math.Pow(byte.MaxValue+1, _headerSize)-1;

    }
}
