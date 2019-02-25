using Layer4Stack.DataProcessors;
using Layer4Stack.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Layer4Stack.Services
{

    /// <summary>
    /// Socket basics. Used by server and client.
    /// </summary>
    internal abstract class TcpSocketBase<TConfig>
        where TConfig : ConfigBase
    {

        /// <summary>
        /// logger
        /// </summary>
        protected readonly ILogger _logger;

        /// <summary>
        /// Data processor creator
        /// </summary>
        protected readonly Func<IDataProcessor> _createDataProcessorFunc;

        /// <summary>
        /// Config 
        /// </summary>
        protected readonly TConfig _config;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="dataProcessor"></param>
        protected TcpSocketBase(TConfig config, Func<IDataProcessor> createDataProcessorFunc, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(GetType());
            _createDataProcessorFunc = createDataProcessorFunc;
            _config = config;
        }

        #region Events 

        /// <summary>
        /// Message received event.
        /// </summary>
        public Action<DataContainer> MsgReceivedAction;

        /// <summary>
        /// Message sent event.
        /// </summary>
        public Action<DataContainer> MsgSentAction;

        /// <summary>
        /// Client connected event
        /// </summary>
        public Action<ClientInfo> ClientConnectedAction;

        /// <summary>
        /// Client disconnected event
        /// </summary>
        public Action<ClientInfo> ClientDisconnectedAction;

        #endregion

        /// <summary>
        /// Send message
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        protected async Task<bool> SendMessage(TcpClientInfo client, DataContainer message)
        {

            // decorate message to be sent over network
            byte[] msg = client.DataProcessor.FilterSendData(message.Payload);

            // sent message 
            bool status = await SendData(client.Client, msg);

            // raise sent 
            if(status)
            {
                TaskUtil.RunAction(() => MsgSentAction?.Invoke(message), _logger);
            }
            return status;

        }

        /// <summary>
        /// Sends data
        /// </summary>
        /// <param name="client"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private async Task<bool> SendData(TcpClient client, byte[] data)
        {
            try
            {
                await client.GetStream().WriteAsync(data, 0, data.Length);
                _logger.LogDebug("Data of lenght {length} sent.", data.Length);
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to send data with exception {type}: {message}.", e.GetType(), e.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Reads data from stream
        /// </summary>
        /// <param name="client"></param>
        /// <param name="clientInfo"></param>
        /// <param name="ct"></param>
        protected async Task ReadData(TcpClientInfo client)
        {
            // get buffer size 
            int bufferSize = _config.SocketBufferSize;

            // Buffer for reading data
            byte[] buffer = new byte[bufferSize];

            // Get a stream object for reading and writing
            NetworkStream stream = client.Client.GetStream();

            // Loop to receive all the data sent by the client.
            stream.ReadTimeout = 2000;
            int i = 0;
            while (true)
            {

                // read data from steram
                try
                {
                    i = await stream.ReadAsync(buffer, 0, bufferSize);
                }
                catch (IOException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                } catch(Exception)
                {
                    break;
                }

                // No data (end of strem)
                if (i == 0)
                {
                    break;
                }

                // received data
                var receivedMessages = client.DataProcessor.ProcessReceivedRawData(buffer, i);
 
                // message received
                if(receivedMessages != null && receivedMessages.Any())
                {
                    receivedMessages.ToList().ForEach((msg) => DataReceived(msg, client.Info.Id));
                }
            }

            // Close connection
            client.Dispose();

        }

        /// <summary>
        /// Message received handler 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="clientId"></param>
        private async Task DataReceived(byte[] msg, string clientId)
        {

            //  create message model 
            DataContainer model = new DataContainer { ClientId = clientId, Payload = msg, Time = DateTime.Now };

            // trigger event
            TaskUtil.RunAction(() => MsgReceivedAction?.Invoke(model), _logger);
            await Task.FromResult(true);

        }

    }
}