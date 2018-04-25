namespace Serilog.Sinks.Async.Tests.Support
{
    class DummyMonitor : IAsyncLogEventSinkMonitor
    {
        IAsyncLogEventSinkInspector inspector;
        public IAsyncLogEventSinkInspector Inspector => inspector;

        void IAsyncLogEventSinkMonitor.StartMonitoring(IAsyncLogEventSinkInspector inspector) =>
            this.inspector = inspector;

        void IAsyncLogEventSinkMonitor.StopMonitoring(IAsyncLogEventSinkInspector inspector) =>
            System.Threading.Interlocked.CompareExchange(ref this.inspector, null, inspector);
    }
}