using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;

namespace Layer4Stack.Handlers
{

    /// <summary>
    /// Data synchronizator
    /// </summary>
    public class DataSynchronizator : IDisposable
    {

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<DataSynchronizator> _logger;

        /// <summary>
        /// Constructor with logger
        /// </summary>
        /// <param name="logger"></param>
        public DataSynchronizator(ILogger<DataSynchronizator> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Data item
        /// </summary>
        private class DataItem
        {
            public byte[] Id { get; set; }
            public byte[] Payload { get; set; }
            public TaskCompletionSource<byte[]> ResetEvent { get; set; }
            public bool Replaced { get; set; }
        }

        /// <summary>
        /// Data items
        /// </summary>
        private ConcurrentDictionary<string, DataItem> _items = new ConcurrentDictionary<string, DataItem>();

        /// <summary>
        /// Active count 
        /// </summary>
        /// <returns></returns>
        public int ActiveCount() => _items.Count;

        /// <summary>
        /// Action execute
        /// </summary>
        /// <param name="id"></param>
        /// <param name="timeoutMs"></param>
        /// <param name="actionDelegate"></param>
        /// <returns></returns>
        public async Task<byte[]> ExecuteAction(byte[] id, int timeoutMs, Func<bool> actionDelegate)
        {
            // try adding new item 
            var ids = GetIds(id);
            var item = new DataItem
            {
                Id = id,
                ResetEvent = new TaskCompletionSource<byte[]>()
            };
            bool status = _items.TryAdd(ids, item);

            // double key 
            if(!status)
            {
                _logger.LogWarning("Double id {id} detected.", GetIds(id));
                return null;
            }
            
            // declare response 
            byte[] rsp = null;

            // send request and wait for response 
            try
            {
                var delegateStatus = actionDelegate();
                if (!delegateStatus)
                {
                    _logger.LogError("Failed to execute action.");
                    return null;
                }
                await Wait(item,timeoutMs);
            }
            catch (Exception e)
            {
                return null;
            }
            finally
            {
                DataItem rspItem;
                if (_items.TryRemove(GetIds(id), out rspItem))
                {
                    rsp = rspItem.Payload;
                }
            }

            // return response
            return rsp;

        }

        /// <summary>
        /// Notify result
        /// </summary>
        /// <param name="id"></param>
        /// <param name="payload"></param>
        public bool NotifyResult(byte[] id, byte[] payload)
        {
            DataItem rspItem;
            var ids = GetIds(id);
            if (_items.TryGetValue(ids, out rspItem))
            {
                rspItem.Payload = payload;
                try
                {
                    rspItem.ResetEvent?.TrySetResult(payload);
                }
                catch (Exception e) { }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Wait for response or timeout
        /// </summary>
        /// <param name="item"></param>
        /// <param name="timeout"></param>
        private async Task Wait(DataItem item, int timeout)
        {
            await Task.WhenAny(item.ResetEvent.Task, Task.Delay(timeout));
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            _items = null;
        }

        public void Reset()
        {

        }

        /// <summary>
        /// Convert byte id to string id. It makes it possible to be matched in a dictionary.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private string GetIds(byte[] id) => Encoding.ASCII.GetString(id);
    }
}
