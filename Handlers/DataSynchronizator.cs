using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Layer4Stack.Handlers
{

    /// <summary>
    /// Data synchronizator
    /// </summary>
    public class DataSynchronizator : IDisposable
    {

        /// <summary>
        /// Data item
        /// </summary>
        private class DataItem
        {
            public byte[] Id { get; set; }
            public byte[] Payload { get; set; }
            public ManualResetEvent ResetEvent { get; set; }
        }

        /// <summary>
        /// Data items
        /// </summary>
        private ConcurrentDictionary<byte[], DataItem> _items = new ConcurrentDictionary<byte[], DataItem>();

        /// <summary>
        /// Action execute
        /// </summary>
        /// <param name="id"></param>
        /// <param name="timeoutMs"></param>
        /// <param name="actionDelegate"></param>
        /// <returns></returns>
        public byte[] ExecuteAction(byte[] id, int timeoutMs, Func<bool> actionDelegate)
        {

            // set message for send
            var ev = new ManualResetEvent(false);
            bool status = _items.TryAdd(id, new DataItem
            {
                Id = id,
                ResetEvent = ev
            });

            // declare response 
            byte[] rsp = null;

            // send request and wait for response 
            try
            {
                bool sentStatus = actionDelegate();
                ev.WaitOne(timeoutMs);
            }
            catch (Exception e)
            {
                return null;
            }
            finally
            {
                DataItem rspItem;
                if (_items.TryRemove(id, out rspItem))
                {
                    rsp = rspItem.Payload;
                    try
                    {
                        rspItem.ResetEvent?.Dispose();
                    }
                    catch (Exception e) { }
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
            if (_items.TryGetValue(id, out rspItem))
            {
                rspItem.Payload = payload;
                try
                {
                    rspItem.ResetEvent?.Set();
                }
                catch (Exception e) { }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            _items.Where(d => d.Value.ResetEvent != null).Select(d => d.Value.ResetEvent).ToList().ForEach(e => e.Dispose());
        }
    }
}
