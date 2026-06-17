using System.Drawing;

namespace TeamSpot.Service.Device
{
    public class ColorAndBrightness
    {
        private byte _brightness = 0;

        public ColorAndBrightness() { }

        public ColorAndBrightness(Color color, byte brightnessPercent) : this(color.R, color.G, color.B, brightnessPercent) { }

        public ColorAndBrightness(byte red, byte green, byte blue, byte brightnessPercent)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(brightnessPercent, 100, nameof(brightnessPercent));

            bool isBlack = red == 0 && green == 0 && blue == 0;

            Red = red;
            Green = green;
            Blue = blue;
            Brightness = isBlack ? (byte)0 : brightnessPercent;
        }

        public byte Red { get; set; } = 0;
        public byte Green { get; set; } = 0;
        public byte Blue { get; set; } = 0;
        public byte Brightness {
            get => _brightness;
            set => _brightness = Math.Clamp(value, (byte)0, (byte)100);
        }

    }
}
