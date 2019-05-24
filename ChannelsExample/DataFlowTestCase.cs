using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ChannelsExample
{
    public static class DataFlowTestCase 
    {
        public static BufferBlock<byte[]> CreateChannel(int capacity) => new BufferBlock<byte[]>(new DataflowBlockOptions { BoundedCapacity = capacity });

        public static void CompleteChannel(BufferBlock<byte[]> channel) => channel.Complete();

        public static async Task<int> Consume(BufferBlock<byte[]> channel, Func<Task> simulateWorkload)
        {
            var receivedBytes = 0;

            while (await channel.OutputAvailableAsync())
            {
                while (channel.TryReceive(out var message))
                {
                    receivedBytes += message.Length;
                    await simulateWorkload();
                }
            }

            return receivedBytes;
        }

        public static async Task Produce(BufferBlock<byte[]> channel, byte[][] messages)
        {
            foreach (var message in messages)
            {
                var sentSuccessfully = await channel.SendAsync(message);
                Debug.Assert(sentSuccessfully);
            }
        }
    }
}
