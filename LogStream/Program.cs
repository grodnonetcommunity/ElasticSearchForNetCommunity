using System;
using System.Threading;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Http.BatchFormatters;

namespace LogStream
{
    class Program
    {
        static void Main(string[] args)
        {
            var log = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Http("http://localhost:31311", batchFormatter: new ArrayBatchFormatter())
                .WriteTo.Console()
                .CreateLogger();
            var random = new Random();
            while (true)
            {
                var level = (LogEventLevel)random.Next(0, 6);
                var value = random.Next();
                log.Write(level, "Executing some code with value {@value}", value);
                Thread.Sleep(1000);
            }
        }
    }
}
