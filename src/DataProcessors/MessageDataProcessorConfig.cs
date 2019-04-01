using System;

namespace Layer4Stack.DataProcessors
{

    /// <summary>
    /// Data processor config 
    /// </summary>
    public class MessageDataProcessorConfig
    {

        /// <summary>
        /// Default configuration
        /// </summary>
        /// <param name="idGetFunc"></param>
        /// <returns></returns>
        public static MessageDataProcessorConfig Default(Func<byte[], byte[]> idGetFunc = null) =>
            new MessageDataProcessorConfig(5000, new byte[1] { 0 }, false, null, idGetFunc);

        /// <summary>
        /// Constructor with all properties 
        /// </summary>
        /// <param name="maxLength"></param>
        /// <param name="terminator"></param>
        /// <param name="useLengthHeader"></param>
        /// <param name="synchronizator"></param>
        /// <param name="idGetFunc"></param>
        public MessageDataProcessorConfig(int maxLength, byte[] terminator, bool useLengthHeader = false, byte[] synchronizator = null, Func<byte[], byte[]> idGetFunc = null)
        {
            // set config
            MaxLength = maxLength;
            Terminator = terminator?.Length > 0 ? terminator.Clone() as byte[] : null;
            UseLengthHeader = useLengthHeader;
            Synchronizator = synchronizator?.Length > 0 ? synchronizator.Clone() as byte[] : null;
            IdGetFunc = idGetFunc;

            // at least one of the options has to selected
            if (Terminator == null && !UseLengthHeader)
            {
                throw new ArgumentException("There should be defined at least a valid terminator or enabled header length.");
            }

            // minimum buffer length should fit a message of length 1
            if (maxLength < ContainerSize + 1)
            {
                throw new ArgumentException(string.Format("Minimal lengths for provided configuration is {0}.", ContainerSize + 1));
            }

        }

        /// <summary>
        /// Id get function
        /// </summary>
        public Func<byte[], byte[]> IdGetFunc { get; private set; }

        /// <summary>
        /// Terminator
        /// </summary>
        public byte[] Terminator { get; private set; }

        /// <summary>
        /// Max buffer length
        /// </summary>
        public int MaxLength { get; private set; }

        /// <summary>
        /// Length header, like used in ISO8583
        /// </summary>
        public bool UseLengthHeader { get; private set; }

        /// <summary>
        /// Syncrhonizator. Usually the prefix of each message used to synchronize buffer.
        /// </summary>
        public byte[] Synchronizator { get; private set; }

        /// <summary>
        /// Container size 
        /// </summary>
        public int ContainerSize => (UseLengthHeader ? 2 : 0) + (Terminator?.Length ?? 0);

    }
}
