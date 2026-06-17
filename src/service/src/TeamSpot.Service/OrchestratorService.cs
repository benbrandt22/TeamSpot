using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Drawing;
using TeamSpot.Service.Device;
using TeamSpot.Service.Hid;
using TeamSpot.Service.Teams;

namespace TeamSpot.Service;

/// <summary>
/// Monitors USB device and Teams activity and triggers events between each of them
/// </summary>
public sealed class OrchestratorService : BackgroundService
{
    private readonly HidMessageBus _hidMessageBus;
    private readonly ILogger<OrchestratorService> _logger;
    private readonly IOptionsMonitor<StatusColorsSettings> _colorSettings;
    private readonly ITeamsStateReader _stateReader;
    private readonly ITeamsCommandWriter _commandWriter;
    private Stopwatch _buttonPressedTimer = new();
    private TimeSpan _longPressThreshold = TimeSpan.FromMilliseconds(1000);
    private TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(3);
    private TeamsSimplifiedState _currentState;

    public OrchestratorService(HidMessageBus hidMessageBus,
        ILogger<OrchestratorService> logger,
        IOptionsMonitor<StatusColorsSettings> colorSettings,
        ITeamsStateReader stateReader, ITeamsCommandWriter commandWriter)
    {
        _hidMessageBus = hidMessageBus;
        _logger = logger;
        _colorSettings = colorSettings;
        _stateReader = stateReader;
        _commandWriter = commandWriter;
        _currentState = TeamsSimplifiedState.Offline;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            await DisplayTeamsStateAsync(TeamsSimplifiedState.Offline, ct);

            await Task.WhenAll(
                HandleUsbInputReports(ct),
                HandleTeamsStateUpdates(ct),
                HeartbeatDisplayCurrentState(ct)
            );
        }
        catch (OperationCanceledException)
        {
            // When the stopping token is canceled, for example, a call made from services.msc,
            // we shouldn't exit with a non-zero exit code. In other words, this is expected...
            await DisplayTeamsStateAsync(TeamsSimplifiedState.Offline, ct);
        }
        catch (Exception ex)
        {
            await DisplayTeamsStateAsync(TeamsSimplifiedState.Offline, ct);
            _logger.LogError(ex, "{Message}", ex.Message);

            // Terminates this process and returns an exit code to the operating system.
            // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
            // performs one of two scenarios:
            // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
            // 2. When set to "StopHost": will cleanly stop the host, and log errors.
            //
            // In order for the Windows Service Management system to leverage configured
            // recovery options, we need to terminate the process with a non-zero exit code.
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Reads input reports from the HID device, and triggers appropriate events based on the report content.
    /// </summary>
    private async Task HandleUsbInputReports(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await foreach (var report in _hidMessageBus.Inbound.Reader.ReadAllAsync(ct))
            {
                _logger.LogDebug("Processing input report: {Hex}", Convert.ToHexString(report));

                var inputEvent = UsbInputEventParser.TryParse(report);

                switch (inputEvent)
                {
                    case ButtonStateChange { State: ButtonState.Down }:
                        await ButtonDown();
                        break;
                    case ButtonStateChange { State: ButtonState.Up }:
                        await ButtonUp();
                        break;
                    default:
                        _logger.LogWarning("Received unknown input event {type}", inputEvent?.GetType().Name);
                        break;
                }

            }

        }
    }

    private async Task HandleTeamsStateUpdates(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await foreach (var state in _stateReader.StateReader.ReadAllAsync(ct))
            {
                // state change received from Teams
                _currentState = state.ToSimplifiedState();
                await DisplayTeamsStateAsync(_currentState, ct);
            }
        }
    }

    /// <summary>
    /// Periodically sends the current state (color) to the device, to ensure it doesn't get out of sync
    /// especially after USB disconnects/reconnects, which may cause the device to lose its state.
    /// </summary>
    private async Task HeartbeatDisplayCurrentState(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(_heartbeatInterval, ct);
            await DisplayTeamsStateAsync(_currentState, ct);
        }
    }

    private async Task ButtonDown()
    {
        _logger.LogDebug("Button pressed down");
        _buttonPressedTimer.Restart();
        await ToggleMuteAsync();
    }

    private async Task ButtonUp()
    {
        _buttonPressedTimer.Stop();
        var pressedDuration = _buttonPressedTimer.Elapsed;
        _buttonPressedTimer.Reset();

        if (pressedDuration == TimeSpan.Zero)
        {
            // button down event was not registered, no way to know if it was short or long press... ignore
            return;
        }

        var wasLongPress = (pressedDuration > _longPressThreshold);
        _logger.LogDebug($"Button released after {pressedDuration.TotalMilliseconds} ms{ (wasLongPress ? " (Long Press)" : "") }");

        if (wasLongPress) {
            // button was held awhile (hold-to-talk or hold-to-mute), now toggle back to what it was before
            await ToggleMuteAsync();
        }
    }

    private async Task ToggleMuteAsync()
    {
        _logger.LogDebug("Toggling Mute");
        await _commandWriter.CommandWriter.WriteAsync(TeamsCommand.ToggleMute());
    }

    private async Task DisplayTeamsStateAsync(TeamsSimplifiedState teamsSimplifiedState, CancellationToken ct)
    {
        var colors = _colorSettings.CurrentValue;

        ColorAndBrightness colorAndBrightness = teamsSimplifiedState switch
        {
            TeamsSimplifiedState.Offline => colors.Offline,
            TeamsSimplifiedState.Connecting => colors.ConnectedToTeams,
            TeamsSimplifiedState.Connected => colors.ConnectedToTeams,
            TeamsSimplifiedState.MeetingMutedMic => colors.MeetingMutedMic,
            TeamsSimplifiedState.MeetingLiveMic => colors.MeetingLiveMic,
            _ => new ColorAndBrightness(Color.Black, 0)
        };

        var ledState = new SetLedOutput(colorAndBrightness);

        await _hidMessageBus.Outbound.Writer.WriteAsync(ledState.ToUsbOutputReport());
    }
}