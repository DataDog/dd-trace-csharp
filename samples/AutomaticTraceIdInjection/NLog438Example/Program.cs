using Datadog.Trace;
using NLog;

namespace NLog438Example
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            MappedDiagnosticsContext.Set("order-number", 1024);
            Logger.Info("Message before a trace.");

            using (var scope = Tracer.Instance.StartActive("NLog45Example - Main()"))
            {
                Logger.Info("Message during a trace.");
            }

            Logger.Info("Message after a trace.");
            MappedDiagnosticsContext.Remove("order-number");
        }
    }
}
