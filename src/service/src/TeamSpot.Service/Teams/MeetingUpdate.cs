namespace TeamSpot.Service.Teams
{
    public class MeetingUpdate
    {
        public MeetingPermissions MeetingPermissions { get; set; } = new();
        public MeetingState? MeetingState { get; set; }
    }

    public class MeetingPermissions
    {
        public bool CanReact { get; set; }
        public bool CanToggleVideo { get; set; }
        public bool CanToggleMute { get; set; }
        public bool CanToggleHand { get; set; }
        public bool CanToggleShareTray { get; set; }
        public bool CanLeave { get; set; }
        public bool CanToggleBlur { get; set; }
        public bool CanToggleChat { get; set; }
        public bool CanStopSharing { get; set; }
        public bool CanPair { get; set; }
    }

    public class MeetingState
    {
        public bool IsMuted { get; set; }
        public bool IsVideoOn { get; set; }
        public bool IsHandRaised { get; set; }
        public bool IsInMeeting { get; set; }
        public bool IsRecordingOn { get; set; }
        public bool IsBackgroundBlurred { get; set; }
    }

}
