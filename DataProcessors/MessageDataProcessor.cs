using Layer4Stack.DataProcessors.Base;
using Layer4Stack.DataProcessors.Interfaces;
using System;

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
        public MessageDataProcessor(MessageDataProcessorConfig config) : base(config)
        {

        }


        /// <summary>
        /// Currently receiving message 
        /// </summary>
        private byte[] _message = new byte[10000];
        

        /// <summary>
        /// Currently receiveing message size
        /// </summary>
        private int _messageBufferSize = 0;


        /// <summary>
        /// Last Message size
        /// </summary>
        private int _lastMessageSize = 0;


        /// <summary>
        /// Last completely received message 
        /// </summary>
        private byte[] _lastMessage;


        /// <summary>
        /// Gets message terminator position
        /// </summary>
        /// <param name="haystack"></param>
        /// <param name="needle"></param>
        /// <returns></returns>
        private int GetMessageTerminatorPosition(byte[] haystack)
        {
            var len = Config.MessageTerminator.Length;
            var limit = haystack.Length - len;
            for (var i = 0; i <= limit; i++)
            {
                var k = 0;
                for (; k < len; k++)
                {
                    if (Config.MessageTerminator[k] != haystack[i + k]) break;
                }
                if (k == len) return i;
            }
            return -1;
        }


        /// <summary>
        /// Gets last completely received data item
        /// </summary>
        /// <returns></returns>
        public byte[] GetNewData()
        {
            byte[] retVal = new byte[_lastMessageSize];
            Buffer.BlockCopy(_lastMessage, 0, retVal, 0, _lastMessageSize);
            _lastMessage = null;
            return retVal;
        }


        /// <summary>
        /// Processes received data
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        public void ProcessReceivedRawData(byte[] buffer, int length)
        {

            // check for termintor
            int terminatorPos = GetMessageTerminatorPosition(buffer);

            // copy buffer to message
            Buffer.BlockCopy(buffer, 0, _message, _messageBufferSize, terminatorPos != -1 ? terminatorPos : length);
            _messageBufferSize += terminatorPos != -1 ? terminatorPos : length;

            // message received
            if (terminatorPos != -1)
            {

                // set received message
                byte[] receivedMessage = new byte[_messageBufferSize];
                Buffer.BlockCopy(_message, 0, receivedMessage, 0, _messageBufferSize);

                // message received
                _lastMessage = _message;
                _lastMessageSize = _messageBufferSize;

                // clear message
                _message = new byte[10000];
                _messageBufferSize = 0;

                // add remaining read to message
                if (length > terminatorPos + Config.MessageTerminator.Length)
                {
                    Buffer.BlockCopy(
                        buffer,
                        terminatorPos + Config.MessageTerminator.Length,
                        _message,
                        0, length - terminatorPos - Config.MessageTerminator.Length);
                }

            }

            // clear buffer
            buffer = new byte[256];

        }


        /// <summary>
        /// Filters outgoing message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public byte[] FilterSendData(byte[] msg)
        {
            // set a message terminator
            byte[] msgBlcok = new byte[msg.Length + Config.MessageTerminator.Length];
            Buffer.BlockCopy(msg, 0, msgBlcok, 0, msg.Length);
            Buffer.BlockCopy(Config.MessageTerminator, 0, msgBlcok, msg.Length, Config.MessageTerminator.Length);

            return msgBlcok;
        }

    }
}
