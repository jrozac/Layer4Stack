using Layer4Stack.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Layer4Stack.DataProcessors
{

    /// <summary>
    /// Message data processor
    /// </summary>
    public class MessageDataProcessor : IDataProcessor
    {

        /// <summary>
        /// Create ISO 8583 data processor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="idGetFunc"></param>
        /// <returns></returns>
        public static MessageDataProcessor CreateIso8583Processor(ILogger<MessageDataProcessor> logger, int maxLength, Func<byte[],byte[]> idGetFunc = null) =>
            new MessageDataProcessor(
                new MessageDataProcessorConfig(maxLength, new byte[1] {3}, true, Encoding.ASCII.GetBytes("ISO"), idGetFunc), logger);
 
        /// <summary>
        /// Configuration
        /// </summary>
        public MessageDataProcessorConfig Config { get; private set; }

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Recieved buffer
        /// </summary>
        private byte[] _buffer;

        /// <summary>
        /// Buffer current length
        /// </summary>
        private int _bufferLength;

        /// <summary>
        /// Buffer sync status
        /// </summary>
        private bool _synchronized = true;
 
        /// <summary>
        /// Constructor with injected properties 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public MessageDataProcessor(MessageDataProcessorConfig config, ILogger<MessageDataProcessor> logger)
        {
            _logger = logger ?? new Logger<MessageDataProcessor>(null);
            Config = config;
            _buffer = new byte[config.MaxLength];
        }

        /// <summary>
        /// Pack data to be sent
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public byte[] FilterSendData(byte[] msg)
        {
            // create header 
            var header = Config.UseLengthHeader ? new byte[2]
            {
                Convert.ToByte((char) (msg.Length + (Config.Terminator?.Length ?? 0)) / 256),
                Convert.ToByte((char) (msg.Length + (Config.Terminator?.Length ?? 0)) % 256),
            } : new byte[0];

            // set packet 
            var packet = Config.Terminator != null && Config.Terminator.Length > 0 ?
                header.Append(msg).Append(Config.Terminator) :
                header.Append(msg);

            // return packet 
            return packet;
        }

        /// <summary>
        /// Get identifier (used for rpc)
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public byte[] GetIdentifier(byte[] msg)
        {
            return Config.IdGetFunc?.Invoke(msg);
        }

        /// <summary>
        /// Process received raw data 
        /// </summary>
        /// <param name="recieved"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public IEnumerable<byte[]> ProcessReceivedRawData(byte[] recieved, int length)
        {

            // clear buffer if too big to fit and reject message 
            if (_bufferLength + length > _buffer.Length)
            {
                _bufferLength = 0;
                _synchronized = false;
                _logger.LogError("Buffer out of sync due to size overdrown ({buffer}{size}. Data lost.", _bufferLength + length, _buffer.Length);
                return null;
            }

            // add recieved data to buffer
            recieved.CopyTo(_buffer, _bufferLength);
            _bufferLength += length;

            #region syncBuffer

            // try to synchronize
            if(!_synchronized)
            {
                if(Config.Terminator != null)
                {
                    var buf = _buffer.GetAfter(Config.Terminator, false, _bufferLength);
                    if (buf.Length > 0)
                    {
                        _bufferLength = _bufferLength = _buffer.ReplaceWith(buf, 0, 0);
                        _synchronized = true;
                        _logger.LogInformation("Buffer was synchronized with terminator.");
                    }
                    else
                    {
                        return null;
                    }
                } else if(Config.Synchronizator != null)
                {
                    var pos = _buffer.FindFirstOccurrence(Config.Synchronizator, _bufferLength);
                    if(pos != null && pos.Value > Config.Synchronizator.Length + Config.ContainerSize)
                    {
                        pos = pos - (Config.UseLengthHeader ? 2 : 0) - Config.Synchronizator.Length + 1;
                        _bufferLength = _buffer.ReplaceWith(_buffer, pos.Value, 0, _bufferLength - pos);
                        _synchronized = true;
                        _logger.LogInformation("Buffer was synchronized with sync sign.");
                    } else
                    {
                        return null;
                    }
                } else
                {
                    // throw exception if sycnhronization is not possible and buffer is out of sync
                    throw new DataMisalignedException("Data out of sync and cannot be synchronized.");
                }
            }

            #endregion

            // declare messages array
            IEnumerable<byte[]> msgs = new List<byte[]>();

            // read messages positions by terminator
            int[] msgsEndIndexesByTerminator = Config.Terminator != null ? 
                _buffer.FindOccurrences(Config.Terminator, _bufferLength,
                Config.ContainerSize, Config.UseLengthHeader ? 2 : 0) : null;

            // read messages by length header
            int[] msgsEndIndexesByHeader = null;
            if (Config.UseLengthHeader)
            {
                // get end indexes 
                var ixs = new List<int>();
                int pointer = -1;
                while (pointer < _bufferLength)
                {
                    int len = _buffer[pointer+1] * 256 + _buffer[pointer + 2] + 2;
                    if(pointer + len < _bufferLength)
                    {
                        ixs.Add(pointer + len);
                    }
                    pointer += len;
                }
                msgsEndIndexesByHeader = ixs.ToArray();
            }

            // if header and terminator determinations are enabled, both must provide the same results
            if(msgsEndIndexesByHeader != null && msgsEndIndexesByTerminator != null && !msgsEndIndexesByHeader.SequenceEqual(msgsEndIndexesByTerminator))
            {
                _logger.LogError("Indexes from terminators and header are not the same: header={header}, terminator={terminator}.",
                    string.Join(",", msgsEndIndexesByHeader),
                    string.Join(",", msgsEndIndexesByTerminator));
            }

            // get indexes, terminator has priority over length header 
            var msgsEndIndexes = msgsEndIndexesByTerminator ?? msgsEndIndexesByHeader;

            // get messages bounds (intervals to slice the buffer to)
            var intervals = msgsEndIndexes.GetIntervals(Config.UseLengthHeader ? 2: 0, Config.Terminator?.Length ?? 0);

            // extract messages 
            var messages = _buffer.SliceMulti(intervals.Select(t => new Tuple<int,int>(t.Item1, t.Item2-t.Item1+1)).ToArray(), _bufferLength);

            // shift remaining buffer to start if messages are found
            if(msgsEndIndexes.Any())
            {
                _bufferLength = _buffer.ReplaceWith(_buffer, msgsEndIndexes.Max() + 1, 0, _bufferLength - msgsEndIndexes.Last() - 1);
            }

            // return recieved messages
            return messages;
        }

        /// <summary>
        /// Reset processor
        /// </summary>
        public void Reset()
        {
            _bufferLength = 0;
            _synchronized = true;
        }

    }
}
