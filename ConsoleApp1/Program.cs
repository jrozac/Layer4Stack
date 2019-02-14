using Layer4Stack.DataProcessors;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApp1
{
    class Program
    {

        /// <summary>
        /// Create test messages 
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        private static string[] CreateTestMessages(int count)
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
        /// <returns></returns>
        private static DataProcessor CreteDataProcessor(bool useHeader = false, string terminator = "XXXXX")
        {
            var logp = new LoggerFactory();
            var cfg = new DataProcessorConfig(5000, Encoding.ASCII.GetBytes(terminator), useHeader);
            return new DataProcessor(cfg, logp.CreateLogger<DataProcessor>());
        }


        static void TestDataPackWithHeaderAndTerminator()
        {

            // data processor
            var proc = CreteDataProcessor(true);

            // create some messages to be sent 
            var msgs = CreateTestMessages(100);

            // create packets
            var packets = msgs.Select(m => proc.FilterSendData(Encoding.ASCII.GetBytes(m))).ToArray();

            Enumerable.Range(1, msgs.Length).ToList().ForEach(i => {

                // sent mesage and recieved packet 
                var msg = msgs.Take(i).Last();
                var pck = packets.Take(i).Last();

                // check length
                var len = pck[0] * 256 + pck[1];
                // Assert.AreEqual(msg.Length, len);

                // check terminator 
                var terminator = pck.TakeLast(proc.Config.Terminator.Length);

                // check message  
                var data = pck.Take(pck.Length - proc.Config.Terminator.Length).
                    TakeLast(pck.Length - proc.Config.Terminator.Length - 2).ToArray();
                var rcv = Encoding.ASCII.GetString(data);
                // Assert.AreEqual(msg, rcv);

            });


        }


        static void TestDataRawProcessingMultiMessage()
        {
            // data processor
            var proc = CreteDataProcessor(true);

            // create some messages to be sent 
            var msgs = CreateTestMessages(3);

            // create packets
            var packets = msgs.Select(m => proc.FilterSendData(Encoding.ASCII.GetBytes(m))).ToList();

            // add partial packet at the end
            var lastPacket = proc.FilterSendData(Encoding.ASCII.GetBytes(CreateTestMessages(1).First()));
            packets.Add(lastPacket.Take(3).ToArray());

            // glue packets to single packet 
            var packet = new List<byte>();
            packets.ForEach(p => packet.AddRange(p));
            var recieved = proc.ProcessReceivedRawData(packet.ToArray(), packet.Count());

            // todo: additional checks 


        }
        

        /// <summary>
        /// Test basic processing of messages
        /// </summary>
        static void TestDataRawDataProcessingWithTerminatorAndHeader()
        {

            // data processor
            var proc = CreteDataProcessor(true);

            // create some messages to be sent 
            var msgs = CreateTestMessages(100);

            // create packets
            var packets = msgs.Select(m => proc.FilterSendData(Encoding.ASCII.GetBytes(m))).ToArray();

            // send data (packets)
            var recieved = new List<string>();
            packets.ToList().ForEach(p => {
                var response = proc.ProcessReceivedRawData(p, p.Length);
                if(response?.Any() == true)
                {
                    recieved.AddRange(response.Select(b => Encoding.ASCII.GetString(b)));
                }
            });

            bool ok = false;
            if(recieved.Count() == msgs.Count())
            {
                ok = true; 
            }

            var matched = !recieved.Except(msgs).Any();
            if(matched)
            {
                ok = true;
            }




        }


        static void Main(string[] args)
        {


            TestDataRawProcessingMultiMessage();
     

            Console.ReadLine();
        }
    }
}
