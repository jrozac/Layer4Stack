using Layer4Stack.DataProcessors;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Layer4StackTest
{

    /// <summary>
    /// Message data processor test
    /// </summary>
    [TestClass]
    public class MessageDataProcessorTest
    {

        /// <summary>
        /// Create test messages 
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        private string[] CreateTestMessages(int count)
        {
            var rand = new Random();
            return Enumerable.Range(0, count).Select(id =>
            {
                var msg = Guid.NewGuid().ToString();
                msg = msg.Substring(0, rand.Next(3, msg.Length));
                return msg;
            }).ToArray();
        }

        /// <summary>
        /// Create data processor
        /// </summary>
        /// <param name="terminator"></param>
        /// <param name="useHeader"></param>
        /// <param name="synchronizator"></param>
        /// <returns></returns>
        private MessageDataProcessor CreateDataProcessor(bool useHeader = false, string terminator = "XXXXX", string synchronizator = null)
        {
            var logp = new LoggerFactory();
            var cfg = new MessageDataProcessorConfig(5000,
                terminator != null ? Encoding.ASCII.GetBytes(terminator) : null, useHeader,
                synchronizator != null ? Encoding.ASCII.GetBytes(synchronizator) : null);
            return new MessageDataProcessor(cfg, logp.CreateLogger<MessageDataProcessor>());
        }

        /// <summary>
        /// Check that messages are correctly recieved with length header only 
        /// </summary>
        [TestMethod]
        public void TestMultiMessageDeliveryLengthHeaderOnly()
        {

            TestMultiMessageRandomBufferDelivery(true, null);
        }

        /// <summary>
        /// Test delivery with terminator and length header 
        /// </summary>
        [TestMethod]
        public void TestMultiMessageDeliveryLengthHeaderAndTerminator()
        {
            TestMultiMessageRandomBufferDelivery(true, "XXXXX");
        }

        /// <summary>
        /// Test delivery with terminator
        /// </summary>
        [TestMethod]
        public void TestMultiMessageDeliveryTerminatorOnly()
        {
            TestMultiMessageRandomBufferDelivery(false, "XXXXX");
        }

        /// <summary>
        /// Messages are sent in random packages length. At the end all messages must be recieved properly.
        /// </summary>
        /// <param name="useHeader"></param>
        /// <param name="terminator"></param>
        private void TestMultiMessageRandomBufferDelivery(bool useHeader = false, string terminator = "XXXXX")
        {
            // all messages
            int allMessages = 500;

            // random generator 
            var random = new Random();

            // data processor
            var proc = CreateDataProcessor(useHeader, terminator);

            // create some messages to be sent 
            var msgs = CreateTestMessages(allMessages).Select(m => Encoding.ASCII.GetBytes(m)).ToArray();

            // add messages to buffer
            var buffer = new List<byte>();
            msgs.ToList().ForEach(m => buffer.AddRange(proc.FilterSendData(m)));

            // send buffer in random steps 
            var bufferSent = new List<byte>();
            var recieved = new List<byte[]>();
            int length = 0;
            for(int i = 0; i < buffer.Count(); i+=length)
            {
                length = random.Next(10, buffer.Count()) / allMessages;
                length = Math.Min(length, buffer.Count() - i);
                var data = new byte[length];
                Buffer.BlockCopy(buffer.ToArray(), i, data, 0, length);
                var item = proc.ProcessReceivedRawData(data, length);
                bufferSent.AddRange(data);
                if(item != null)
                {
                    recieved.AddRange(item);
                }
            }
            Assert.IsTrue(buffer.SequenceEqual(bufferSent));
            
            // check that all messegaes are recieved 
            Assert.AreEqual(allMessages, recieved.Count());
            var responses = recieved.Select(m => Encoding.ASCII.GetString(m)).ToList();
            var requests = msgs.Select(m => Encoding.ASCII.GetString(m)).ToList();
            Assert.IsTrue(requests.SequenceEqual(responses));

        }

        /// <summary>
        /// Messages are sent in random packages length. At the end all messages must be recieved properly.
        /// </summary>
        [TestMethod]
        public void TestMultiMessageRandomBufferDeliveryFixedMessages()
        {

            // test data 
            string msgsRaw = "e6bd4d8d-1db1-473e-aa03-4e1f42;910f0edc-1877-4370-8afb;e0d59638-8e0d-44aa-a5b6-be709cbd;6dc8;5242df37-89a9-4037-9269-14cf883;f210600e-bf33-4b0;3b8bf6d2-7384-;0d83c34a-ab;9eb4dcb3-f95c-46a6-8786-;aa3a213;5d04e6ef-0a5a-4ce6;97aeec43-3950-4291-9181-5;68fbc107-e786-4119-bd;b404040f-95d4-44d8-bf90-;2d6b2";
            string stepsRaw = "25,0,22,6,13,24,10,21,15,20,0,2,14,12,11,15,5,10,12,13,1,3,6,20,15,15,9,0,15,3,1,9,1,12,5,22,11,0,16,5,1,3,22,22,18,16,22,12,9,11,1,17,10,8,9,22,19,1,16,23,14,10,6,11,18,16,0,2,7,15,3,5,19,7,11,23,6,10,1,24,19,22,16,4,2,10,24,18,2,8,2,9,8,19,15,16,22,10,3,1,15,19,5,19,7,21,23,2,22,11,15,9,25,17,4,17,18,1,9,8,17,7,25,23,12,19,21,25,17,20,0,21,7,0,9,24,17,23,22,6,12,22,21,16,18,18,9,16,15,23,23,21,1,20,9,7,20,6,6,13,25,23,10,2,10,20,2,25,8,5,0,21,5,2,21,20,11,8,5,20,10,5,10,19,21,5,19,12,15,1,4,10,16,8,6,14,11,19,5,24,15,9,7,9,23,2,15,24,24,14,21,11,1,12,3,20,13,16,6,5,0,15,9,7,0,14,4,22,21,7,9,18,2,6,4,0,23,0,21,19,15,20,13,1,5,18,9,4,5,11,0,20,13,0,23,25,0,0,9,8,23,19,2,16,5,3,9,20,7,2,22,13,20,12,15,6,1,7,1,8,5,25,16,9,7,14,5,2,9,4,0,22,16,13,20,21,17,22,22,2,9,15,22,3,14,12,0,0,10,16,23,17,24,0,3,24,2,18,9,16,23,7,10,3,2,0,22,7,17,19,14,4,11,24,24,15,2,13,20,20,14,4,17,18,6,7,23,15,9,22,19,24,13,5,4,1,4,19,0,14,15,18,9,16,2,19,6,25,1,13,21,14,23,14,2,18,15,18,22,20,1,7,23,20,0,15,16,6,19,22,11,8,12,5,5,21,21,1,15,12,1,24,14,22,1,4,1,25,2,11,12,11,23,6,13,8,20,14,15,13,2,0,5,4,1,12,1,17,8,6,22,13,2,9,18,2,18,1,19,21,13,16,3,7,10,25,11,6,18,23,1,6,11,2,10,1,19,9,20,14,2,9,16,21,14,24,8,13,12,10,14,19,21,19,25,11,22,22,19,2,1,20,24,10,5,16,2,0,24,17,9,9,6,5,19,3,25,6,18,12,16,23,19,8,8,7,10,16,2,24,4,21,15,18,12,7,11,6,16,23,21,8,14,20,0,5,12,16,0,21,11,15,4,7,10,3,1,16,2,23,5,11,23,25,13,13,18,13,17,11,18,19,16,20,16,8,8,24,7,2,20,20,16,6,13,13,17,8,7,8,6,20,4,18,18,10,10,15,7,13,12,24,23,14,3,17,25,9,16,2,21,2,8,25,3,10,20,16,11,25,20,10,6,9,5,23,14,23,2,6,23,18,16,19,15,21,22,1,3,21,14,8,17,4,12,3,11,4,15,7,15,19,3,7,18,2,18,14,8,5,3,1,6,10,4,3,11,15,10,7,14,18,12,16,11,12,18,1,22,2,11,9,8,20,16,4,15,9,1,12,2,7,18,23,18,12,1,7,0,9,17,22,20,7,19,22,6,12,22,5,10,17,17,6,20,18,19,12,19,0,13,2,13,23,5,6,25,15,23,24,10,11,0,12,10,14,15,1,1,20,23,2,12,20,2,10,15,12,3,25,18,14,13,0,21,15,8,22,1,12,22,17,14,23,20,24,16,5,21,5,3,18,11,24,2,11,14,18,20,20,16,12,11,23,5,21,23,0,16,21,6,17,25,14,23,6,13,15,25,3,19,10,17,23,21,0,3,14,23,10,0,15,2,6,21,9,5,16,17,25,1,16,14,25,24,11,18,22,0,22,23,25,19,16,0,6,21,14,1,4,17,8,2,5,20,16,15,18,18,14,0,21,22,0,2,3,20,17,24,10,7,6,13,0,12,0,21,11,6,14,13,16,14,12,7,18,13,9,4,8,21,0,4,12,21,1,2,18,14,22,11,3,15,16,22,11,20,1,18,20,20,4,12,3,0,9,7,9,18,9,15,11,12,16,15,7,2,1,5,10,23,2,21,9,3,9,1,23,15,21,23,11,16,9,13,17,13,0,11,11,20,18,0,5,7,25,16,25,1,14,9,19,22,7,22,17,7,2,0,16,8,8,19,21,12,3,20,2,2,23,19,1,15,19,16,5,1,16,1,3,18,1,22,1,12,3,15,9,12,21,9,10,25,20,2,11,24,4,18,4,4,14,22,3,20,7,3,3,16,17,15,17,1,23,23,7,12,18,8,8,13,11,15,6,14,8,2,20,16,4,6,22,23,22,23,4,4,18,6,5,25,12,17,20,6,17,9,1,22,21,24,5,21,10,16,19,23,16,14,11,5,9,12,1";
            var msgsClear = msgsRaw.Split(';');
            var steps = stepsRaw.Split(',').Select(s => int.Parse(s)).ToArray();
            int allMessages = msgsClear.Count();

            // data processor
            var proc = CreateDataProcessor(true);

            // create some messages to be sent 
            var msgs = msgsClear.Select(m => Encoding.ASCII.GetBytes(m)).ToArray();

            // add messages to buffer
            var buffer = new List<byte>();
            msgs.ToList().ForEach(m => buffer.AddRange(proc.FilterSendData(m)));

            // send buffer in random steps 
            var bufferSent = new List<byte>();
            var recieved = new List<byte[]>();
            int read = 0;
            for(int i=0; i<steps.Length && read < buffer.Count; i++)
            {
                var length = steps[i];
                length = Math.Min(length, buffer.Count() - read);
                var data = new byte[length];
                Buffer.BlockCopy(buffer.ToArray(), read, data, 0, length);
                var item = proc.ProcessReceivedRawData(data, length);
                bufferSent.AddRange(data);
                if (item != null)
                {
                    recieved.AddRange(item);
                }
                read += length;
            }

            // buffers are equal
            Assert.IsTrue(buffer.SequenceEqual(bufferSent));

            // check that all messegaes are recieved 
            Assert.AreEqual(allMessages, recieved.Count());
            var responses = recieved.Select(m => Encoding.ASCII.GetString(m)).ToList();
            var requests = msgs.Select(m => Encoding.ASCII.GetString(m)).ToList();
            Assert.IsTrue(requests.SequenceEqual(responses));

        }

        /// <summary>
        /// Test synchronization
        /// </summary>
        [TestMethod]
        public void TestSynchronizationWithSyncData()
        {

            var proc = CreateDataProcessor(true, null, "!!!!");
            var bufferMax = proc.Config.MaxLength;
            string msg = "!!!!THIS IS MY MESSAGE!";

            // create buffer too set data out of sync
            var header = new byte[] { 99, 99 };
            var buffer = new byte[bufferMax + 300];
            buffer[0] = 100;

            // set sychrnoizator and message 
            var msgPacket = proc.FilterSendData(Encoding.ASCII.GetBytes(msg));
            Buffer.BlockCopy(msgPacket, 0, buffer, bufferMax + 150, msgPacket.Length);

            // send buffer
            int step = 100;
            int sent = 0;
            List<byte[]> rec = new List<byte[]>();
            while(sent < buffer.Length)
            {
                int size = Math.Min(step, buffer.Length - sent);
                var items = proc.ProcessReceivedRawData(buffer.Skip(sent).Take(size).ToArray(), size);
                if(items != null && items.Any())
                {
                    rec.AddRange(items);
                    break;
                }
                sent += size;
            }

            // assert message is recieved, therefore synchronization was performed
            Assert.AreEqual(1, rec.Count());
            Assert.AreEqual(msg, Encoding.ASCII.GetString(rec.First()));
        }

        /// <summary>
        /// Test synchronization with terminator
        /// </summary>
        [TestMethod]
        public void TestSynchronizationWithTerminator()
        {

            var proc = CreateDataProcessor(true, "XXXXXXX");
            var bufferMax = proc.Config.MaxLength;
            string msg = "!!!!THIS IS MY MESSAGE!";
            msg = "MMMMMMMMMMMMMMMMMMMMMMM";

            // create buffer too set data out of sync
            var header = new byte[] { 99, 99 };
            var buffer = new byte[bufferMax + 300];
            buffer[0] = 100;

            // set sychrnoizator and message (add it twice, first will be not returned due to out of sync)
            var msgPacket = proc.FilterSendData(Encoding.ASCII.GetBytes(msg)).ToList();
            msgPacket.AddRange(msgPacket);
            Buffer.BlockCopy(msgPacket.ToArray(), 0, buffer, bufferMax + 150, msgPacket.Count());

            // send buffer
            int step = 100;
            int sent = 0;
            List<byte[]> rec = new List<byte[]>();
            while (sent < buffer.Length)
            {
                int size = Math.Min(step, buffer.Length - sent);
                var items = proc.ProcessReceivedRawData(buffer.Skip(sent).Take(size).ToArray(), size);
                if (items != null && items.Any())
                {
                    rec.AddRange(items);
                    break;
                }
                sent += size;
            }

            // assert message is recieved, therefore synchronization was performed
            Assert.AreEqual(1, rec.Count());
            Assert.AreEqual(msg, Encoding.ASCII.GetString(rec.First()));
        }

        /// <summary>
        /// Test that nothing goes wrong when zero lenght buffer is delivered
        /// </summary>
        [TestMethod]
        public void TestZeroLengthBufferDelivery()
        {
            var proc = CreateDataProcessor(false, "X");
            var res = proc.ProcessReceivedRawData(new byte[0], 0);
            Assert.IsNotNull(res);
            Assert.AreEqual(0, res.Count());

        }

        /// <summary>
        /// Test that delimiter present in header (delimiter in length header) does not split message.
        /// </summary>
        [TestMethod]
        public void TestDelimiterInHeaderDoesNotSplitMessage()
        {

            // create processor 
            var logp = new LoggerFactory();
            var cfg = new MessageDataProcessorConfig(5000, new byte[] { 3 }, true);
            var proc = new MessageDataProcessor(cfg, logp.CreateLogger<MessageDataProcessor>());

            // create buffer 
            // (03 - length, XXX  - message, 3 - terminator)
            var buffer = new byte[] {
                0, 3, 88, 88, 88, 3,
                0, 3, 99, 99, 99, 3,
            };

            // process result
            var ret = proc.ProcessReceivedRawData(buffer, buffer.Length);
            Assert.AreEqual(2, ret.Count());
            Assert.IsTrue(new byte[] { 88, 88, 88 }.SequenceEqual(ret.First()));
            Assert.IsTrue(new byte[] { 99, 99, 99 }.SequenceEqual(ret.Skip(1).First()));

        }

    }
}
