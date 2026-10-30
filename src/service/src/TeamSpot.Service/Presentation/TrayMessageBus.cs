using System.Threading.Channels;

namespace TeamSpot.Service.Teams
{
    public interface ITrayStateWriter { ChannelWriter<TeamsSimplifiedState> StateWriter { get; } }
    public interface ITrayStateReader { ChannelReader<TeamsSimplifiedState> StateReader { get; } }

    /// <summary>
    /// Shared message bus for Teams state information to be passed to the system tray UI.
    /// This is intended to be used as a singleton service that serves as a bridge between the Orchestrator and the system tray UI.
    /// </summary>
    public class TrayMessageBus : ITrayStateWriter, ITrayStateReader
    {
        private readonly Channel<TeamsSimplifiedState> _stateUpdates = Channel.CreateUnbounded<TeamsSimplifiedState>();

        // Orchestrator writes state, tray interface reads it
        public ChannelWriter<TeamsSimplifiedState> StateWriter => _stateUpdates.Writer;
        public ChannelReader<TeamsSimplifiedState> StateReader => _stateUpdates.Reader;
    }
}
