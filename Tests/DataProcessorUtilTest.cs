using Layer4Stack.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Layer4StackTest
{

    /// <summary>
    /// Data processor util test 
    /// </summary>
    [TestClass]
    public  class DataProcessorUtilTest
    {

        /// <summary>
        /// Test header size is calculated correctly 
        /// </summary>
        [TestMethod]
        public void TestHeaderSizeFromLength()
        {
            long size = 224 + (2 * 256) + (3 * 256 * 256) + (4 * 256 * 256 * 256);
            byte[] headerExpected = new byte[] { 4,3,2,224 };

            var heeader = DataProcessorUtil.LengthToHeader(size, 4);
            Assert.IsTrue(headerExpected.SequenceEqual(heeader));
        
        }

        /// <summary>
        /// Test header size from length is padded 
        /// </summary>
        [TestMethod]
        public void TestHeaderSizeFromLengthPad()
        {

            long size = 224 + (2 * 256) + (3 * 256 * 256) + (4 * 256 * 256 * 256);
            byte[] headerExpected = new byte[] { 0,0,4, 3, 2, 224 };

            var heeader = DataProcessorUtil.LengthToHeader(size, 6);
            Assert.IsTrue(headerExpected.SequenceEqual(heeader));

        }

        /// <summary>
        /// Test header size edge 
        /// </summary>
        [TestMethod]
        public void TestHeaderSizeFromLengthEdge()
        {
            long size = 255;
            byte[] headerExpected = new byte[] { 255 };

            var heeader = DataProcessorUtil.LengthToHeader(size, 1);
            Assert.IsTrue(headerExpected.SequenceEqual(heeader));
        }

        /// <summary>
        /// Test header size zero length
        /// </summary>
        [TestMethod]
        public void TestHeaderSizeFromLengthZero()
        {
            long size = 0;
            byte[] headerExpected = new byte[] { 0 };

            var heeader = DataProcessorUtil.LengthToHeader(size, 1);
            Assert.IsTrue(headerExpected.SequenceEqual(heeader));
        }

        /// <summary>
        /// Test header size too small
        /// </summary>
        [TestMethod]
        public void TestHeaderSizeTooSmall()
        {
            long length = 256 * 256;
            Assert.ThrowsException<ArgumentException>(() => DataProcessorUtil.LengthToHeader(length, 1));
        }

        /// <summary>
        /// Test header size is 1 minumum
        /// </summary>
        [TestMethod]
        public void TestHeaderSizeZeroRequiresOneByteForLength()
        {
            long length = 0;
            Assert.ThrowsException<ArgumentException>(() => DataProcessorUtil.LengthToHeader(length, 0));
        }

        /// <summary>
        /// Test header to length
        /// </summary>
        [TestMethod]
        public void TestHeaderToLength()
        {
            var header = new byte[] { 1, 32, 3 };
            long expectedLength = 256 * 256 * 1 + 32 * 256 + 3;
            var length = DataProcessorUtil.HeaderToLength(header);
            Assert.AreEqual(expectedLength, length);
        }

        /// <summary>
        /// Test header to length throws exception if header to big
        /// </summary>
        [TestMethod]
        public void TestHeadertoLengthTooBigHeader()
        {
            var header = Enumerable.Range(0, 2000).Select(i => (byte)1).ToArray();
            Assert.ThrowsException<ArgumentException>(() => DataProcessorUtil.HeaderToLength(header));
        }

    }
}
