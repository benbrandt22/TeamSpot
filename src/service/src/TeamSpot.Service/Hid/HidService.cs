using HidSharp;

namespace TeamSpot.Service.Hid;

/// <summary>
/// Coordinates three tasks:
/// <list type="bullet">
/// <item>A. HID Reader   — blocks on ReadAsync, enqueues to Inbound</item>
/// <item>B. HID Processor — reads Inbound, enqueues replies to Outbound</item>
/// <item>C. HID Writer   — drains Outbound, sends via ConnectionHolder</item>
/// </list>
/// </summary>
public class HidService : BackgroundService
{
    // TODO: move USB identifiers outside this class to keep the HidService more generic
    private const int VendorId = 0x2E8A; // Raspberry Pi Foundation
    private const int ProductId = 0x101F; // RP2040
    private const uint UsagePage = 0xFF22; // custom usage page as defined in device code

    private TimeSpan ReconnectDelay = TimeSpan.FromSeconds(2);

    private readonly HidConnectionHolder _connection;
    private readonly HidMessageBus _bus;
    private readonly ILogger<HidService> _logger;

    public HidService(
        HidConnectionHolder connection,
        HidMessageBus bus,
        ILogger<HidService> logger)
    {
        _connection = connection;
        _bus = bus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("HidService starting.");

        try
        {
            await Task.WhenAll(
                RunWithLogging(RunHidReaderAsync(ct), "HID Reader", ct),
                RunWithLogging(RunHidWriterAsync(ct), "HID Writer", ct)
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
        finally
        {
            _logger.LogInformation("HidService stopped.");
        }
    }

    /// <summary>
    /// Waits for the device to appear, opens a stream, then blocks on ReadAsync until a report arrives or the device disconnects. On disconnect it cleans up and retries.
    /// </summary>
    private async Task RunHidReaderAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var device = await WaitForDeviceAsync(ct);
            if (device is null) return; // ct was cancelled

            try
            {
                using var stream = device.Open();
                stream.ReadTimeout = Timeout.Infinite;

                _connection.SetStream(stream);
                _logger.LogInformation("HID device connected.");

                int reportLen = device.GetMaxInputReportLength();

                while (!ct.IsCancellationRequested)
                {
                    var buf = new byte[reportLen];

                    // Blocks here — no CPU consumed while waiting
                    int bytesRead = await stream.ReadAsync(buf, ct);

                    var report = bytesRead < buf.Length ? buf[..bytesRead] : buf;

                    _logger.LogDebug("Received {N} bytes from device.", bytesRead);

                    await _bus.Inbound.Writer.WriteAsync(report, ct);
                }
            }
            catch (IOException ex)
            {
                _logger.LogWarning("HID disconnected: {Msg}", ex.Message);
                _connection.SetStream(null);
            }

            if (!ct.IsCancellationRequested)
            {
                _logger.LogInformation("Waiting {Ms}ms before reconnect...", ReconnectDelay.TotalMilliseconds);
                await Task.Delay(ReconnectDelay, ct);
            }
        }
    }

    /// <summary>
    /// Drains the outbound channel and sends each message to the USB device via the connection holder. Has no reconnect logic — that's the reader's job.
    /// </summary>
    private async Task RunHidWriterAsync(CancellationToken ct)
    {
        await foreach (var report in _bus.Outbound.Reader.ReadAllAsync(ct))
        {
            if (!_connection.IsConnected)
            {
                _logger.LogWarning("HID Writer: device not connected, dropping report.");
                continue;
            }

            bool sent = await _connection.SendAsync(report, ct);

            if (sent)
            {
                _logger.LogDebug("Sent {N} bytes to device.", report.Length);
            }
            else
            {
                _logger.LogWarning("HID Writer: send failed (device may have disconnected).");
            }
        }
    }

    private async Task<HidDevice?> WaitForDeviceAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var device = DeviceList.Local
                .GetHidDevices(VendorId, ProductId)
                .FirstOrDefault(device =>
                    device.GetReportDescriptor().DeviceItems
                    .Any(item => item.Usages.GetAllValues()
                    .Any(u => (u >> 16) == UsagePage)));

            if (device is not null)
            {
                return device;
            }

            _logger.LogInformation("HID device not found (VID={V:X4} PID={P:X4} UsagePage={U:X4}). Waiting...", VendorId, ProductId, UsagePage);

            await Task.Delay(ReconnectDelay, ct);
        }

        return null;
    }

    private async Task RunWithLogging(Task task, string name, CancellationToken ct)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("{Name} task cancelled.", name);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "{Name} task faulted.", name);
            throw;
        }
    }
}