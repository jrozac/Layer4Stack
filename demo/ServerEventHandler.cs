using Layer4Stack.Handlers.Interfaces;
using Layer4Stack.Models;
using System;
using System.Text;

namespace Layer4StackCmdDemo
{
    /// <summary>
    /// Server event handler
    /// </summary>
    public class ServerEventHandler : IServerEventHandler
    {

        /// <summary>
        /// Request disconnect event
        /// </summary>
        public EventHandler<string> EventRequestDisconnect;

        /// <summary>
        /// Request stop server
        /// </summary>
        public EventHandler<string> EventRequestStop;

        /// <summary>
        /// It displays a new client connected message.
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="info"></param>
        public void HandleClientConnected(ClientInfo info)
        {
            Console.WriteLine(string.Format("Client {0} with IP {1} connected on port {2}.", info.Id, info.IpAddress, info.Port));
        }

        /// <summary>
        /// It displays a client disconnected message.
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="info"></param>
        public void HandleClientDisconnected(ClientInfo info)
        {
            Console.WriteLine(string.Format("Client {0} with IP {1} disconnected on port {2}.", info.Id, info.IpAddress, info.Port));
        }

        /// <summary>
        /// Handles received data.
        /// It disconnects a client if "exit" received.
        /// It stops the server if "stop" recieved.
        /// In all other cases it reverses the string and sends it back to client.
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public byte[] HandleReceivedData(DataContainer data)
        {
            // log received message 
            string msg = Encoding.UTF8.GetString(data.Payload);
            Console.WriteLine("A message '{0}' was received from client {1}.", data.ClientId, msg);

            // disconnect client if exit received 
            if (msg.Trim().ToLowerInvariant() == "exit")
            {
                EventRequestDisconnect?.Invoke(this, data.ClientId);
                return null;
            }

            // stop server if stop received
            if (msg.Trim().ToLowerInvariant() == "stop")
            {
                EventRequestStop?.Invoke(this, data.ClientId);
                return null;
            }

            // reverse message 
            char[] charArray = msg.ToCharArray();
            Array.Reverse(charArray);
            msg = new string(charArray);
            return Encoding.UTF8.GetBytes(msg);
        }

        /// <summary>
        /// Displays received data.
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="data"></param>
        public void HandleSentData(DataContainer data)
        {
            string msg = Encoding.UTF8.GetString(data.Payload);
            Console.WriteLine("A message '{0}' was sent to client {1}.", data.ClientId, msg);
        }

        /// <summary>
        /// Displays a message server started.
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="config"></param>
        public void HandleServerStarted(ServerConfig config)
        {
            Console.WriteLine("Server started.");
        }

        /// <summary>
        /// Displays a message server failed to start
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="config"></param>
        public void HandleServerStartFailure(ServerConfig config)
        {
            Console.WriteLine("Server failed to start.");
        }

        /// <summary>
        /// Displays a message stopped
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="config"></param>
        public void HandleServerStopped(ServerConfig config)
        {
            Console.WriteLine("Server stopped.");
        }

    }
}
