using Layer4Stack.DataProcessors.Interfaces;
using Layer4Stack.Models;
using Layer4Stack.Models.Base;
using log4net;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Layer4Stack.Services.Base
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
        protected static ILog _logger = LogManager.GetLogger(typeof(TcpSocketBase<TConfig>));


        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="dataProcessor"></param>
        protected TcpSocketBase(IDataProcessorProvider dataProcessorProvider)
        {
            DataProcessorProvider = dataProcessorProvider;
        }


        /// <summary>
        /// Data processor provider
        /// </summary>
        protected IDataProcessorProvider DataProcessorProvider { get; set; }


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
            _logger.Debug(string.Format("Message of lenght {0} received.", model.Payload.Length));
            var eh = MsgReceivedEvent;
            if (eh != null)
            {
                eh(this, model);
            }
        }


        /// <summary>
        /// Raises message received event
        /// </summary>
        /// <param name="model"></param>
        protected void RaiseMsgSentEvent(DataContainer model)
        {
            _logger.Debug(string.Format("Message of lenght {0} sent.", model.Payload.Length));
            var eh = MsgSentEvent;
            if (eh != null)
            {
                eh(this, model);
            }
        }


        /// <summary>
        /// Raises client disconnected event
        /// </summary>
        /// <param name="model"></param>
        protected void RaiseClientDisconnectedEvent(ClientInfo model)
        {
            _logger.Info(string.Format("Client {0} disconnected.", model.Id));
            var eh = ClientDisconnectedEvent;
            if (eh != null)
            {
                eh(this, model);
            }
        }


        /// <summary>
        /// Raises client connected event
        /// </summary>
        /// <param name="model"></param>
        protected void RaiseClientConnectedEvent(ClientInfo model)
        {
            _logger.Info(string.Format("Client {0} connected.", model.Id));
            var eh = ClientConnectedEvent;
            if (eh != null)
            {
                eh(this, model);
            }
        }


        /// <summary>
        /// Send message
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        protected bool SendMessage(TcpClientInfo client, DataContainer message)
        {

            byte[] msg = client.DataProcessor.FilterSendData(message.Payload);

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
                _logger.Debug(string.Format("Data of lenght {0} sent.", data.Length));
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
                client.DataProcessor.ProcessReceivedRawData(buffer, i);

                // get message 
                byte[] receivedMessage = client.DataProcessor.GetNewData();
                
                // message received
                if(receivedMessage != null)
                {
                    #pragma warning disable
                    Task.Run(async () =>
                    {
                        DataReceived(receivedMessage, client.Id);
                    });
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
