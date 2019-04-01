using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;

namespace Layer4StackTest
{

    /// <summary>
    /// Tcp server service test 
    /// </summary>
    [TestClass]
    public class TcpServerServiceTest
    {

        /// <summary>
        /// Test server can disconnect clients 
        /// </summary>
        [TestMethod]
        public void TestClientDisconnect()
        {

            // create and start server 
            int port = 5555;
            var bundle = TestUtil.CreateTestServer<ServerEventHandler>(port, true);
            var server = bundle.Item1;
            var eventHandler = bundle.Item2;

            // client multiple clients 
            int count = 10;
            var clients = Enumerable.Range(1, count).Select(i => {
                var client = TestUtil.CreateTestNativeClient(port);
                TestUtil.Wait();
                Assert.AreEqual(i, eventHandler.ConnectedClients.Count);
                var id = eventHandler.ConnectedClients.Last().Id;
                return new Tuple<string, TcpClient>(id, client);
                }).ToList();
            Assert.AreEqual(count, clients.Count(c => c.Item2.Connected));
            Assert.AreEqual(count, server.Clients.Count);

            // disconnect n clients (server action)
            int discCount = 3;
            clients.Take(discCount).ToList().ForEach(c => Assert.IsTrue(server.DisconnectClient(c.Item1)));
            TestUtil.Wait();

            // disconnected client will not read data 
            clients.Take(discCount).ToList().ForEach((c) => {
                var buffer = new byte[2];
                Assert.AreEqual(0, c.Item2.GetStream().Read(buffer, 0, 2));
            });

            // Assert.AreEqual(count - discCount, clients.Count(c => c.Connected));
            Assert.AreEqual(count - discCount, server.Clients.Count());

            // stop server
            server.Stop();
        }

        /// <summary>
        /// Test server events are fired
        /// </summary>
        [TestMethod]
        public void TestServerEventsAreFired()
        {

            /// create server 
            int port = 5556;
            var bundle = TestUtil.CreateTestServer<ServerEventHandler>(port, true);
            var server = bundle.Item1;
            var eventHandler = bundle.Item2;
            var dataProcessor = bundle.Item3;
            TestUtil.Wait();

            // chech start event 
            Assert.IsTrue(eventHandler.ServerStarted);

            // client connect 
            TcpClient client = new TcpClient();
            client.Connect("127.0.0.1", port);
            TestUtil.Wait();
            Assert.IsTrue(eventHandler.ConnectedClients.Any(c => c.Time > DateTime.Now.AddMinutes(-1)));
            
            // recieved message
            string testMessage = "THIS IS TEST MESSAGE";
            client.GetStream().Write(dataProcessor.FilterSendData(TestUtil.ToByte(testMessage)));
            TestUtil.Wait();
            Assert.AreEqual(testMessage, eventHandler.RecievedMessage);

            // check that client disconnected event was triggered
            client.Close();
            TestUtil.Wait();
            Assert.IsTrue(eventHandler.DisonnectedClients.Any(c => c.Time > DateTime.Now.AddMinutes(-1)));

            // server start failure (same port start)
            var bundle2 = TestUtil.CreateTestServer<ServerEventHandler>(port, true);
            TestUtil.Wait();
            Assert.IsTrue(bundle2.Item2.ServerStartFailed);

            // stop server
            server.Stop();
            TestUtil.Wait();
            Assert.IsTrue(eventHandler.ServerStopped);

        }

        /// <summary>
        /// Test that server stop disconnectes the clients 
        /// </summary>
        [TestMethod]
        public void TestServerStopDisconnectsClients()
        {

            /// create server 
            int port = 5557;
            var bundle = TestUtil.CreateTestServer<ServerEventHandler>(port, true);
            var server = bundle.Item1;
            var eventHandler = bundle.Item2;
            var dataProcessor = bundle.Item3;

            // client connect 
            TcpClient client = new TcpClient();
            client.Connect("127.0.0.1", port);
            TestUtil.Wait();
            Assert.IsTrue(eventHandler.ConnectedClients.Any(c => c.Time > DateTime.Now.AddMinutes(-1)));

            // stop server
            server.Stop();
            TestUtil.Wait(1000);
            Assert.IsTrue(eventHandler.ServerStopped);
            Assert.IsTrue(eventHandler.DisonnectedClients.Any(c => c.Time > DateTime.Now.AddMinutes(-1)));

            // should not read data 
            var buffer = new byte[100];
            var read = client.GetStream().Read(buffer, 0, 100);
            Assert.AreEqual(0, read);

        }

