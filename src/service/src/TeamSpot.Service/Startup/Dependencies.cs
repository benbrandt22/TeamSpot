using TeamSpot.Service.Hid;

namespace TeamSpot.Service.Startup
{
    public static class Dependencies
    {
        public static void Configure(IServiceCollection services)
        {
            services.AddSingleton<HidConnectionHolder>();
            services.AddSingleton<HidMessageBus>(); // single HID message bus to manage USB comms and be shared between services

            services.AddHostedService<HidService>();
            services.AddHostedService<OrchestratorService>();

        }
    }
}
