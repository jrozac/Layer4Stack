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
            return SimpleMessageDataProcessor.CreateHsmProcessor(logger);
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

    }
}
