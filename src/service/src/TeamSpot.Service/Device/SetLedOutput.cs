namespace TeamSpot.Service.Device
{
    public class SetLedOutput
    {
        private readonly ColorAndBrightness _colorAndBrightness;

        public SetLedOutput(ColorAndBrightness colorAndBrightness)
        {
            _colorAndBrightness = colorAndBrightness;
        }

        /// <summary>
        /// Converts to USB output report format expected by the device.
        /// </summary>
        public byte[] ToUsbOutputReport()
        {
            byte outputReportId = 0x01; // report ID for LED output
            return new byte[] {
                outputReportId,
                _colorAndBrightness.Red,
                _colorAndBrightness.Green,
                _colorAndBrightness.Blue,
                _colorAndBrightness.Brightness
            };
        }
    }
}
