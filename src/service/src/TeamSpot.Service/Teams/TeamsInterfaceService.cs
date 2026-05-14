using System.Diagnostics;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace TeamSpot.Service.Teams
{
    public class TeamsInterfaceService : BackgroundService
    {
        private readonly ILogger<TeamsInterfaceService> _logger;
        private readonly SecureSettingsService<TeamSpotSettings> _settingsService;
        private readonly ITeamsStateWriter _stateWriter;
        private readonly ITeamsCommandReader _commandReader;

        private ClientWebSocket? _ws;
        private Task? _receiveLoop;
        private Task? _commandLoop;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public TeamsInterfaceService(ILogger<TeamsInterfaceService> logger,
            SecureSettingsService<TeamSpotSettings> settingsService,
            ITeamsStateWriter stateWriter, ITeamsCommandReader commandReader)
        {
            _logger = logger;
            _settingsService = settingsService;
            _stateWriter = stateWriter;
            _commandReader = commandReader;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                if (_ws?.State == WebSocketState.Open){
                    continue;
                }

                if (await IsTeamsAvailableAsync())
                {
                    try
                    {
                        await ConnectAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Teams connection attempt failed; will retry.");
                        await PublishStateAsync(new TeamsState { IsTeamsRunning = true, IsConnected = false }, stoppingToken);
                    }
                }
                else
                {
                    await CleanupSocketAsync();
                    await PublishStateAsync(new TeamsState { IsTeamsRunning = false, IsConnected = false }, stoppingToken);
                }
            }

            await CleanupSocketAsync();
            _stateWriter.StateWriter.TryComplete();
        }

        /// <summary>
        /// Verifies if Teams is running by checking for its process and attempting to connect to its local WebSocket port.
        /// </summary>
        private async Task<bool> IsTeamsAvailableAsync()
        {
            bool processRunning = Process.GetProcessesByName("ms-teams")
                                         .Concat(Process.GetProcessesByName("Teams"))
                                         .Any();

            if (!processRunning) return false;

            // process is running, see if Teams is listening on the expected WebSocket port
            try
            {
                using var tcp = new TcpClient();
                await tcp.ConnectAsync("localhost", 8124);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task ConnectAsync(CancellationToken cancellationToken)
        {
            await CleanupSocketAsync();

            var apiConnection = new TeamsApiConnection(_settingsService.Settings.TeamsApiToken);

            _ws = new ClientWebSocket();
            await _ws.ConnectAsync(apiConnection.ToTeamsApiUrl(), cancellationToken);

            _logger.LogInformation("Connected to Teams local API.");

            _receiveLoop = Task.Run(() => ReceiveLoopAsync(cancellationToken), cancellationToken);
            _commandLoop = Task.Run(() => CommandLoopAsync(cancellationToken), cancellationToken);
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];

            try
            {
                while (_ws!.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closed", CancellationToken.None);
                            await PublishStateAsync(new TeamsState { IsTeamsRunning = true, IsConnected = false }, cancellationToken);
                            return;
                        }

                        ms.Write(buffer, 0, result.Count);
                    }
                    while (!result.EndOfMessage);

                    await ProcessMessageAsync(Encoding.UTF8.GetString(ms.ToArray()), cancellationToken);
                }
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
                    if (_ws?.State != WebSocketState.Open)
                    {
                        _logger.LogWarning("Dropping command {Action} — WebSocket not open.", command.Action);
                        continue;
                    }

                    try
                    {
                        await SendCommandAsync(command, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send command {Action}.", command.Action);
                    }
                }
            }
            catch (OperationCanceledException) { /* normal shutdown */ }
        }

        private async Task SendCommandAsync(TeamsCommand command, CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Serialize(command);

            _logger.LogDebug("Sending Teams command:\r\n{Command}", payload);

            var bytes = Encoding.UTF8.GetBytes(payload);

            await _ws!.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken);
        }

        private async Task ProcessMessageAsync(string json, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Received Teams message:\r\n{Message}", json);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Token refresh
                if (root.TryGetProperty("tokenRefresh", out var tokenEl))
                {
                    var token = tokenEl.GetString();
                    if (token != null)
                    {
                        _logger.LogInformation("Teams token refreshed.");
                        // save the new token for future connections
                        _settingsService.Update(x => x.TeamsApiToken = token);
                    }
                }

                // Meeting update
                if (root.TryGetProperty("meetingUpdate", out var updateElement))
                {
                    var meetingUpdateJson = updateElement.GetRawText();
                    var meetingUpdate = JsonSerializer.Deserialize<MeetingUpdate>(meetingUpdateJson, _jsonOptions)!;

                    if (meetingUpdate.MeetingState != null)
                    {
                        await PublishStateAsync(new TeamsState
                        {
                            IsTeamsRunning = true,
                            IsConnected = true,
                            IsInMeeting = meetingUpdate.MeetingState.IsInMeeting,
                            IsMicrophoneLive = !meetingUpdate.MeetingState.IsMuted
                        }, cancellationToken);
                    }
                    else if (meetingUpdate.MeetingState == null && meetingUpdate.MeetingPermissions.CanPair == true)
                    {
                        // we're not paired yet, send a command to start the pairing process
                        _logger.LogInformation("Initiating pairing with Teams.");
                        await SendCommandAsync(TeamsCommand.SendReaction(TeamsCommand.ReactionType.Like), cancellationToken);
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogDebug(ex, "Malformed Teams message ignored.");
            }
        }

        private async Task PublishStateAsync(TeamsState state, CancellationToken cancellationToken)
        {
            await _stateWriter.StateWriter.WriteAsync(state, cancellationToken);
        }

        private async Task CleanupSocketAsync()
        {
            // Wait for background loops to finish
            if (_receiveLoop != null || _commandLoop != null)
            {
                try
                {
                    await Task.WhenAll(
                        _receiveLoop ?? Task.CompletedTask,
                        _commandLoop ?? Task.CompletedTask)
                        .WaitAsync(TimeSpan.FromSeconds(2));
                }
                catch { /* best effort */ }

                _receiveLoop = null;
                _commandLoop = null;
            }

            if (_ws != null)
            {
                if (_ws.State == WebSocketState.Open)
                {
                    try
                    {
                        await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconnecting", CancellationToken.None);
                    }
                    catch { /* ignore close errors */ }
                }

                _ws.Dispose();
                _ws = null;
            }
        }
    }
}
