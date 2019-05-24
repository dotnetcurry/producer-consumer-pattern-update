using System;
using System.Threading.Tasks;

namespace ChannelsExample
{
    public static class WorkloadSimulator
    {
        private const int DurationInMs = 10;

        public static Task None() => Task.CompletedTask;

        public static Task IoBound() => Task.Delay(DurationInMs);

        public static Task CpuBound()
        {
            var enterTime = DateTime.Now;
            while (DateTime.Now.Subtract(enterTime).TotalMilliseconds < DurationInMs)
            {
            }
            
            return None();
        }
    }
}