using Layer4Stack.DataProcessors;
using Layer4Stack.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
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
        protected readonly ILogger Logger;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="dataProcessor"></param>
        protected TcpSocketBase(Func<IDataProcessor> createDataProcessorFunc, ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger(GetType());
            CreateDataProcessorFunc = createDataProcessorFunc;
        }

        /// <summary>
        /// Data processor creator
        /// </summary>
        protected Func<IDataProcessor> CreateDataProcessorFunc { get; private set; }


        /// <summary>
        /// Config 
        /// </summary>
        protected TConfig Config { get; set; }

        /// <summary>
        /// Message received event.
        /// </summary>
        public event EventHandler<DataContainer> MsgReceivedEvent;


        /// <summary>
        /// Message sent event.
        /// </summary>
        public event EventHandler<DataContainer> MsgSentEvent;


        /// <summary>
        /// Client connected event
        /// </summary>
        public event EventHandler<ClientInfo> ClientConnectedEvent;


        /// <summary>
        /// Client disconnected event
        /// </summary>
        public event EventHandler<ClientInfo> ClientDisconnectedEvent;


        /// <summary>
        /// Raises message received event
        /// </summary>
        /// <param name="model"></param>
        protected void RaiseMsgReceivedEvent(DataContainer model)
        {
            Logger.LogDebug("Message of lenght {length} received.", model.Payload.Length);
            MsgReceivedEvent?.Invoke(this, model);
        }


        /// <summary>
        /// Raises message received event
        /// </summary>
        /// <param name="model"></param>
        protected void RaiseMsgSentEvent(DataContainer model)
        {
            Logger.LogDebug("Message of lenght {lenght} sent.", model.Payload.Length);
            MsgSentEvent?.Invoke(this, model);
        }


        /// <summary>
        /// Raises client disconnected event
        /// </summary>
        /// <param name="model"></param>
        protected void RaiseClientDisconnectedEvent(ClientInfo model)
        {
            Logger.LogInformation("Client {id} disconnected.", model.Id);
            ClientDisconnectedEvent?.Invoke(this, model);
        }


        /// <summary>
        /// Raises client connected event
        /// </summary>
        /// <param name="model"></param>
        protected void RaiseClientConnectedEvent(ClientInfo model)
        {
            Logger.LogInformation("Client {id} connected.", model.Id);
            ClientConnectedEvent?.Invoke(this, model);
        }


        /// <summary>
        /// Send message
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        protected bool SendMessage(TcpClientInfo client, DataContainer message)
        {

            // decorate message to be sent over network
            byte[] msg = client.DataProcessor.FilterSendData(message.Payload);
            message.RawPayload = msg;

            // sent message 
            bool status = SendData(client.Client, msg);

            // raise sent 
            if(status)
            {
                RaiseMsgSentEvent(message);
            }
            return status;

        }


        /// <summary>
        /// Sends data
        /// </summary>
        /// <param name="client"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool SendData(TcpClient client, byte[] data)
        {
            try
            {
                client.GetStream().Write(data, 0, data.Length);
                Logger.LogDebug("Data of lenght {length} sent.", data.Length);
            }
            catch (Exception)
            {
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
        protected void ReadData(TcpClientInfo client, CancellationToken[] ct)
        {
            // get buffer size 
            int bufferSize = Config.SocketBufferSize;

            // Buffer for reading data
            byte[] buffer = new byte[bufferSize];

            // Get a stream object for reading and writing
            NetworkStream stream = client.Client.GetStream();

            // Loop to receive all the data sent by the client.
            stream.ReadTimeout = 2000;
            int i = 0;
            while (true)
            {

                // Task cancelled
                bool quit = false;
                if(ct != null)
                {
                    foreach(CancellationToken token in ct)
                    {
                        if(token.IsCancellationRequested)
                        {
                            quit = true;
                            break;
                        }
                    }
                }
                if(quit)
                {
                    break;
                }

                // read data from steram
                try
                {
                    i = stream.Read(buffer, 0, bufferSize);
                }
                catch (IOException)
                {
                    continue;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                // No data (like client disconnected).
                if (i == 0)
                {
                    break;
                }

                // received data
                var receivedMessages = client.DataProcessor.ProcessReceivedRawData(buffer, i);
 
                
                // message received
                if(receivedMessages != null && receivedMessages.Any())
                {
                    #pragma warning disable
                    receivedMessages.ToList().ForEach(msg => Task.Run(async () =>
                    {
                        DataReceived(msg, client.Id);
                    }));
                    #pragma warning restore
                }

            }

            // Close connection.
            client.Client.Close();

            // trigger diconnected event
            Task.Run(() => {
                RaiseClientDisconnectedEvent(client);
            });

        }

        /// <summary>
        /// Message received handler 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="clientId"></param>
        private void DataReceived(byte[] msg, string clientId)
        {

            //  create message model 
            DataContainer model = new DataContainer { ClientId = clientId, Payload = msg, Time = DateTime.Now };

            // trigger event
            RaiseMsgReceivedEvent(model);

        }

    }
}
