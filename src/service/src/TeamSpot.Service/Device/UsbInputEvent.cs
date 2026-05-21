namespace TeamSpot.Service.Device
{
    public abstract class UsbInputEvent
    {

        // TODO: evolve this so that each event contains its own parsing logic

        public static UsbInputEvent FromReport(byte[] inputReport)
        {
            return inputReport switch
            {
                [0x01, 0x01] => new ButtonStateChange(ButtonState.Down),
                [0x01, 0x00] => new ButtonStateChange(ButtonState.Up),
                _ => throw new ArgumentException("Unrecognized USB input report", nameof(inputReport))
            };
        }

    }



}
