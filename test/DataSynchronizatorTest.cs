using Layer4Stack.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Layer4StackTest
{

    [TestClass]
    public class DataSynchronizatorTest
    {

        /// <summary>
        /// Create data synchronizator
        /// </summary>
        /// <returns></returns>
        private DataSynchronizator<byte[]> CreateSynchronizator() => 
            new DataSynchronizator<byte[]>((new LoggerFactory().CreateLogger<DataSynchronizator<byte[]>>()));

        /// <summary>
        /// Test delivery and response
        /// </summary>
        [TestMethod]
        public void TestResponeDelivery()
        {
            // setup
            var sync = CreateSynchronizator();
            var id = "ID";
            int timeout = 100000;
            string result = "THIS IS RESULT";
            Stopwatch sw = new Stopwatch();

            // send request 
            var ev = new ManualResetEvent(false);
            sw.Start();
            var reqTask = Task.Run(() => {
                return sync.ExecuteActionAndWaitForResult(id, timeout, () => { ev.Set(); return true; });
            });

            // wait until id is properly set 
            ev.WaitOne();
            ev.Dispose();

            // set result 
            bool resStatus = sync.NotifyResult(id, Encoding.ASCII.GetBytes(result));
            Assert.IsTrue(resStatus);

            // wait request to complete 
            var resultTask = reqTask.GetAwaiter().GetResult();
            Assert.IsNotNull(resultTask);
            Assert.AreEqual(result, Encoding.ASCII.GetString(resultTask));

            // check that timeout does not overlap
            sw.Stop();
            Assert.IsTrue(timeout - sw.ElapsedMilliseconds > 0);
        }

        /// <summary>
        /// Test timeout
        /// </summary>
        [TestMethod]
        public void TestResponseTimeout()
        {
            // setup
            var sync = CreateSynchronizator();
            var id = "ID";
            int timeout = 10;

            // send request with timeout
            var result = sync.ExecuteActionAndWaitForResult(id, timeout, () => true);
            Assert.IsNull(result.Result);
            
            // set result 
            bool resStatus = sync.NotifyResult(id, Encoding.ASCII.GetBytes("FAKE RESUTL"));
            Assert.IsFalse(resStatus);
        }

        /// <summary>
        /// Test that exception in action is handled
        /// </summary>
        [TestMethod]
        public void TestActionExceptionIsHandled()
        {
            // setup
            var sync = CreateSynchronizator();
            var id = "ID";
            int timeout = 10;

            // send request with timeout
            var result = sync.ExecuteActionAndWaitForResult(id, timeout, () => throw new Exception());
            Assert.IsNull(result.Result);
        }


        [TestMethod]
        public void ParalelSyncTest()
        {

            // setup
            var sync = CreateSynchronizator();
            int timeout = 50000;
            var messages = 100;

            // create fake response function
            var createRsp = new Func<string, string>((r) => new string(r.Reverse().ToArray()));

            // requests ids random 
            var ids = Enumerable.Range(0, messages).ToList().
                Select((i) => Guid.NewGuid().ToString()).ToArray();

            // senders tasks 
            var senders = new ConcurrentDictionary<string, Task<byte[]>>();

            // tasks indicating that execution has started
            var inits = ids.ToDictionary(i => i, i => new TaskCompletionSource<bool>());
            
            // send requests in parallel
            var sendss = Parallel.ForEach(ids, (id) => {
                senders.TryAdd(id,
                Task.Run(() =>
                {
                    var result = sync.ExecuteActionAndWaitForResult(id, timeout, () =>
                    {
                        inits[id].SetResult(true);
                        return true;
                    });
                    return result;
                }));
            });
            Assert.IsTrue(sendss.IsCompleted);

            // wait until senders delegates were executed
            Task.WaitAll(inits.Values.Select(s => s.Task).ToArray());

            // fire resulst 
            Parallel.ForEach(ids, (id) => sync.NotifyResult(id, Encoding.ASCII.GetBytes(createRsp(id))));

            // wait until completed  senders
            Task.WaitAll(senders.Values.ToArray());

            // there are no active actions
            Assert.AreEqual(0, sync.ActiveCount());

            // expected and recieved results must be the same 
            senders.Keys.ToList().ForEach(id => {
                var exp = createRsp(id); // reversed id 
                var act = Encoding.ASCII.GetString(senders[id].Result);
                Assert.AreEqual(exp, act);
            });

            // clean up
            sync.Dispose();

        }

    }
}
