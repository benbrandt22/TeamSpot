using System.Diagnostics;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace TeamSpot.Service.Teams
{
    public class TeamsApplicationUiInterfaceService : BackgroundService
    {
        private readonly ILogger<TeamsApplicationUiInterfaceService> _logger;
        private readonly ITeamsStateWriter _stateWriter;
        private readonly ITeamsCommandReader _commandReader;

        private Task? _receiveLoop;
        private Task? _commandLoop;

        public TeamsApplicationUiInterfaceService(ILogger<TeamsApplicationUiInterfaceService> logger,
            ITeamsStateWriter stateWriter, ITeamsCommandReader commandReader)
        {
            _logger = logger;
            _stateWriter = stateWriter;
            _commandReader = commandReader;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];

            try
            {
                
            }
            catch (OperationCanceledException) { /* normal shutdown */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teams receive loop faulted.");
                await PublishStateAsync(new TeamsState { IsTeamsRunning = true, IsConnected = false }, CancellationToken.None);
            }
        }

        private async Task CommandLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var command in _commandReader.CommandReader.ReadAllAsync(cancellationToken))
                {
                    try
                    {
                        //await SendCommandAsync(command, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send command {command}.", command);
                    }
                }
            }
            catch (OperationCanceledException) { /* normal shutdown */ }
        }

        private async Task PublishStateAsync(TeamsState state, CancellationToken cancellationToken)
        {
            await _stateWriter.StateWriter.WriteAsync(state, cancellationToken);
        }

    }
}