        /// <summary>
        /// Test send to all clients 
        /// </summary>
        [TestMethod]
        public void TestSendToAllClients()
        {

            /// create server 
            int port = 5558;
            var bundle = TestUtil.CreateTestServer<ServerEventHandler>(port, true);
            var server = bundle.Item1;
            var eventHandler = bundle.Item2;
            var dataProcessor = TestUtil.CreateSimpleDataProcessor();

            // connect clients 
            int count = 2;
            var clients = Enumerable.Range(0, count).Select(i => TestUtil.CreateTestNativeClient(port)).ToList();
            TestUtil.Wait();
            Assert.AreEqual(count, server.Clients.Count());

            // send message from server to all clients 
            string message = "TEST MESSAGE";
            var sent = server.SendToAll(TestUtil.ToByte(message));
            Assert.AreEqual(count, sent);

            // read message from clients 
            var recieved = clients.Select(c => {
                var buffer = new byte[200];
                var cnt = c.GetStream().Read(buffer, 0, 200);
                Assert.AreNotEqual(0, cnt);
                var rcnt = dataProcessor.ProcessReceivedRawData(buffer, cnt);
                Assert.AreEqual(1, rcnt.Count());
                return TestUtil.ToString(rcnt.First());
            });

            // messages must be the same 
            recieved.ToList().ForEach(msg => Assert.AreEqual(message, msg));

        }

        /// <summary>
        /// Test server is agnostic to event handler errors 
        /// </summary>
        [TestMethod]
        public void TestServerIsAgnosticToEventHandlerErrors()
        {

            // start server 
            var watch = Stopwatch.StartNew();
            int port = 5559;
            var bundle = TestUtil.CreateTestServer<BadServerEventHandler>(port, true);
            var server = bundle.Item1;
            var eventHandler = bundle.Item2;
            var dataProcessor = TestUtil.CreateSimpleDataProcessor();
            TestUtil.Wait();
            Assert.IsTrue(watch.ElapsedMilliseconds < 2000);
            Assert.AreEqual(1, eventHandler.Counter);

            // client connect 
            watch = Stopwatch.StartNew();
            var client = TestUtil.CreateTestNativeClient(port);
            TestUtil.Wait();
            Assert.IsTrue(watch.ElapsedMilliseconds < 2000);
            Assert.AreEqual(2, eventHandler.Counter);
       
            // recieve data from client
            watch = Stopwatch.StartNew();
            var clientDataProcessor = TestUtil.CreateSimpleDataProcessor();
            client.GetStream().Write(clientDataProcessor.FilterSendData(TestUtil.ToByte("TEST MESSAGE")));
            TestUtil.Wait();
            client.GetStream().Write(clientDataProcessor.FilterSendData(TestUtil.ToByte("TEST MESSAGE")));
            TestUtil.Wait();
            Assert.IsTrue(watch.ElapsedMilliseconds < 3000);
            Assert.AreEqual(4, eventHandler.Counter);

            // send data to client 
            watch = Stopwatch.StartNew();
            server.SendToClient(eventHandler.ConnectedClients.First().Id, TestUtil.ToByte("TEST MESSAGE"));
            TestUtil.Wait();
            Assert.IsTrue(watch.ElapsedMilliseconds < 2000);
            Assert.AreEqual(5, eventHandler.Counter);
         
            // client disconnect 
            watch = Stopwatch.StartNew();
            client.GetStream().Close();
            client.Close();
            TestUtil.Wait();
            Assert.IsTrue(watch.ElapsedMilliseconds < 2000);
            Assert.AreEqual(6, eventHandler.Counter);

            // stop server 
            watch = Stopwatch.StartNew();
            server.Stop();
            TestUtil.Wait();
            Assert.IsTrue(watch.ElapsedMilliseconds < 2000);
            // Assert.AreEqual(7, eventHandler.Counter);

            // no clients remain
            Assert.AreEqual(0, server.Clients.Count());
        }

        /// <summary>
        /// Test server responds as defined in event handler
        /// </summary>
        [TestMethod]
        public void TestServerResponse()
        {
            // create server 
            int port = 5560;
            var serverBundle = TestUtil.CreateTestServer<ServerEventHandler>(port, true);
            var server = serverBundle.Item1;
            var serverEventHandler = serverBundle.Item2;
            Assert.IsTrue(server.Started);

            // create client 
            var clinetBundle = TestUtil.CreateClient<ClientEventHandler>(port, true);
            var client = clinetBundle.Item1;
            var eventHandler = clinetBundle.Item2;
            TestUtil.Wait();
            Assert.IsTrue(client.Connected);

            // client sends message
            string msg = "TEST MESSAGE";
            string rsp = "RESPONSE TEST MESSAGE";
            serverEventHandler.Response = rsp;
            bool sent = client.Send(TestUtil.ToByte(msg));
            Assert.IsTrue(sent);

            // server should return static responss
            TestUtil.Wait(1000);
            Assert.IsNotNull(eventHandler.Recieved);
            Assert.AreEqual(rsp, eventHandler.Recieved);

            // stop 
            server.Stop();

        }

    }
}
