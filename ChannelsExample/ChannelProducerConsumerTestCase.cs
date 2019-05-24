using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ChannelsExample
{
    public static class ChannelProducerConsumerTestCase
    {
        public static Channel<byte[]> CreateChannel(int capacity) => Channel.CreateBounded<byte[]>(capacity);

        public static void CompleteChannel(Channel<byte[]> channel) => channel.Writer.Complete();

        public static async Task<int> Consume(Channel<byte[]> channel, Func<Task> simulateWorkload)
        {
            var receivedBytes = 0;

            while (await channel.Reader.WaitToReadAsync())
            {
                while (channel.Reader.TryRead(out var message))
                {
                    receivedBytes += message.Length;
                    await simulateWorkload();
                }
            }

            return receivedBytes;
        }

        public static async Task Produce(Channel<byte[]> channel, byte[][] messages)
        {
            foreach (var message in messages)
            {
                await channel.Writer.WriteAsync(message);
            }
        }
    }
}
