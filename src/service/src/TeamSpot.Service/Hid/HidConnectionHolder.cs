using HidSharp;

namespace TeamSpot.Service.Hid;

/// <summary>
/// Owns the single live HidStream. Shared between the reader and writer tasks. The reader sets the stream after each
/// successful connect; the writer calls SendAsync() without needing to know anything about reconnection logic.
/// </summary>
public class HidConnectionHolder
{
    private HidStream? _stream;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public bool IsConnected => _stream is not null;

    public void SetStream(HidStream? stream)
    {
        _stream = stream;
    }

    /// <summary>
    /// Sends an output report to the device.
    /// Returns false if no device is connected or the send fails.
    /// </summary>
    public async Task<bool> SendAsync(byte[] report, CancellationToken ct)
    {
        if (_stream is null)
        {
            return false;
        }

        await _writeLock.WaitAsync(ct);
        try
        {
            await _stream.WriteAsync(report, ct);
            return true;
        }
        catch (IOException)
        {
            _stream = null; // reader task will detect disconnect and reconnect
            return false;
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
