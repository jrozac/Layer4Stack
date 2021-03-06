﻿namespace Layer4Stack.DataProcessors
{

    /// <summary>
    /// Data processor type
    /// </summary>
    public enum EnumDataProcessorType
    {

        /// <summary>
        /// Iso 8583 message
        /// format: [length header two chars][data][terminator byte value 3]
        /// </summary>
        Iso8583,

        /// <summary>
        /// Hsm communication
        /// format: [zero as message start][length one char][data]
        /// </summary>
        Hsm,

        /// <summary>
        /// Default message
        /// format: [zero as message start][length two char][data]
        /// </summary>
        Message

    }
}
