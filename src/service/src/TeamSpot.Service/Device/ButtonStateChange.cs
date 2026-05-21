namespace TeamSpot.Service.Device
{
    /// <summary>
    /// USB event that indicates a button state change (down or up)
    /// </summary>
    public class ButtonStateChange(ButtonState state) : UsbInputEvent
    {
        // TODO: consider evolving this to include a button number for future multi-button flexibility

        public ButtonState State { get; } = state;
    }

    public enum ButtonState
    {
        Down,
        Up
    }
}
