using System.Reflection;
using System.Windows.Forms;
using TeamSpot.Service.Presentation;
using TeamSpot.Service.Startup;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        using var mutex = new Mutex(
            initiallyOwned: true,
            name: $@"Global\{Assembly.GetExecutingAssembly().GetName().Name}",
            createdNew: out bool isFirstInstance);
        // exit the application if another instance is already running
        if (!isFirstInstance) { return; }

        // Begin normal application startup

        ApplicationConfiguration.Initialize(); // initialize WinForms app to support system tray interface
        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        
        Logging.Configure(builder);
        Dependencies.Configure(builder);

        IHost host = builder.Build();

        // Start all registered BackgroundServices
        host.StartAsync().GetAwaiter().GetResult();

        try
        {
            // Run the tray application's message loop
            var tray = host.Services.GetRequiredService<TrayApplicationContext>();
            Application.Run(tray);
        }
        finally
        {
            // Gracefully stop all BackgroundServices
            host.StopAsync().GetAwaiter().GetResult();
        }
    }
}