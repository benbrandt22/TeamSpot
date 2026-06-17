using TeamSpot.Service.Hid;
using TeamSpot.Service.Teams;

namespace TeamSpot.Service.Startup
{
    public static class Dependencies
    {
        public static void Configure(HostApplicationBuilder builder)
        {
            var services = builder.Services;

            services.AddSingleton<HidConnectionHolder>();
            services.AddSingleton<HidMessageBus>(); // single HID message bus to manage USB comms and be shared between services

            services.Configure<StatusColorsSettings>(builder.Configuration.GetSection("StatusColors"));

            services.AddSingleton<SecureSettingsService<TeamsApiSettings>>();

            services.AddSingleton<TeamsMessageBus>();
            services.AddSingleton<ITeamsStateWriter>(sp => sp.GetRequiredService<TeamsMessageBus>());
            services.AddSingleton<ITeamsStateReader>(sp => sp.GetRequiredService<TeamsMessageBus>());
            services.AddSingleton<ITeamsCommandWriter>(sp => sp.GetRequiredService<TeamsMessageBus>());
            services.AddSingleton<ITeamsCommandReader>(sp => sp.GetRequiredService<TeamsMessageBus>());

            services.AddHostedService<HidService>();
            services.AddHostedService<OrchestratorService>();
            services.AddHostedService<TeamsInterfaceService>();

        }
    }
}
