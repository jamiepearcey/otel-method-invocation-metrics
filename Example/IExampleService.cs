namespace MetricsLibrary.Example;

public interface IExampleService
{
    Task DoSomethingAsync();
    Task DoSomethingElseAsync();
    Task UnmonitoredMethodAsync();
} 