using Layer4Stack.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Layer4StackTest
{

    [TestClass]
    public class ByteArrayExtensionsTest
    {

        /// <summary>
        /// Test array slice 
        /// </summary>
        [TestMethod]
        public void TestSlice()
        {

            // test buffer
            string msg = "abcdefghijklmn";
            var buffer = Encoding.ASCII.GetBytes(msg);

            // valid slice returns valid data
            var slice = Encoding.ASCII.GetString(buffer.Slice(2, 5));
            Assert.AreEqual(msg.Substring(2, 5), slice);

            // slice with too big length returns existing part
            slice = Encoding.ASCII.GetString(buffer.Slice(2, 500));
            Assert.AreEqual(msg.Substring(2), slice);

            // slice with zero length returns empty array
            slice = Encoding.ASCII.GetString(buffer.Slice(5, 0));
            Assert.AreEqual("", slice);

            // slice non existing part return empty array
            slice = Encoding.ASCII.GetString(buffer.Slice(500, 5000));
            Assert.AreEqual("", slice);

            // slice empty data returns empty array
            slice = Encoding.ASCII.GetString((new byte[0]).Slice(20, 20));
            Assert.AreEqual("", slice);

            // negative arguments retruns empty array
            slice = Encoding.ASCII.GetString(buffer.Slice(-2, -3));
            Assert.AreEqual("", slice);

            // negativ start but legth enough to fit returns partial 
            slice = Encoding.ASCII.GetString(buffer.Slice(-2, 5));
            Assert.AreEqual("abc", slice);

            // border case begin returns first char
            slice = Encoding.ASCII.GetString(buffer.Slice(-500, 501));
            Assert.AreEqual("a", slice);

            // border case end returns last char
            slice = Encoding.ASCII.GetString(buffer.Slice(msg.Length - 1, 1));
            Assert.AreEqual("n", slice);

            // border case end over last char returns emtpy
            slice = Encoding.ASCII.GetString(buffer.Slice(msg.Length, 1));
            Assert.AreEqual("", slice);

        }

        /// <summary>
        /// Tests multiple slice
        /// </summary>
        [TestMethod]
        public void TestMultiSlice()
        {
            // test buffer
            string msg = "abcdefghijklmn";
            var buffer = Encoding.ASCII.GetBytes(msg);

            // slice intervals 
            var intervals = new Dictionary<Tuple<int, int>, string>() {
                { new Tuple<int,int>(0,2), "ab" },
                { new Tuple<int,int>(4,3), "efg" },
                { new Tuple<int,int>(200,1), "" },
                { new Tuple<int,int>(-1,3), "ab" },
                { new Tuple<int,int>(13,1), "n" },
                { new Tuple<int,int>(13,3), "n" },
                { new Tuple<int,int>(14323,3), "" },
            };

            // do slice
            var slices = buffer.SliceMulti(intervals.Keys.ToArray());

            // check if sliced correctly 
            Assert.AreEqual(intervals.Count, slices.Count());
            Enumerable.Range(0, intervals.Count).ToList().ForEach(i =>
            {
                var expected = intervals.Values.Skip(i).First();
                var actual = Encoding.ASCII.GetString(slices[i]);
                Assert.AreEqual(expected, actual);
            });
        }

        /// <summary>
        /// Test get intervals from indexes
        /// </summary>
        [TestMethod]
        public void TestGetIntervals()
        {

            // test dataset 
            var dataset = new Dictionary<int, Tuple<int, int>>() {
                { 12, new Tuple<int,int>(7,12) },
                { 19, new Tuple<int,int>(13,19) },
                { 2, new Tuple<int,int>(0,2) },
                { 6, new Tuple<int,int>(3,6) },
                { 27, new Tuple<int,int>(20,27) }
            };

            // test that intervals are correctly provided 
            var intervals = dataset.Keys.ToArray().GetIntervals();
            Enumerable.Range(0, intervals.Length).ToList().ForEach(i =>
            {
                var ix = intervals[i].Item2;
                var expected = dataset[ix];
                var actual = intervals[i];
                Assert.AreEqual(expected, actual);
            });

            // test that duplicates are removed 
            var indexes = new int[] { 5, 5, 10 };
            intervals = indexes.GetIntervals();
            Assert.AreEqual(2, intervals.Length);
            Assert.IsTrue(intervals.Any(i => i.Item1 == 0 && i.Item2 == 5));
            Assert.IsTrue(intervals.Any(i => i.Item1 == 6 && i.Item2 == 10));

            // test data skip
            int skipStart = 2;
            int skipEnd = 3;
            var expectedList = dataset.Select(p => new Tuple<int, int>(p.Value.Item1 + skipStart, p.Value.Item2 - skipEnd)).
                Where(p => p.Item2 >= p.Item1).OrderBy(p => p.Item1).ToList();
            intervals = dataset.Keys.ToArray().GetIntervals(skipStart, skipEnd);
            Enumerable.Range(0, intervals.Length).ToList().ForEach(i =>
            {
                var expected = expectedList[i];
                var actual = intervals[i];
                Assert.AreEqual(expected, actual);
            });

            // test zero returns zero
            indexes = new int[] { 0 };
            intervals = indexes.GetIntervals();
            Assert.AreEqual(1, intervals.Length);
            Assert.AreEqual(0, intervals.First().Item1);
            Assert.AreEqual(0, intervals.First().Item2);

        }


        [TestMethod]
        public void TestFindOccurencies()
        {
            // set test message 
            string delimiter = "XXX";
            string msg = "fsdfdsfdsfsdXXXfsdfsdfsdfsXXXfsddfsdfsdfdsfsdXXXXXfsdfdsfsdfXXXXXXfdsfsd";
            var buffer = Encoding.ASCII.GetBytes(msg);
            var terminator = Encoding.ASCII.GetBytes(delimiter);

            // run test 
            var expected = new int[] { 14, 28, 47, 62, 65 };
            var actual = buffer.FindOccurrences(terminator);
            Assert.IsTrue(expected.SequenceEqual(actual));

            // run test with limited length
            expected = new int[] { 14, 28, 47 };
            actual = buffer.FindOccurrences(terminator, 49);
            Assert.IsTrue(expected.SequenceEqual(actual));

            // test empty buffer 
            actual = (new byte[0]).FindOccurrences(terminator);
            Assert.AreEqual(0, actual.Length);

            // test emty terminator
            actual = buffer.FindOccurrences(new byte[0]);
            Assert.AreEqual(0, actual.Length);

            // test find the same 
            actual = new byte[] { 0,1,2 }.FindOccurrences(new byte[] { 0,1,2 });
            Assert.AreEqual(2, actual.First());

            // test that custom lenght is correctly handled on edge (one char less)
            msg = "abcdefghiXXX"; // lenght 12
            buffer = Encoding.ASCII.GetBytes(msg);
            // terminator = Encoding.ASCII.GetBytes("xxxxx");
            actual = buffer.FindOccurrences(terminator, 11); // take 1 char less - no terminator matching 
            Assert.AreEqual(0, actual.Length);



        }

        /// <summary>
        /// Test buffer replace (copy from)
        /// </summary>
        [TestMethod]
        public void ReplaceWithTest()
        {
            // set test message 
            var msg = string.Join('a', Enumerable.Range(0, 1000).Select(p => p.ToString()));
            var buffer = Encoding.ASCII.GetBytes(msg);
            int maxLen = buffer.Length;

            // copy buffer (from 500/20 to 0/20)
            int len = 20;
            var data = msg.Skip(500).Take(len);
            int count = buffer.ReplaceWith(buffer, 500, 0, len);
            string msgAfter = Encoding.ASCII.GetString(buffer);
            Assert.AreEqual(20, count);
            Assert.AreEqual(msg.Substring(20), msgAfter.Substring(20));
            Assert.AreEqual(msg.Substring(500, 20), msgAfter.Substring(0, 20));

            // test for overlaping buffer
            msg = "abcdefghij";
            buffer = Encoding.ASCII.GetBytes(msg);
            maxLen = buffer.Length;
            count = buffer.ReplaceWith(buffer, 3, 1);
            Assert.AreEqual(7, count);
            msgAfter = Encoding.ASCII.GetString(buffer);
            Assert.AreEqual("adefghijij", msgAfter);

            // test for overlap position data
            buffer = Encoding.ASCII.GetBytes(msg);
            maxLen = buffer.Length;
            count = buffer.ReplaceWith(buffer, 10, 0, 600);
            Assert.AreEqual(0, count);
            msgAfter = Encoding.ASCII.GetString(buffer);
            Assert.AreEqual(msg, msgAfter);

            // test for last char only 
            buffer = Encoding.ASCII.GetBytes(msg);
            maxLen = buffer.Length;
            count = buffer.ReplaceWith(buffer, 9, 0, 600);
            msgAfter = Encoding.ASCII.GetString(buffer);
            Assert.AreEqual(1, count);
            Assert.AreEqual("jbcdefghij", msgAfter);

            // test for lenght 0 should not change anything 
            buffer = Encoding.ASCII.GetBytes(msg);
            maxLen = buffer.Length;
            count = buffer.ReplaceWith(buffer, 3, 0, 0);
            msgAfter = Encoding.ASCII.GetString(buffer);
            Assert.AreEqual(0, count);
            Assert.AreEqual(msg, msgAfter);

            // test negative position is treated as 0
            buffer = Encoding.ASCII.GetBytes(msg);
            maxLen = buffer.Length;
            count = buffer.ReplaceWith(buffer, -5, 5);
            msgAfter = Encoding.ASCII.GetString(buffer);
            Assert.AreEqual(5, count);
            Assert.AreEqual("abcdeabcde", msgAfter);

            // test emty data does not change anything
            buffer = Encoding.ASCII.GetBytes(msg);
            maxLen = buffer.Length;
            count = buffer.ReplaceWith(new byte[0], 0, 0, 0);
            msgAfter = Encoding.ASCII.GetString(buffer);
            Assert.AreEqual(0, count);
            Assert.AreEqual(msg, msgAfter);

            // test empy buffer remains empty buffer
            buffer = Encoding.ASCII.GetBytes(msg);
            count = new byte[0].ReplaceWith(buffer, 0, 0, 0);
            msgAfter = Encoding.ASCII.GetString(buffer);
            Assert.AreEqual(0, count);
            Assert.AreEqual(msg, msgAfter);

        }

        /// <summary>
        /// Test append 
        /// </summary>
        [TestMethod]
        public void TestAppend()
        {

            // regular ste 
            var msg = "abcdefghij";
            var append = "123";
            var buffer = Encoding.ASCII.GetBytes(msg);
            var buggerAppend = Encoding.ASCII.GetBytes(append);
            var result = buffer.Append(buggerAppend);
            var resultMsg = Encoding.ASCII.GetString(result);
            Assert.AreEqual("abcdefghij123", resultMsg);

            // test emtpy append empty
            Assert.AreEqual(0, new byte[0].Append(new byte[0]).Length);

        }

        /// <summary>
        /// Test get after method
        /// </summary>
        [TestMethod]
        public void TestGetAfter()
        {

            // test data 
            var terminator = "xx";
            var msg = "abcdefghijxxxxjso";
            var buffer = Encoding.ASCII.GetBytes(msg);
            var needle = Encoding.ASCII.GetBytes(terminator);

            // test get witout needle
            var res = buffer.GetAfter(needle, false, 15);
            var ress = Encoding.ASCII.GetString(res);
            Assert.AreEqual("xxj", ress);

            // test get with needle
            res = buffer.GetAfter(needle, true, 15);
            ress = Encoding.ASCII.GetString(res);
            Assert.AreEqual("xxxxj", ress);

            // test with empty needle 
            res = buffer.GetAfter(new byte[0]);
            ress = Encoding.ASCII.GetString(res);
            Assert.AreEqual("", ress);

            // test with empty buffer
            res = new byte[0].GetAfter(needle);
            ress = Encoding.ASCII.GetString(res);
            Assert.AreEqual("", ress);

            // test with lenght overdraw
            res = buffer.GetAfter(needle, true, 4324);
            ress = Encoding.ASCII.GetString(res);
            Assert.AreEqual("xxxxjso", ress);

            // test get with needle at the end 
            terminator = "jso";
            needle = Encoding.ASCII.GetBytes(terminator);
            res = buffer.GetAfter(needle, false);
            ress = Encoding.ASCII.GetString(res);
            Assert.AreEqual("", ress);
        }
    }
}
