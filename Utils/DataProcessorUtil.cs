using System;
using System.Collections.Generic;
using System.Linq;

namespace Layer4Stack.Utils
{

    /// <summary>
    /// Data processor util
    /// </summary>
    internal static class DataProcessorUtil
    {

        /// <summary>
        /// Get length header 
        /// </summary>
        /// <param name="length"></param>
        /// <param name="headerSize"></param>
        /// <returns></returns>
        internal static byte[] LengthToHeader(long length, int headerMaxSize)
        {
            // divisor base 
            var divisorBase = (long) byte.MaxValue + 1;

            // get required size 
            int size = 0;
            for(int i=1; length/((long)Math.Pow(divisorBase, i)) > 0; i++)
            {
                size = i;
            }
            size += 1;

            // check size 
            if(headerMaxSize < size)
            {
                throw new ArgumentException("Header size is too small.");
            }

            // get header size 
            List<byte> header = new List<byte>();
            for(int i= size-1; i>=0; i--) {
                long value = length;
                if (i!=0)
                {
                    var divisor = (long)Math.Pow(divisorBase, i);
                    value = length / divisor;
                    length = length - value * divisor;
                } 
                header.Add((byte)value);
            }

            // return 
            if(header.Count() == headerMaxSize)
            {
                return header.ToArray();
            } else
            {
                var ret = Enumerable.Range(0, headerMaxSize - header.Count()).Select(c => (byte)0).ToList();
                ret.AddRange(header);
                return ret.ToArray();
            }
            
        }

        /// <summary>
        /// Gets lenght from header
        /// </summary>
        /// <param name="header"></param>
        /// <returns></returns>
        internal static long HeaderToLength(byte[] header)
        {
            
            // check size 
            int maxHeaderSize = Convert.ToString(long.MaxValue, 2).Length;
            if(header.Length > maxHeaderSize)
            {
                throw new ArgumentException("Header size is to big");
            }

            // caclulate size and return 
            var sizes = Enumerable.Range(0, header.Length).Select(i =>
                {
                    return header[i] * ((long)Math.Pow(256, header.Length-i-1));
                });
            return sizes.Sum();
        }

    }
}
