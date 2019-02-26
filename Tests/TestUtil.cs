using Layer4Stack.DataProcessors;
using Layer4Stack.Handlers;
using Layer4Stack.Handlers.Interfaces;
using Layer4Stack.Models;
using Layer4Stack.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Layer4StackTest
{

    /// <summary>
    /// Test util 
    /// </summary>
    internal static class TestUtil
    {

        /// <summary>
        /// Logger factory 
        /// </summary>
        private static LoggerFactory _loggerFactory = new LoggerFactory();

        /// <summary>
        /// Convert string to byte array 
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static byte[] ToByte(string msg) => Encoding.ASCII.GetBytes(msg);

        /// <summary>
        /// Convert byte array to string 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ToString(byte[] bytes) => Encoding.ASCII.GetString(bytes);

        /// <summary>
        /// Wait a bit 
        /// </summary>
        /// <param name="ms"></param>
        public static void Wait(int? ms = null) => Task.Delay(ms ?? 200).GetAwaiter().GetResult();

        /// <summary>
        /// Creates a test server 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        public static Tuple<IServerService, TEventHandler, SimpleMessageDataProcessor>
            CreateTestServer<TEventHandler>(int port, bool start = false)
            where TEventHandler : IServerEventHandler
        {
            var dataProcessor = SimpleMessageDataProcessor.CreateHsmProcessor(_loggerFactory.CreateLogger<SimpleMessageDataProcessor>());
            var eventHandler = Activator.CreateInstance<TEventHandler>();
            IServerService server = new TcpServerService(eventHandler, new ServerConfig("127.0.0.1", port),
                _loggerFactory, EnumDataProcessorType.Hsm, (b) => b.ToList().Take(1).ToArray());
            if (start)
            {
                server.Start();
            }
            return new Tuple<IServerService, TEventHandler, SimpleMessageDataProcessor>(server, eventHandler, dataProcessor);
        }

        /// <summary>
        /// Creates and returns native client 
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static TcpClient CreateTestNativeClient(int port)
        {
            TcpClient client = new TcpClient();
            client.Connect("127.0.0.1", port);
            return client;
        }

        /// <summary>
        /// Creates a simple data processor
        /// </summary>
        /// <returns></returns>
        public static IDataProcessor CreateSimpleDataProcessor()
        {
            var dataProcessor = SimpleMessageDataProcessor.CreateHsmProcessor(_loggerFactory.CreateLogger<SimpleMessageDataProcessor>());
            return dataProcessor;
        }

        /// <summary>
        /// Create client 
        /// </summary>
        /// <typeparam name="TEventHandler"></typeparam>
        /// <param name="port"></param>
        /// <param name="connect"></param>
        /// <param name="autoReconnect"></param>
        /// <returns></returns>
        public static Tuple<IClientService, TEventHandler> CreateClient<TEventHandler>(int port, bool connect = false, bool autoReconnect = false)
            where TEventHandler : IClientEventHandler
        {
            var eventHandler = Activator.CreateInstance<TEventHandler>();
            var client = new TcpClientService(eventHandler,
                new ClientConfig("127.0.0.1", port, autoReconnect),
                _loggerFactory, EnumDataProcessorType.Hsm, (b) => b.ToList().Take(1).ToArray());
            if (connect)
            {
                client.Connect();
            }
            return new Tuple<IClientService, TEventHandler>(client, eventHandler);
        }

    }

    /// <summary>
    /// Bad event handler, full of errors and throws
    /// </summary>
    public class BadServerEventHandler : IServerEventHandler
    {

        public int Counter;
        public List<ClientInfo> ConnectedClients = new List<ClientInfo>();

        private void Error()
        {
            Interlocked.Increment(ref Counter);
            Task.Delay(5000000).GetAwaiter().GetResult();
            throw new NotImplementedException();
        }

        public void HandleClientConnected(ClientInfo info)
        {
            ConnectedClients.Add(info);
            Error();
        }

        public void HandleClientDisconnected(ClientInfo info)
        {
            Error();
        }

        public byte[] HandleReceivedData(DataContainer data)
        {
            Error();
            return null;
        }

        public void HandleSentData(DataContainer data)
        {
            Error();
        }

        public void HandleServerStarted(ServerConfig config)
        {
            Error();
        }

        public void HandleServerStartFailure(ServerConfig config)
        {
            Error();
        }

        public void HandleServerStopped(ServerConfig config)
        {
            Error();
        }
    }

    /// <summary>
    /// Event handler for test 
    /// </summary>
    public class ServerEventHandler : IServerEventHandler
    {

        public List<ClientInfo> ConnectedClients = new List<ClientInfo>();
        public List<ClientInfo> DisonnectedClients = new List<ClientInfo>();
        public bool ServerStopped;
        public bool ServerStarted;
        public string RecievedMessage;
        public bool ServerStartFailed;
        public string Response;

        public void HandleClientConnected(ClientInfo info)
        {
            ConnectedClients.Add(info);
        }

        public void HandleClientDisconnected(ClientInfo info)
        {
            DisonnectedClients.Add(info);
        }

        public byte[] HandleReceivedData(DataContainer data)
        {
            RecievedMessage = Encoding.ASCII.GetString(data.Payload);
            return Response != null ? TestUtil.ToByte(Response) : null;
        }

        public void HandleSentData(DataContainer data)
        {

        }

        public void HandleServerStarted(ServerConfig config)
        {
            ServerStarted = true;
        }

        public void HandleServerStartFailure(ServerConfig config)
        {
            ServerStartFailed = true;
        }

        public void HandleServerStopped(ServerConfig config)
        {
            ServerStopped = true;
        }
    }

    /// <summary>
    /// Client event handler
    /// </summary>
    public class ClientEventHandler : IClientEventHandler
    {
        public int Count;
        public string Recieved;
        public string Sent;

        public void HandleClientConnected(ClientInfo info)
        {
            Interlocked.Increment(ref Count);
        }

        public void HandleClientConnectionFailure(ClientInfo info)
        {
            Interlocked.Increment(ref Count);
        }

        public void HandleClientDisconnected(ClientInfo info)
        {
            Interlocked.Increment(ref Count);
        }

        public byte[] HandleReceivedData(DataContainer data, bool rpcResponse = false)
        {
            Interlocked.Increment(ref Count);
            Recieved = TestUtil.ToString(data.Payload);
            return null;
        }

        public void HandleSentData(DataContainer data)
        {
            Interlocked.Increment(ref Count);
            Sent = TestUtil.ToString(data.Payload);
        }
    }

}
