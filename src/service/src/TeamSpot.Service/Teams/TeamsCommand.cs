using System.Text.Json.Serialization;

namespace TeamSpot.Service.Teams
{
    public class TeamsCommand
    {
        [JsonPropertyName("action")]
        public string Action { get; set; } = "";

        [JsonPropertyName("parameters")]
        public object Parameters { get; set; } = new { };

        [JsonPropertyName("requestId")]
        public int RequestId { get; set; } = 1;

        // Convenience factories
        public static TeamsCommand ToggleMute() => new() { Action = "toggle-mute" };
        public static TeamsCommand ToggleVideo() => new() { Action = "toggle-video" };
        public static TeamsCommand ToggleHand() => new() { Action = "toggle-hand" };
        public static TeamsCommand LeaveCall() => new() { Action = "leave-call" };
        public static TeamsCommand ToggleBackgroundBlur() => new() { Action = "toggle-background-blur" };
        public static TeamsCommand SendReaction(ReactionType reaction) => new() {
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
