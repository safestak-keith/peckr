using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peckr.Metrics.UnitTests.Generators
{
    public class MetricGenerator
    {
        public const string DefaultSource = "prodfooapimetrics";
        public const string DefaultAppId = "fooapi";
        public const string DefaultType = "HTTP";
        public const string DefaultName = "BarApi";
        public const string DefaultTarget = "/users";
        public const string DefaultInstanceId = "FooAPI_0";
        public const int DefaultValue = 50;

        public static Metric Create(
            int seed = 0,
            string source = DefaultSource,
            string appId = DefaultAppId,
            DateTimeOffset? timestamp = null,
            string type = DefaultType,
            string name = DefaultName,
            string target = DefaultTarget,
            Func<int, double> valueProvider = null,
            string instanceId = DefaultInstanceId,
            string operationId = null)
        {
            return new Metric(
                source,
                appId,
                timestamp ?? new DateTimeOffset(2019, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(-1 * seed),
                type,
                name,
                target,
                valueProvider?.Invoke(seed) ?? DefaultValue,
                instanceId,
                operationId ?? $"op-{seed}");
        }

        public static Metric DefaultInstance = Create(0);

        public static Metric[] OfCount(int count, Func<int, double> valueProvider = null)
        {
            return count == 0
                ? Array.Empty<Metric>()
                : Enumerable.Range(0, count)
                    .Select(i => Create(i, valueProvider: valueProvider))
                    .ToArray();
        }

        public static Metric[] OfPercentage(int metricPercentage, Func<int, double> successValueProvider, Func<int, double> failValueProvider)
        {
            if (metricPercentage > 100 || metricPercentage < 1) throw new ArgumentOutOfRangeException(nameof(metricPercentage));

            return Enumerable.Range(0, metricPercentage)
                .Select(i => Create(i, valueProvider: successValueProvider))
                .Concat(
                    Enumerable.Range(metricPercentage, 100 - metricPercentage)
                        .Select(i => Create(i, valueProvider: failValueProvider))
                ).ToArray();
        }
    }
}