using Layer4Stack.DataProcessors;
using Layer4Stack.Models;
using Microsoft.Extensions.Logging;
using System;

namespace Layer4Stack.Services
{

    /// <summary>
    /// Service base
    /// </summary>
    public abstract class TcpServiceBase
    {

        /// <summary>
        /// Create data processor function
        /// </summary>
        /// <typeparam name="TConfig"></typeparam>
        /// <param name="config"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="dataProcessorType"></param>
        /// <param name="getIdFunc"></param>
        /// <returns></returns>
        public Func<IDataProcessor> CreateDataProcesorFunc<TConfig>(TConfig config, ILoggerFactory loggerFactory,
                EnumDataProcessorType dataProcessorType, Func<byte[], byte[]> getIdFunc = null)
            where TConfig : ConfigBase
        {

            // set data processor creator
            switch (dataProcessorType)
            {
                case EnumDataProcessorType.Hsm:
                    return new Func<IDataProcessor>(() => SimpleMessageDataProcessor.CreateHsmProcessor(
                        loggerFactory.CreateLogger<SimpleMessageDataProcessor>(),
                        config.SocketBufferSize * 2));
                case EnumDataProcessorType.Iso8583:
                    return new Func<IDataProcessor>(() => MessageDataProcessor.CreateIso8583Processor(
                        loggerFactory.CreateLogger<MessageDataProcessor>(),
                        config.SocketBufferSize * 2, getIdFunc));
            }

            // nothing done 
            return null;

        }
    } 
}
