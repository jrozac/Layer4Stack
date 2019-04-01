using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Layer4Stack.Handlers
{

    /// <summary>
    /// Data synchronizator
    /// </summary>
    public class DataSynchronizator<TData> : IDisposable
    {

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<DataSynchronizator<TData>> _logger;

        /// <summary>
        /// Constructor with logger
        /// </summary>
        /// <param name="logger"></param>
        public DataSynchronizator(ILogger<DataSynchronizator<TData>> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Data item
        /// </summary>
        private class DataItem
        {
            public string Id { get; set; }
            public TData Payload { get; set; }
            public TaskCompletionSource<TData> ResetEvent { get; set; }
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
        /// Wait for result
        /// </summary>
        /// <param name="id"></param>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        public async Task<TData> WaitForResult(string id, int timeoutMs)
        {
            return await ExecuteActionAndWaitForResult(id, timeoutMs, null);
        }

        /// <summary>
        /// Action execute
        /// </summary>
        /// <param name="id"></param>
        /// <param name="timeoutMs"></param>
        /// <param name="actionDelegate"></param>
        /// <returns></returns>
        public async Task<TData> ExecuteActionAndWaitForResult(string id, int timeoutMs, Func<bool> actionDelegate)
        {
            // try adding new item 
            var item = new DataItem
            {
                Id = id,
                ResetEvent = new TaskCompletionSource<TData>()
            };
            bool status = _items.TryAdd(id, item);

            // double key 
            if(!status)
            {
                _logger.LogWarning("Double id {id} detected.", id);
                return default(TData);
            }
            
            // declare response 
            TData rsp = default(TData);

            // send request and wait for response 
            try
            {
                if(actionDelegate != null)
                {
                    var delegateStatus = actionDelegate();
                    if (!delegateStatus)
                    {
                        _logger.LogError("Failed to execute action.");
                        return default(TData);
                    }
                }
                await Wait(item,timeoutMs);
            }
            catch (Exception)
            {
                return default(TData);
            }
            finally
            {
                DataItem rspItem;
                if (_items.TryRemove(id, out rspItem))
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
        public bool NotifyResult(string id, TData payload)
        {
            bool result = false;
            DataItem rspItem;
            if (_items.TryGetValue(id, out rspItem))
            {
                rspItem.Payload = payload;
                try
                {
                    rspItem.ResetEvent?.TrySetResult(payload);
                }
                catch (Exception) { }
                result = true;
            }
            return result;
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

        /// <summary>
        /// Reset synchronizator
        /// </summary>
        public void Reset()
        {
            _items.Clear();
        }
    }
}
