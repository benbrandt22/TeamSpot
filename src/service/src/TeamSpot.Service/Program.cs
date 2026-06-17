using TeamSpot.Service.Startup;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "TeamSpot.Service";
});

Logging.Configure(builder);
Dependencies.Configure(builder);

IHost host = builder.Build();
host.Run();
