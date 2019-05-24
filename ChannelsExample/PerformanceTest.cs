using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ChannelsExample
{
    public class PerformanceTest
    {
        private const int MessageSizeBytes = 1024;
        private readonly Random _random = new Random();
        private readonly int _channelCapacity;
        private readonly int _itemsCount;

        public int WriterCount { get; }
        public int ReaderCount { get; }

        public PerformanceTest(int channelCapacity, int writerCount, int readerCount, int itemsCount)
        {
            _channelCapacity = channelCapacity;
            WriterCount = writerCount;
            ReaderCount = readerCount;
            _itemsCount = itemsCount;
        }

        public async Task<TimeSpan> Run<TChannel>(Func<int, TChannel> createChannel, 
                                                  Func<TChannel, Task<int>> consume, 
                                                  Func<TChannel, byte[][], Task> produce, 
                                                  Action<TChannel> completeChannel)
        {
            var stopwatch = Stopwatch.StartNew();
            
            var channel = createChannel(_channelCapacity);

            var readerTasks = Enumerable.Range(1, ReaderCount)
                                        .Select(_ => Task.Run(async () => await consume(channel)))
                                        .ToArray();

            var writerTasks = Enumerable.Range(1, WriterCount)
                                        .Select(_ => GetRandomMessagesArray(_itemsCount / WriterCount))
                                        .Select(m => Task.Run(async () => await produce(channel, m)))
                                        .ToArray();

            await Task.WhenAll(writerTasks);

            completeChannel(channel);

            await Task.WhenAll(readerTasks);

            stopwatch.Stop();

            // Check that total published bytes are equal to total consumed bytes
            Debug.Assert(_itemsCount * MessageSizeBytes == readerTasks.Aggregate(0, (s, t) => s + t.Result));

            return TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
        }

        private byte[][] GetRandomMessagesArray(int size) => Enumerable.Range(1, size).Select(_ => GetRandomMessage()).ToArray();

        private byte[] GetRandomMessage()
        {
            var message = new byte[MessageSizeBytes];
            _random.NextBytes(message);
            return message;
        }
    }
}
