using Layer4Stack.DataProcessors;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text;

namespace Layer4StackTest
{

    /// <summary>
    /// Simple message data processor test
    /// </summary>
    [TestClass]
    public class SimpleMessageDataProcessorTest
    {

        /// <summary>
        /// Create processor
        /// </summary>
        /// <returns></returns>
        private SimpleMessageDataProcessor CreateProcessor()
        {
            var lf = new LoggerFactory();
            var logger = lf.CreateLogger<SimpleMessageDataProcessor>();
            return SimpleMessageDataProcessor.CreateProcessor(logger);
        }

        /// <summary>
        /// Test delivery 
        /// </summary>
        [TestMethod]
        public void TestDelivery()
        {
            // create processor
            var proc = CreateProcessor();

            // buffers
            var testChar = (byte)'a';
            var buffer1 = new byte[]
            {
                9,
                0,
                3,
                testChar,
                testChar,
                testChar
            };
            testChar = (byte)'b';
            var buffer2 = new byte[]
            {
                9,
                5,
                2,
                0,
                5,
                testChar,
                testChar,
                testChar
            };
            var buffer3 = new byte[]
            {
                testChar,
                testChar,
                testChar,
                0
            };
            var buffer4 = new byte[]
            {
                2,
                testChar,
                testChar,
                0,
                1,
                testChar
            };

            // buffer 1 test 
            var msgs = proc.ProcessReceivedRawData(buffer1, buffer1.Length);
            Assert.AreEqual(1, msgs.Count());
            Assert.AreEqual("aaa", Encoding.ASCII.GetString(msgs.First()));

            // buffer 2
            msgs = proc.ProcessReceivedRawData(buffer2, buffer2.Length);
            Assert.AreEqual(0, msgs.Count());

            // buffer 3 
            msgs = proc.ProcessReceivedRawData(buffer3, buffer3.Length);
            Assert.AreEqual(1, msgs.Count());
            Assert.AreEqual("bbbbb", Encoding.ASCII.GetString(msgs.First()));

            // buffer 4
            msgs = proc.ProcessReceivedRawData(buffer4, buffer4.Length);
            Assert.AreEqual(2, msgs.Count());
            Assert.AreEqual("bb", Encoding.ASCII.GetString(msgs.First()));
            Assert.AreEqual("b", Encoding.ASCII.GetString(msgs.Skip(1).First()));

        }

        /// <summary>
        /// Test pack
        /// </summary>
        [TestMethod]
        public void TestPack()
        {
            var proc = CreateProcessor();
            var msg = new byte[] { 73, 73, 73 };
            var buffer = proc.FilterSendData(msg);
            Assert.IsTrue(new byte[] { 0, 3, 73, 73, 73 }.ToList().SequenceEqual(buffer));
        }

        /// <summary>
        /// Test with bigger header
        /// </summary>
        [TestMethod]
        public void TestBiggerHeader()
        {

            // setup
            var delimiter = new byte[] { 0 };
            int headerLength = 2;

            // message of 342 a
            var txt = new string(Enumerable.Range(0, 342).Select(i => 'a').ToArray());
            var msg = Encoding.ASCII.GetBytes(txt);

            // create processor 
            var proc = new SimpleMessageDataProcessor(new LoggerFactory().CreateLogger<SimpleMessageDataProcessor>(),
                500, delimiter, headerLength);

            // pack message 
            var buffer = proc.FilterSendData(msg);
            Assert.AreEqual(0, buffer[0]);
            Assert.AreEqual(1, buffer[1]);
            Assert.AreEqual(342 - 256, buffer[2]);
            var btxt = Encoding.ASCII.GetString(buffer.Skip(3).ToArray());
            Assert.AreEqual(txt, btxt);

            // process message 
            var ret = proc.ProcessReceivedRawData(buffer, buffer.Length);
            Assert.AreEqual(1, ret.Count());
            Assert.IsTrue(msg.SequenceEqual(ret.First()));

        }

        /// <summary>
        /// Test that processor is agnostic to usage of delimiter insided length header (eg. 0 delimiter with length header of 2 chars, one 0).
        /// </summary>
        [TestMethod]
        public void TestAgnosticToRepeatedDelimiter()
        {

            // setup
            var delimiter = new byte[] { 0 };
            int headerLength = 2;

            // create processor 
            var proc = new SimpleMessageDataProcessor(new LoggerFactory().CreateLogger<SimpleMessageDataProcessor>(),
                500, delimiter, headerLength);

            // test buffer
            var buffer = new byte[] { 0, 1, 0 }.ToList(); // delimiter with length header (256 + 0)
            buffer.AddRange(Enumerable.Range(0, 256).Select(i => (byte) 99));

            // tesr ret 
            var ret = proc.ProcessReceivedRawData(buffer.ToArray(), buffer.Count());
            Assert.AreEqual(1, ret.Count());
            Assert.AreEqual(256, ret.First().Length);

        }

        /// <summary>
        /// Test that processor is agnostic to bad data 
        /// </summary>
        [TestMethod]
        public void TestAgnosticToRepeatedDelimiterMultuple()
        {

            // setup
            var delimiter = new byte[] { 0 };
            int headerLength = 2;

            // create processor 
            var logger = new LoggerFactory().CreateLogger<SimpleMessageDataProcessor>();
            var proc = SimpleMessageDataProcessor.CreateProcessor(logger, 500, 2);

            // test buffer with errors
            var buffer = new byte[] { 0, 1, 0, 0, 0, 0,0,1,0}.ToList(); // delimiter with length header (256 + 0) and error delimietrs more 
            buffer.AddRange(Enumerable.Range(0, 34).Select(i => (byte)99));
            buffer.AddRange(new byte[] { 0, 1, 0 });
            buffer.AddRange(Enumerable.Range(0, 256).Select(i => (byte)99));

            // tesr ret 
            var ret = proc.ProcessReceivedRawData(buffer.ToArray(), buffer.Count());
            Assert.AreEqual(256, ret.Last().Length);

        }

    }
}
