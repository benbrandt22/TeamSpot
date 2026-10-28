using System.Text.Json.Serialization;

namespace TeamSpot.Service.Teams.ObsoleteApi
{
    [Obsolete("Teams no longer supports this local API as of August 2026. Keeping this code here for posterity for the time being.")]
    public class TeamsApiCommand
    {
        [JsonPropertyName("action")]
        public string Action { get; set; } = "";

        [JsonPropertyName("parameters")]
        public object Parameters { get; set; } = new { };

        [JsonPropertyName("requestId")]
        public int RequestId { get; set; } = 1;

        // Convenience factories
        public static TeamsApiCommand ToggleMute() => new() { Action = "toggle-mute" };
        public static TeamsApiCommand ToggleVideo() => new() { Action = "toggle-video" };
        public static TeamsApiCommand ToggleHand() => new() { Action = "toggle-hand" };
        public static TeamsApiCommand LeaveCall() => new() { Action = "leave-call" };
        public static TeamsApiCommand ToggleBackgroundBlur() => new() { Action = "toggle-background-blur" };
        public static TeamsApiCommand SendReaction(ReactionType reaction) => new() {
            Action = "send-reaction",
            Parameters = new { type = reaction.ToString().ToLower() }
        };

        public enum ReactionType
        {
            Like,
            Love,
            Applause,
            Wow,
            Laugh
        }
    }
}
