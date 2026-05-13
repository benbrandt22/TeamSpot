using System.Diagnostics;
using TeamSpot.Service.Hid;

namespace TeamSpot.Service;

/// <summary>
/// Monitors USB device and Teams activity and triggers events between each of them
/// </summary>
public sealed class OrchestratorService : BackgroundService
{
    private readonly HidMessageBus _hidMessageBus;
    private readonly ILogger<OrchestratorService> _logger;

    private Stopwatch _buttonPressedTimer = new();
    private TimeSpan _longPressThreshold = TimeSpan.FromMilliseconds(1000);

    public OrchestratorService(HidMessageBus hidMessageBus, ILogger<OrchestratorService> logger)
    {
        _hidMessageBus = hidMessageBus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {

                await foreach (var report in _hidMessageBus.Inbound.Reader.ReadAllAsync(ct))
                {
                    _logger.LogDebug("Processing input report: {Hex}", Convert.ToHexString(report));

                    switch(report)
                    {
                        case [0x01, 0x01]:
                            ButtonDown();
                            break;
                        case [0x01, 0x00]:
                            ButtonUp();
                            break;
                    };

                }

            }
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

    private void ButtonDown()
    {
        _logger.LogDebug("Button pressed down");
        _buttonPressedTimer.Restart();
        ToggleMute();
    }

    private void ButtonUp()
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
            ToggleMute();
        }
    }

    private void ToggleMute()
    {
        // TODO: send signal to Teams to toggle mute
        _logger.LogInformation("TOGGLE MUTE");
    }

    
}