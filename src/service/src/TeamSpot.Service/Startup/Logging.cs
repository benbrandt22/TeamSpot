using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using System.Diagnostics;

namespace TeamSpot.Service.Startup
{
    public static class Logging
    {
        public static void Configure(HostApplicationBuilder builder)
        {
            // setup Windows event logging
            LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(builder.Services);


            // Setup console logging if we're live debugging in Visual Studio
            if (Debugger.IsAttached)
            {
                builder.Logging.ClearProviders();
                // Visible in Visual Studio Output window
                builder.Logging.AddDebug();
                // Visible in console window if running interactively
                builder.Logging.AddConsole();
            }
        }

    }
}
