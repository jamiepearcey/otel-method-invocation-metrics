using Castle.DynamicProxy;
using OpenTelemetry.Metrics;
using System.Diagnostics;
using MetricsLibrary.Options;

namespace MetricsLibrary.Interceptors;

public class MetricsInterceptor<T> : IInterceptor where T : class
{
    private readonly Meter _meter;
    private readonly Dictionary<string, Histogram<double>> _histograms;
    private readonly MetricsInterceptorOptions<T> _options;

    public MetricsInterceptor(MetricsInterceptorOptions<T> options)
    {
        _options = options;
        _meter = new Meter(options.MeterName);
        _histograms = new Dictionary<string, Histogram<double>>();
    }

    private Histogram<double> GetOrCreateHistogram(MetricsMethodOptions methodOptions)
    {
        if (!_histograms.TryGetValue(methodOptions.MetricName, out var histogram))
        {
            histogram = _meter.CreateHistogram<double>(
                name: methodOptions.MetricName,
                unit: "ms",
                description: methodOptions.Description ?? "Histogram of method execution times");
            _histograms[methodOptions.MetricName] = histogram;
        }
        return histogram;
    }

    public void Intercept(IInvocation invocation)
    {
        var methodName = $"{invocation.Method.DeclaringType?.Name}.{invocation.Method.Name}";
        var (shouldRecord, methodOptions) = _options.GetMethodOptions(methodName);
        
        if (!shouldRecord)
        {
            invocation.Proceed();
            return;
        }

        var histogram = GetOrCreateHistogram(methodOptions);
        var tags = new List<KeyValuePair<string, object>>
        {
            new("method", methodName)
        };

        // Add global tags
        foreach (var tag in _options.GlobalTags)
        {
            tags.Add(new(tag.Key, tag.Value));
        }

        // Add method-specific tags
        foreach (var tag in methodOptions.DefaultTags)
        {
            tags.Add(new(tag.Key, tag.Value));
        }

#if NET47
        var startTime = Stopwatch.GetTimestamp();
#else
        var stopwatch = Stopwatch.StartNew();
#endif
        
        try
        {
            invocation.Proceed();
        }
        finally
        {
#if NET47
            var endTime = Stopwatch.GetTimestamp();
            var elapsedMs = (endTime - startTime) * 1000.0 / Stopwatch.Frequency;
            histogram.Record(elapsedMs, tags.ToArray());
#else
            stopwatch.Stop();
            histogram.Record(stopwatch.ElapsedMilliseconds, tags.ToArray());
#endif
        }
    }
} 