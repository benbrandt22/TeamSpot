namespace TeamSpot.Service.Device
{
    /// <summary>
    /// USB event that indicates a button state change (down or up)
    /// </summary>
    public class ButtonStateChange(ButtonState state) : IUsbInputEvent, IUsbInputEventParser
    {
        // TODO: consider evolving this to include a button number for future multi-button flexibility

        public ButtonState State { get; } = state;

        public static bool CanParse(byte[] inputReport)
        {
            var isCorrectReportId = inputReport[0] == 0x01;
            var isValidButtonState = (inputReport[1] == 0x00 || inputReport[1] == 0x01);
            return isCorrectReportId && isValidButtonState;
        }

        public static IUsbInputEvent Parse(byte[] inputReport)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(inputReport[0], 0x01, "Invalid report ID for ButtonStateChange event.");
            ArgumentOutOfRangeException.ThrowIfGreaterThan(inputReport[1], 1, nameof(inputReport));

            var state = inputReport[1] == 0x01 ? ButtonState.Down : ButtonState.Up;
            return new ButtonStateChange(state);
        }
    }

    public enum ButtonState
    {
        Up,
        Down
    }
}
