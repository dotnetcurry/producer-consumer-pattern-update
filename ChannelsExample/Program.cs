using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ChannelsExample
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var results = new List<(string,int,int,TimeSpan)>();
            const int channelCapacity = 512;
            const int itemsCount = 4_096;
            var counts = new[] { 1, 2, 4, 8, 16, 32, 64 };
            Func<Task> simulatedWorkload = WorkloadSimulator.IoBound;

            Debug.Assert(counts.All(c => itemsCount % c == 0));

            var tests = (from readerCount in counts
                         from writerCount in counts
                         select new PerformanceTest(channelCapacity, writerCount, readerCount, itemsCount)).ToList();
            
            // Adding a short warmup cycle
            tests = tests.Prepend(new PerformanceTest(channelCapacity, 1, 1, channelCapacity)).ToList();

            foreach (var test in tests)
            {
                Console.WriteLine($"TestCase - Readers: {test.ReaderCount} Writers: {test.WriterCount}");

                MemoryCleanup();
                var channelTime = await RunChannelProducerConsumerTestCase(test, simulatedWorkload);
                results.Add(("Channel", test.ReaderCount, test.WriterCount, channelTime));

                MemoryCleanup();
                var blockingCollectionTime = await RunBlockingCollectionTestCase(test, simulatedWorkload);
                results.Add(("BlockingCollection", test.ReaderCount, test.WriterCount, blockingCollectionTime));

                MemoryCleanup();
                var dataFlowTime = await RunDataFlowTestCase(test, simulatedWorkload);
                results.Add(("DataFlow", test.ReaderCount, test.WriterCount, dataFlowTime));
            }

            using (var sw = new StreamWriter("results.txt"))
            {
                await sw.WriteLineAsync("API,Readers,Writers,Elapsed");
                foreach (var result in results.Skip(1))
                {
                    await sw.WriteLineAsync($"{result.Item1},{result.Item2},{result.Item3},{result.Item4}");
                }
            }

            Console.WriteLine("Finished");
        }

        private static Task<TimeSpan> RunChannelProducerConsumerTestCase(PerformanceTest test, Func<Task> simulatedWorkload)
        => test.Run(ChannelProducerConsumerTestCase.CreateChannel,
                channel => ChannelProducerConsumerTestCase.Consume(channel, simulatedWorkload),
                ChannelProducerConsumerTestCase.Produce,
                ChannelProducerConsumerTestCase.CompleteChannel);

        private static Task<TimeSpan> RunBlockingCollectionTestCase(PerformanceTest test, Func<Task> simulatedWorkload)
            => test.Run(BlockingCollectionTestCase.CreateChannel,
                channel => BlockingCollectionTestCase.Consume(channel, simulatedWorkload),
                BlockingCollectionTestCase.Produce,
                BlockingCollectionTestCase.CompleteChannel);

        private static Task<TimeSpan> RunDataFlowTestCase(PerformanceTest test, Func<Task> simulatedWorkload)
            => test.Run(DataFlowTestCase.CreateChannel,
                channel => DataFlowTestCase.Consume(channel, simulatedWorkload),
                DataFlowTestCase.Produce,
                DataFlowTestCase.CompleteChannel);

        private static void MemoryCleanup()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
