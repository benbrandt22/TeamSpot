using System.Drawing;
using TeamSpot.Service.Device;

namespace TeamSpot.Service
{
    public class StatusColorsSettings
    {
        public ColorAndBrightness Offline { get; set; } = new(Color.Black, 0);
        public ColorAndBrightness ConnectingToTeams { get; set; } = new(Color.Blue, 5);
        public ColorAndBrightness ConnectedToTeams { get; set; } = new(Color.Blue, 10);
        public ColorAndBrightness MeetingMutedMic { get; set; } = new(Color.Red, 100);
        public ColorAndBrightness MeetingLiveMic { get; set; } = new(Color.Green, 100);
    }
}
