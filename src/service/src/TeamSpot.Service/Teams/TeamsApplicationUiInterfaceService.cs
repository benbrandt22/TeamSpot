using FlaUI.Core;
using FlaUI.UIA3;
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
        private Task? _commandLoop;
        private readonly UIA3Automation _automation;
        private Application? _teamsApp;
        private TeamsUiWindow? _teamsMeetingWindow;

        public TeamsApplicationUiInterfaceService(ILogger<TeamsApplicationUiInterfaceService> logger,
            ITeamsStateWriter stateWriter, ITeamsCommandReader commandReader)
        {
            _logger = logger;
            _stateWriter = stateWriter;
            _commandReader = commandReader;
            _automation = new UIA3Automation();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _commandLoop = Task.Run(() => CommandLoopAsync(stoppingToken), stoppingToken);

            await Task.Yield();

            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var currentState = await ReadCurrentTeamsState();
                await PublishStateAsync(currentState, stoppingToken);
            }

            await PublishStateAsync(TeamsState.Offline, stoppingToken);
            _stateWriter.StateWriter.TryComplete();
        }

        private async Task<TeamsState> ReadCurrentTeamsState()
        {
            var currentState = new TeamsState()
            {
                IsTeamsRunning = false,
                IsConnected = false,
                IsInMeeting = false,
                IsMicrophoneLive = false
            };

            if (_teamsApp != null && !_teamsApp.HasExited) {
                // all good, no need to re-attach to application
            }
            else
            {
                try
                {
                    _teamsApp = FlaUI.Core.Application.Attach("ms-teams.exe");
                }
                catch (Exception)
                {
                    _teamsApp = null;
                    _teamsMeetingWindow = null;
                    // app's not running, don't bother polling so often
                    await Task.Delay(1000);
                }

            }

            currentState.IsTeamsRunning = _teamsApp != null;
            // "connected" was an old status tied to the use of the local API, which is no longer used.
            // For now, we will just use the fact that Teams is running as a proxy for being connected.
            currentState.IsConnected = _teamsApp != null;

            if (_teamsMeetingWindow?.isAvailable ?? false)
            {
                // no need to re-find the window. Just re-check mute/unmute buttons in case the window has changed state (e.g. muted/unmuted)
                _teamsMeetingWindow.LoadMuteUnmuteState();
            }
            else
            {
                // scan all windows to find an active meeting window (sets to null if not found)
                _teamsMeetingWindow = _teamsApp?.GetAllTopLevelWindows(_automation)
                    .Select(w => new TeamsUiWindow(w))
                    .FirstOrDefault(w => w.IsMeetingWindow);
            }

            // if we found a meeting window, then we're in a meeting
            currentState.IsInMeeting = _teamsMeetingWindow != null;

            currentState.IsMicrophoneLive = _teamsMeetingWindow?.IsMicrophoneLive ?? false;

            return currentState;
        }

        /// <summary>
        /// Publishes the current Teams state back to the orchestrator, so the appropriate output can be sent to the hardware
        /// </summary>
        private async Task PublishStateAsync(TeamsState state, CancellationToken cancellationToken)
        {
            await _stateWriter.StateWriter.WriteAsync(state, cancellationToken);
        }

        /// <summary>
        /// Handles commands sent from the hardware (caused by button presses)
        /// </summary>
        private async Task CommandLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var command in _commandReader.CommandReader.ReadAllAsync(cancellationToken))
                {
                    if ((_teamsMeetingWindow?.isAvailable ?? false) == false)
                    {
                        _logger.LogWarning("Dropping command {Action} — Not currently in a Teams meeting.", command);
                        continue;
                    }

                    try
                    {
                        SendCommandToTeams(command, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send command {Action}.", command);
                    }
                }
            }
            catch (OperationCanceledException) { /* normal shutdown */ }
        }

        private void SendCommandToTeams(TeamsApplicationCommand command, CancellationToken cancellationToken)
        {
            switch (command)
            {
                case TeamsApplicationCommand.ToggleMute:
                    _teamsMeetingWindow?.ToggleMute();
                    break;

                default:
                    _logger.LogWarning("Unknown command {Action} received.", command);
                    break;
            }
        }

    }
}
