namespace TeamSpot.Service.Teams
{
    /// <summary>
    /// Basic state of Microsoft Teams that we care about for the purposes of this application
    /// </summary>
    public class TeamsState
    {
        public bool IsTeamsRunning { get; set; }
        public bool IsConnected { get; set; }
        public bool IsInMeeting { get; set; }
        public bool IsMicrophoneLive { get; set; }


        public TeamsSimplifiedState ToSimplifiedState()
        {
            var simplifiedState = this switch
            {
                { IsTeamsRunning: false } => TeamsSimplifiedState.Offline,
                { IsTeamsRunning: true, IsConnected: false } => TeamsSimplifiedState.Connecting,
                { IsTeamsRunning: true, IsConnected: true, IsInMeeting: false } => TeamsSimplifiedState.Connected,
                { IsTeamsRunning: true, IsConnected: true, IsInMeeting: true, IsMicrophoneLive: false } => TeamsSimplifiedState.MeetingMutedMic,
                { IsTeamsRunning: true, IsConnected: true, IsInMeeting: true, IsMicrophoneLive: true } => TeamsSimplifiedState.MeetingLiveMic
            };

            return simplifiedState;
        }
    }
}
