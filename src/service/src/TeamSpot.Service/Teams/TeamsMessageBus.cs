using System.Threading.Channels;

namespace TeamSpot.Service.Teams
{
    // Distinct interfaces — each consumer only sees what it needs
    public interface ITeamsStateWriter { ChannelWriter<TeamsState> StateWriter { get; } }
    public interface ITeamsStateReader { ChannelReader<TeamsState> StateReader { get; } }
    public interface ITeamsCommandWriter { ChannelWriter<TeamsApplicationCommand> CommandWriter { get; } }
    public interface ITeamsCommandReader { ChannelReader<TeamsApplicationCommand> CommandReader { get; } }

    /// <summary>
    /// Shared message bus for Teams-related events and commands.
    /// This is intended to be used as a singleton service that serves as a bridge between Teams and Orchestrator/USB background services.
    /// </summary>
    public class TeamsMessageBus : ITeamsStateWriter, ITeamsStateReader, ITeamsCommandWriter, ITeamsCommandReader
    {
        private readonly Channel<TeamsState> _stateUpdates = Channel.CreateUnbounded<TeamsState>();

        private readonly Channel<TeamsApplicationCommand> _commands = Channel.CreateBounded<TeamsApplicationCommand>(capacity: 10);

        // Teams service writes state; everything else reads it
        public ChannelWriter<TeamsState> StateWriter => _stateUpdates.Writer;
        public ChannelReader<TeamsState> StateReader => _stateUpdates.Reader;

        // Everything else writes commands; Teams service reads them
        public ChannelWriter<TeamsApplicationCommand> CommandWriter => _commands.Writer;
        public ChannelReader<TeamsApplicationCommand> CommandReader => _commands.Reader;
    }
}
