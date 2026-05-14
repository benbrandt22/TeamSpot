using HidSharp.Reports;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Drawing;
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
    private readonly ITeamsStateReader _stateReader;
    private readonly ITeamsCommandWriter _commandWriter;
    private Stopwatch _buttonPressedTimer = new();
    private TimeSpan _longPressThreshold = TimeSpan.FromMilliseconds(1000);

    public OrchestratorService(HidMessageBus hidMessageBus,
        ILogger<OrchestratorService> logger,
        ITeamsStateReader stateReader, ITeamsCommandWriter commandWriter)
    {
        _hidMessageBus = hidMessageBus;
        _logger = logger;
        _stateReader = stateReader;
        _commandWriter = commandWriter;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            await Task.WhenAll(
                HandleUsbInputReports(ct),
                HandleTeamsStateUpdates(ct)
            );
        }
        catch (OperationCanceledException)
        {
            // When the stopping token is canceled, for example, a call made from services.msc,
            // we shouldn't exit with a non-zero exit code. In other words, this is expected...
        }
        catch (Exception ex)
        {
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

                switch (report)
                {
                    case [0x01, 0x01]:
                        await ButtonDown();
                        break;
                    case [0x01, 0x00]:
                        await ButtonUp();
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
                await DisplayTeamsStateAsync(state.ToSimplifiedState(), ct);
            }
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
        (decimal Brightness, Color Color) ledState = teamsSimplifiedState switch
        {
            TeamsSimplifiedState.Offline => (0.0m, Color.Black),
            TeamsSimplifiedState.Connecting => (0.1m, Color.Blue),
            TeamsSimplifiedState.Connected => (1.0m, Color.Blue),
            TeamsSimplifiedState.MeetingMutedMic => (1.0m, Color.Red),
            TeamsSimplifiedState.MeetingLiveMic => (1.0m, Color.Green),
            _ => (0.0m, Color.Black)
        };

        // TODO: handle actual brightness/RGB once the device supports it. For now just send the true/false signal

        if(ledState.Color == Color.Green)
        {
            await _hidMessageBus.Outbound.Writer.WriteAsync([0x01, 0x01]);
        }
        else
        {
            await _hidMessageBus.Outbound.Writer.WriteAsync([0x01, 0x00]);
        }
    }
}