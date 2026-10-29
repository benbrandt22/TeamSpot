using FlaUI.Core.AutomationElements;

namespace TeamSpot.Service.Teams
{
    public class TeamsUiWindow
    {
        private readonly Window _window;
        private Button? _muteButton;
        private Button? _unmuteButton;

        public TeamsUiWindow(Window window)
        {
            _window = window;
            LoadMuteUnmuteState();
        }

        /// <summary>
        /// Scans the window UI to determine muted/unmuted state and locate the button
        /// </summary>
        public void LoadMuteUnmuteState()
        {
            _muteButton = FindButtonByName("Mute mic");
            _unmuteButton = FindButtonByName("Unmute mic");
        }

        /// <summary>
        /// Scans window descendants for a button by the given name. Returns a button,
        /// or null if not found (or if an exception is thrown)
        /// </summary>
        private Button? FindButtonByName(string name)
        {
            try
            {
                return _window.FindFirstDescendant(cf => cf.ByName(name))?.AsButton();
            }
            catch (Exception)
            {
                // exceptions can occasionally be thrown (if window is closed while scanning)
                return null;
            }
        }

        /// <summary>
        /// Returns true if the window is still open
        /// </summary>
        public bool isAvailable => _window.IsAvailable;

        public bool IsMeetingWindow => _muteButton != null ^ _unmuteButton != null;

        public bool IsMicrophoneLive => _muteButton != null; // if the mute button is present, then the microphone is live (not muted)

        public void Mute() => _muteButton?.Invoke();

        public void Unmute() => _unmuteButton?.Invoke();

        public void ToggleMute()
        {
            if (_muteButton != null)
            {
                Mute();
            }
            else if (_unmuteButton != null)
            {
                Unmute();
            }
        }
    }
}
