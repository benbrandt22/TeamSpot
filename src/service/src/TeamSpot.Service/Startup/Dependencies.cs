using TeamSpot.Service.Hid;
using TeamSpot.Service.Teams;

namespace TeamSpot.Service.Startup
{
    public static class Dependencies
    {
        public static void Configure(IServiceCollection services)
        {
            services.AddSingleton<HidConnectionHolder>();
            services.AddSingleton<HidMessageBus>(); // single HID message bus to manage USB comms and be shared between services

            services.AddSingleton<SecureSettingsService<TeamSpotSettings>>();

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
