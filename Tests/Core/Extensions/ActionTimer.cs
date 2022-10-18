using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Peckr.Tests.Core.Extensions
{
    public static class ActionTimer
    {
        public static async Task<Stopwatch> MeasureAsync(Func<Task> func)
        {
            var sw = Stopwatch.StartNew();
            await func.Invoke();
            sw.Stop();
            return sw;
        }
    }
}
