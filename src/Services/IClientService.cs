﻿using System;
using System.Threading.Tasks;

namespace Layer4Stack.Services
{

    /// <summary>
    /// Client service 
    /// </summary>
    public interface IClientService : IDisposable
    {

        /// <summary>
        /// Connect to server
        /// </summary>
        Task<bool> ConnectAsync();

        /// <summary>
        /// Connect to server 
        /// </summary>
        /// <returns></returns>
        bool Connect();

        /// <summary>
        /// Disconnects from server
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Send data to server 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        Task<bool> SendAsync(byte[] data);

        /// <summary>
        /// Send data to server 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        bool Send(byte[] data);

        /// <summary>
        /// Remote procedure call
        /// </summary>
        /// <param name="req"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        Task<byte[]> RpcAsync(byte[] req, int timeout);

        /// <summary>
        /// Remote procedure call
        /// </summary>
        /// <param name="req"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        byte[] Rpc(byte[] req, int timeout);

        /// <summary>
        /// Connected status
        /// </summary>
        bool Connected { get; }

    }
}
