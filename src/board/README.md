# Circuit Board

The circuit board was designed with [KiCad](https://www.kicad.org/). I've designed boards in the past with other tools, and this was my first project in KiCad. With a little help from YouTube tutorials, I was able to get up to speed quickly.

The board contains a mix of surface mount and through-hole components. The RGB LEDs, capacitors, and resistor are all surface mount, while the microcontroller and keyboard switch are through-hole.

The schematic was inspired by the [Adafruit Neopixel Jewel](https://www.adafruit.com/product/2226), which uses a similar circuit to control RGB LEDs. The board arranges 6 WS2812B LEDs in a circular pattern, with a single data line running through each LED. The keyboard switch is soldered to the front of the board in the middle of the LEDs.

The microcontroller is mounted on the back of the board, offset from the board using pin headers/sockets. The five pins opposite of the USB plug are not included in the board design. They are not used in the circuit, and removing them leaves space to mount the keyboard switch.

Two 3mm mounitng holes are included to allow two M3 bolts to hold the board in place.

For my personal build, I ordered boards from [PCBWay](https://www.pcbway.com/) mainly for their hobbyist-friendly pricing. They also provide a KiCad plugin that makes it easy to export and upload design files and order the boards online.

## Useful Links:

- [KiCad](https://www.kicad.org/)
- [Adafruit Neopixel Jewel](https://www.adafruit.com/product/2226)
- [Neopixel Schematics and PCBs](https://learn.adafruit.com/adafruit-neopixel-uberguide/downloads)
- [RP2040-Zero Symbol/Footprint Download](https://www.snapeda.com/parts/RP2040-ZERO/Waveshare%20Electronics/view-part/)
- [PCBWay](https://www.pcbway.com/)
- [PCBWay KiCad Plugin Info](https://www.pcbway.com/blog/News/PCBWay_Plug_In_for_KiCad_3ea6219c.html)