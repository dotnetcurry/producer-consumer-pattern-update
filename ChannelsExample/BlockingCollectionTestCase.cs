using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ChannelsExample
{
    public static class BlockingCollectionTestCase
    {
        public static BlockingCollection<byte[]> CreateChannel(int capacity) => new BlockingCollection<byte[]>(capacity);

        public static void CompleteChannel(BlockingCollection<byte[]> channel) => channel.CompleteAdding();

        public static async Task<int> Consume(BlockingCollection<byte[]> channel, Func<Task> simulateWorkload)
        {
            var receivedBytes = 0;

            foreach (var message in channel.GetConsumingEnumerable())
            {
                receivedBytes += message.Length;
                await simulateWorkload();
            }

            return receivedBytes;
        }

        public static Task Produce(BlockingCollection<byte[]> channel, byte[][] messages)
        {
            foreach (var message in messages)
            {
                channel.Add(message);
            }

            return Task.CompletedTask;
        }
    }
}
