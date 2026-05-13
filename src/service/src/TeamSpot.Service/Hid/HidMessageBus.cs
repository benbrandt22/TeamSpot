using System.Threading.Channels;

namespace TeamSpot.Service.Hid;

/// <summary>
/// The channel pair that connects the reader and writer tasks. Inject this anywhere you need to enqueue outbound messages
/// or consume inbound ones — easy to extend later when you add WebSocket or other transports.
/// </summary>
public class HidMessageBus
{
    /// <summary>
    /// reader -> processor
    /// </summary>
    public Channel<byte[]> Inbound { get; }

    /// <summary>
    /// processor -> writer
    /// </summary>
    public Channel<byte[]> Outbound { get; }

    public HidMessageBus()
    {
        var opts = new BoundedChannelOptions(64)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = true,
        };

        Inbound = Channel.CreateBounded<byte[]>(opts);
        Outbound = Channel.CreateBounded<byte[]>(opts);
    }
}
