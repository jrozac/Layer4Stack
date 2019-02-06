using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace Layer4Stack.DataProcessors
{

    /// <summary>
    /// Message data processor
    /// </summary>
    public class MessageDataProcessor : DataProcessorBase<MessageDataProcessorConfig>, IDataProcessor
    {


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config"></param>
        public MessageDataProcessor(MessageDataProcessorConfig config, ILogger logger) : base(config, logger)
        {
            ClearBuffer();
        }

        /// <summary>
        /// Currently receiving message 
        /// </summary>
        private byte[] _message;


        /// <summary>
        /// Currently receiving message header
        /// </summary>
        private byte[] _header;


        /// <summary>
        /// Currently receiveing message size
        /// </summary>
        private int _messageBufferSize;


        /// <summary>
        /// Length of received header 
        /// </summary>
        private int _headerBufferSize;


        /// <summary>
        /// Length of received footer 
        /// </summary>
        private int _footerBufferSize;


        /// <summary>
        /// Declared currently receiving message size (based on length in 2 char header) 
        /// </summary>
        private int _messageDeclaredSize = -1;


        /// <summary>
        /// Received messages 
        /// </summary>
        private ConcurrentQueue<byte[]> receivedMessages = new ConcurrentQueue<byte[]>();


        /// <summary>
        /// Gets message terminator position
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private int ReadFooterEndMessage(byte[] buffer, int length)
        {

            // get terminator lenght
            int len = Config.MessageTerminator.Length;
            if(len == 0)
            {
                return -1;
            }

            // find footer last position 
            int matchCount = _footerBufferSize;
            int pos = 0;
            for (int i= Config.UseLengthHeader ? 2 :0; i < length && matchCount < Config.MessageTerminator.Length; i++) {
                matchCount = (buffer[i] == Config.MessageTerminator[matchCount]) ? matchCount + 1 : 0;
                pos = i;
            }

            // update footer buffer 
            _footerBufferSize = matchCount;

            // return position
            return matchCount == Config.MessageTerminator.Length ? pos : -1;
        }


        /// <summary>
        /// Gets end message position 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private int ReadHeaderEndMessage(byte[] buffer, int length)
        {

            // header not enabled or invalid buffer
            length = Math.Min(buffer.Length, length);
            if (!Config.UseLengthHeader || length == 0)
            {
                return -1;
            }

            // bytes available to read 
            int readLength = Math.Min(length, _header.Length - _headerBufferSize);

            // read header 
            if (_headerBufferSize < _header.Length)
            {

                // copy header buffer 
                Buffer.BlockCopy(buffer, 0, _header, _headerBufferSize, readLength);

                // update header length receive
                _headerBufferSize = _headerBufferSize + readLength;

                // header received 
                if (_headerBufferSize == _header.Length)
                {
                    _messageDeclaredSize = Convert.ToChar(_header[0]) * 256 + Convert.ToChar(_header[1]);
                }
            }

            // header was read 
            if(_headerBufferSize == _header.Length)
            {

                // get end message position in buffer (declared size includes message + terminator)
                int endMessageDeclared = _messageDeclaredSize - _messageBufferSize + readLength - 1;

                // return position if in current buffer 
                return endMessageDeclared <= buffer.Length ? endMessageDeclared : -1;

            }

            // header not found yet
            return -1;

        }


        /// <summary>
        /// Clears buffer 
        /// </summary>
        private void ClearBuffer()
        {
            _message = new byte[10000];
            _header = new byte[2];
            _messageBufferSize = 0;
            _headerBufferSize = 0;
            _footerBufferSize = 0;
            _messageDeclaredSize = -1;
        }


        /// <summary>
        /// Gets last completely received data item
        /// </summary>
        /// <returns></returns>
        public byte[] GetNewData()
        {
            byte[] message;
            return receivedMessages.TryDequeue(out message) ? message : null;
        }


        /// <summary>
        /// Processes received data
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        public void ProcessReceivedRawData(byte[] buffer, int length)
        {

            // check for termintor
            int endMessageTerminator = ReadFooterEndMessage(buffer, length);

            // gets end message declared by header 
            int endMessageDeclared = ReadHeaderEndMessage(buffer, length);
            
            // end message length
            int endMessageLength = endMessageTerminator != -1 || endMessageDeclared != -1 ? // either terminator or declared end 
                Math.Min(
                    endMessageTerminator != -1 ? endMessageTerminator+1 : int.MaxValue, 
                    endMessageDeclared != -1 ? endMessageDeclared+1 : int.MaxValue) :
                -1; // not end yet

            // copy buffer to message
            Buffer.BlockCopy(
                buffer,
                0,
                _message,
                _messageBufferSize,
                endMessageLength != -1 ? endMessageLength : length);
            _messageBufferSize += (endMessageLength != -1 ? endMessageLength : length);

            // message received
            if (endMessageLength != -1)
            {

                // log error matching
                if(endMessageLength -1 != endMessageTerminator && Config.MessageTerminator.Length > 0)
                {
                    Logger.LogError("Terminator is missing. Used header lenght of {length}.", endMessageDeclared);       
                }
                if(endMessageLength -1 != endMessageDeclared && Config.UseLengthHeader)
                {
                    Logger.LogError("Lentgh header of {end} is invalid. Terminator was matched before.", endMessageDeclared);
                }

                // set received message
                byte[] receivedMessage = new byte[_messageBufferSize - _headerBufferSize - _footerBufferSize];
                Buffer.BlockCopy(_message, _headerBufferSize, receivedMessage, 0, _messageBufferSize - _headerBufferSize - _footerBufferSize);

                // message received
                receivedMessages.Enqueue(receivedMessage);

                // clear buffer
                ClearBuffer();

                // add remaining read to message
                if (length > endMessageLength + Config.MessageTerminator.Length)
                {
                    Buffer.BlockCopy(
                        buffer,
                        endMessageLength + Config.MessageTerminator.Length,
                        _message,
                        0, length - endMessageLength - Config.MessageTerminator.Length);
                }

            }

        }


        /// <summary>
        /// Filters outgoing message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public byte[] FilterSendData(byte[] msg)
        {
            // get lenght offset (header lenght)
            var headerLength = Config.UseLengthHeader ? 2 : 0;

            // get message full lenght 
            int fullLength = msg.Length + Config.MessageTerminator.Length + headerLength;

            // pack block to be sent 
            byte[] msgBlcok = new byte[fullLength];
            if(headerLength > 0) // length header
            {
                Buffer.BlockCopy(new byte[2]
                {
                    Convert.ToByte((char) (fullLength-headerLength) / 256),
                    Convert.ToByte((char) (fullLength-headerLength) % 256),
                }, 0, msgBlcok, 0, 2);
            }
            Buffer.BlockCopy(msg, 0, msgBlcok, headerLength, msg.Length);
            Buffer.BlockCopy(Config.MessageTerminator, 0, msgBlcok, headerLength + msg.Length, Config.MessageTerminator.Length);

            // return block
            return msgBlcok;
        }

        /// <summary>
        /// Gets identifier
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public byte[] GetIdentifier(byte[] msg)
        {
            if(Config.ExtractId != null)
            {
                return Config.ExtractId(msg);
            }
            return null;
        }
    }
}
