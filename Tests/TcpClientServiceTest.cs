using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace Layer4StackTest
{

    /// <summary>
    /// Test tcp client service 
    /// </summary>
    [TestClass]
    public class TcpClientServiceTest
    {
 
        /// <summary>
        /// Test auto reconnect
        /// </summary>
        [TestMethod]
        public void TestClientReconnectsOnDisconnected()
        {

            // create test server 
            int port = 6555;
            var server = TestUtil.CreateTestServer<ServerEventHandler>(port, true).Item1;

            // create client 
            var client = TestUtil.CreateClient<ClientEventHandler>(port, true, true).Item1;
            Assert.IsTrue(client.Connected);
            TestUtil.Wait();

            // disconnect client (from server)
            server.DisconnectClient(server.Clients.First().Id);
            TestUtil.Wait();
            Assert.IsFalse(client.Connected);

            // wait (to be reconnected);
            TestUtil.Wait(10000);
            Assert.IsTrue(client.Connected);

            // stop server 
            server.Stop();
            TestUtil.Wait();
            Assert.IsFalse(client.Connected);

        }

        /// <summary>
        /// Test client events are fired
        /// </summary>
        [TestMethod]
        public void TestClientEventsAreFired()
        {

            // create test server 
            int port = 6556;
            var server = TestUtil.CreateTestServer<ServerEventHandler>(port, true).Item1;

            // create client 
            var eventHandler = new ClientEventHandler();
            var bundle = TestUtil.CreateClient<ClientEventHandler>(port, true, false);
            var client = bundle.Item1;
            var handler = bundle.Item2;
            TestUtil.Wait();
            Assert.IsTrue(client.Connected);
            Assert.AreEqual(1, handler.Count);

            // server stop (client will disconnect)
            server.Stop();
            TestUtil.Wait();
            Assert.AreEqual(2, handler.Count);

            // client connect again
            Assert.IsTrue(server.Start());
            client.Connect();
            TestUtil.Wait();
            Assert.AreEqual(3, handler.Count);

            // test recieve message 
            string msg = "TEST MESSAGE";
            Assert.AreEqual(1, server.SendToAll(TestUtil.ToByte(msg)));
            TestUtil.Wait();
            Assert.AreEqual(4, handler.Count);
            Assert.AreEqual(msg, handler.Recieved);

            // test send message 
            client.Send(TestUtil.ToByte(msg));
            TestUtil.Wait();
            Assert.AreEqual(5, handler.Count);
            Assert.AreEqual(msg, handler.Sent);

            // server stop 
            server.Stop();
            TestUtil.Wait();
            Assert.IsFalse(client.Connected);
            Assert.AreEqual(6, handler.Count);

            // failed connect 
            client.Connect();
            TestUtil.Wait();
            Assert.AreEqual(7, handler.Count);
        }

        /// <summary>
        /// Test rpc 
        /// </summary>
        [TestMethod]
        public void TestRpc()
        {

            // create test server 
            int port = 6557;
            var serverBundle = TestUtil.CreateTestServer<ServerEventHandler>(port, true);
            var server = serverBundle.Item1;
            var handler = serverBundle.Item2;
            handler.Response = "TEST MESSAGE RECIEVED";

            // create client 
            var client = TestUtil.CreateClient<ClientEventHandler>(port, true).Item1;
            Assert.IsTrue(client.Connected);

            // test rpc 
            var msg = "TEST MESSAGE";
            var rspRpc = client.Rpc(TestUtil.ToByte(msg), 2000);
            Assert.IsNotNull(rspRpc);
            var rspMsg = TestUtil.ToString(rspRpc);
            Assert.AreEqual(msg + " RECIEVED", rspMsg);

            // server stop
            server.Stop();
            TestUtil.Wait();
            Assert.IsFalse(client.Connected);

        }

    }
}
