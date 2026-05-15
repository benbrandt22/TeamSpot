using System.Drawing;

namespace TeamSpot.Service.Device
{
    public class SetLedOutput
    {
        public SetLedOutput(Color color, byte brightnessPercent = 100) : this(color.R, color.G, color.B, brightnessPercent) { }

        public SetLedOutput(byte red, byte green, byte blue, byte brightnessPercent = 100)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(brightnessPercent, 100, nameof(brightnessPercent));

            bool isBlack = (red == 0 && green == 0 && blue == 0);

            Red = red;
            Green = green;
            Blue = blue;
            BrightnessPercent = (isBlack ? (byte)0 : brightnessPercent);
        }

        public byte Red { get; }
        public byte Green { get; }
        public byte Blue { get; }
        public byte BrightnessPercent { get; }

        /// <summary>
        /// Converts to USB output report format expected by the device.
        /// </summary>
        public byte[] ToUsbOutputReport()
        {
            byte outputReportId = 0x01; // report ID for LED output
            return new byte[] {
                outputReportId,
                Red,
                Green,
                Blue,
                BrightnessPercent
            };
        }
    }
}
