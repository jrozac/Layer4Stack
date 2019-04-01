using Layer4Stack.Handlers;
using Layer4Stack.Models;
using System;
using System.Text;

namespace Layer4StackCmdDemo
{
    /// <summary>
    /// Client event handler
    /// </summary>
    public class ClientEventHandler : IClientEventHandler
    {

        /// <summary>
        /// Client connected to server
        /// </summary>
        /// <param name="senderObj"></param>
        /// <param name="info"></param>
        public void HandleClientConnected(ClientInfo info)
        {
            Console.WriteLine(string.Format("Client connected to server {0} on port {1}.", info.IpAddress, info.Port));
        }

        /// <summary>
        /// Client failed to connect to server
        /// </summary>
        /// <param name="info"></param>
        public void HandleClientConnectionFailure(ClientInfo info)
        {
            Console.WriteLine(string.Format("Client failed to connect to server {0} on port {1}.", info.IpAddress, info.Port));
        }

        /// <summary>
        /// Client disconnected from server.
        /// </summary>
        /// <param name="info"></param>
        public void HandleClientDisconnected(ClientInfo info)
        {
            Console.WriteLine(string.Format("Client disconnected from server {0} on port {1}.", info.IpAddress, info.Port));
        }

        /// <summary>
        /// Data received handler. Fired after processed by DataProcessor.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="rpcResponse"></param>
        /// <returns></returns>
        public byte[] HandleReceivedData(DataContainer data, bool rpcResponse = false)
        {
            string msg = Encoding.UTF8.GetString(data.Payload);
            Console.WriteLine(string.Format("A message received from server is {0}.", msg));
            return null;
        }

        /// <summary>
        /// Data sent handler.Fired after processed by DataProcessor.
        /// </summary>
        /// <param name="data"></param>
        public void HandleSentData(DataContainer data)
        {
            string msg = Encoding.UTF8.GetString(data.Payload);
            Console.WriteLine(string.Format("A message sent to server is {0}.", msg));
        }

    }
}
