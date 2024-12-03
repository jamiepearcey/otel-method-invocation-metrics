using System.Linq.Expressions;
#if NET47
using System.Collections.Generic;
#endif

namespace MetricsLibrary.Options;

public class MetricsMethodOptions
{
    public string MetricName { get; set; } = "method_execution_time";
    public string? Description { get; set; }
    public Dictionary<string, object> DefaultTags { get; set; } = new Dictionary<string, object>();
}

public class MetricsInterceptorOptions<T> where T : class
{
    private readonly Dictionary<string, MetricsMethodOptions> _methodsToRecord = new();
    
    public string MeterName { get; set; } = "MetricsLibrary";
    public Dictionary<string, object> GlobalTags { get; set; } = new Dictionary<string, object>();
    
    public void AddMethodToRecord<TResult>(
        Expression<Func<T, TResult>> methodExpression,
        Action<MetricsMethodOptions>? configureOptions = null)
    {
        if (methodExpression.Body is MethodCallExpression methodCall)
        {
            var methodName = $"{typeof(T).Name}.{methodCall.Method.Name}";
            var options = new MetricsMethodOptions();
            configureOptions?.Invoke(options);
            _methodsToRecord[methodName] = options;
        }
    }
    
    public (bool ShouldRecord, MetricsMethodOptions Options) GetMethodOptions(string methodFullName)
    {
        if (_methodsToRecord.Count == 0)
        {
            return (true, new MetricsMethodOptions());
        }
        
        return _methodsToRecord.TryGetValue(methodFullName, out var options)
            ? (true, options)
            : (false, new MetricsMethodOptions());
    }
} 