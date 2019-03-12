using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Layer4StackTest")]

namespace Layer4Stack.Utils
{

    /// <summary>
    /// Byte array extnsions 
    /// </summary>
    internal static class ByteArrayExtensions
    {

        /// <summary>
        /// Slice a partial sub-array from array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="start"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] Slice(this byte[] data, int start, int length)
        {

            // data not set or lenght invalid 
            if(data == null)
            {
                return new byte[0];
            }

            // lenght must be positive
            length = Math.Max(0, length);

            // set end and start correctly 
            int end = start + length;
            start = Math.Max(start, 0);
            end = Math.Min(end, data.Length);
            if(start > end)
            {
                return new byte[0];
            }

            // slice 
            var result = new byte[end-start];
            Buffer.BlockCopy(data, start, result, 0, result.Length);
            return result;
        }

        /// <summary>
        /// Slices multiple parts of an array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="buffer"></param>
        /// <param name="intervals"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        internal static byte[][] SliceMulti(this byte[] buffer, Tuple<int, int>[] intervals, int? maxLength = null)
        {
            // fix length
            var length = maxLength.HasValue ? Math.Min(buffer.Length, maxLength.Value) : buffer.Length;
            return intervals.Select(i => buffer.Slice(i.Item1, i.Item2)).ToArray();
        }

        /// <summary>
        /// Gets intervals from indexes values  
        /// </summary>
        /// <param name="values"></param>
        /// <param name="skipStart"></param>
        /// <param name="skipEnd"></param>
        /// <returns></returns>
        internal static Tuple<int, int>[] GetIntervals(this IEnumerable<int> values, int skipStart = 0, int skipEnd = 0)
        {
            var orderedValues = values.Distinct().OrderBy(v => v);
            var orderedValuesShift = orderedValues.Prepend(-1);
            return orderedValuesShift.Zip(orderedValues, (prev, curr) => new Tuple<int, int>(prev  + skipStart + 1, curr - skipEnd)).
                Where(i => i.Item1 <= i.Item2).ToArray();
        }

        /// <summary>
        /// Finds occurences of a needle inside the provided haystack 
        /// </summary>
        /// <param name="haystack"></param>
        /// <param name="needle"></param>
        /// <param name="haystackLength"></param>
        /// <param name="needleLength"></param>
        /// <param name="haystackLengthLimit"></param>
        /// <param name="minStepSize"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static int[] FindOccurrences(this byte[] haystack, byte[] needle, int? haystackLengthLimit = null, int minStepSize = 0, int offset = 0)
        {
            // nothing to do with invalid buffers
            if(needle?.Length == 0 || haystack?.Length ==0)
            {
                return new int[0];
            }

            int needleLength = needle.Length;
            minStepSize = Math.Max(minStepSize, needleLength);
            int haystackLength = Math.Min(haystack.Length, Math.Max(0,haystackLengthLimit ?? int.MaxValue));
            var poss = new List<int>();
            int? prev = null;
            for(int i=offset; i <= haystackLength - needleLength; i++)
            {
                bool found = haystack.Skip(i).Take(needleLength).SequenceEqual(needle.Take(needleLength));
                int ix = i + needleLength - 1;

                // add if not duplicate (e.g. terminator is XX, message is AaXXXooXXXX, positions are: 2,7,9) 
                // and step big enough
                if (found && (!prev.HasValue || (Math.Abs(ix - prev.Value)) >= minStepSize))
                {
                    poss.Add(ix);
                    prev = ix;
                }
            }

            return poss.ToArray();
        }

        /// <summary>
        /// Get first occurence
        /// </summary>
        /// <param name="haystack"></param>
        /// <param name="needle"></param>
        /// <param name="haystackLengthLimit"></param>
        /// <returns></returns>
        public static int? FindFirstOccurrence(this byte[] haystack, byte[] needle, int? haystackLengthLimit = null)
        {
            var occ = FindOccurrences(haystack, needle, haystackLengthLimit);
            return occ.Any() ? new int?(occ.First()) : null;
        }

        /// <summary>
        /// Replaces part of byte array with provided data
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="data"></param>
        /// <param name="dataStartPos"></param>
        /// <param name="bufferStartPos"></param>
        /// <param name="maxCount"></param>
        /// <returns></returns>
        internal static int ReplaceWith(this byte[] buffer, byte[] data, int dataStartPos, int bufferStartPos, int? maxCount = null)
        {

            // do nothing if positions are over data lenght
            if(dataStartPos >= data.Length || bufferStartPos >= buffer.Length)
            {
                return 0;
            }

            // fix positions 
            dataStartPos = Math.Max(0, dataStartPos);
            bufferStartPos = Math.Max(0,bufferStartPos);
             
            // set count that can be copies
            int count = Math.Min(data.Length - dataStartPos, buffer.Length - bufferStartPos);
            count = maxCount.HasValue ? Math.Min(Math.Max(0, maxCount.Value), count) : count;
                
            // copy and return
            count = Math.Min(count, Math.Max(0, data.Length - dataStartPos));
            Buffer.BlockCopy(data, dataStartPos, buffer, bufferStartPos, count);
            return count;
        }

        /// <summary>
        /// Append data to buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static byte[] Append(this byte[] buffer, byte[] data)
        {
            var result = new byte[buffer.Length + data.Length];
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            Buffer.BlockCopy(data, 0, result, buffer.Length, data.Length);
            return result;
        }

        /// <summary>
        /// Get bytes after needle
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        /// <param name="needle"></param>
        /// <param name="includeNeedle"></param>
        /// <param name="haystackLength"></param>
        /// <returns>Returns array data or empty if needle not found</returns>
        internal static byte[] GetAfter(this byte[] buffer, byte[] needle, bool includeNeedle = false, int? haystackLength = null)
        {

            // check if needle is valid
            if(needle?.Length == 0)
            {
                return new byte[0];
            }

            // fix haystack length
            int length = Math.Min(Math.Max(0, haystackLength ?? buffer.Length), buffer.Length);

            // get data 
            var poss = buffer.FindOccurrences(needle, length);
            if (poss.Any())
            {
                var start = includeNeedle ? poss.First() - needle.Length + 1 : poss.First() + 1;
                var count = length - start;
                if(count <=0)
                {
                    return new byte[0];
                }
                
                var ret = new byte[count];
                Buffer.BlockCopy(buffer, start, ret, 0, count);
                return ret;
            }
            return new byte[0];
        }

        /// <summary>
        /// Trim zero 
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        public static byte[] TrimZero(this byte[] arr)
        {
            // get left position
            int left = 0;
            for (left = 0; left < arr.Length && arr[left] == 0; left++) ;

            // get from right 
            int right;
            for (right = 0; right < arr.Length && arr[arr.Length-right-1] == 0; right++);

            // nothing to do
            if(left == 0 && right == 0)
            {
                return arr;
            }

            // trim and return 
            byte[] ret = new byte[arr.Length - left - right];
            Buffer.BlockCopy(arr, left, ret, 0, arr.Length - left - right);
            return ret;

        }

    }
}