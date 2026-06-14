import time
import digitalio
import neopixel
import usb_hid
import math
import pins

# Get our custom HID device
device = None

def find_device(usage_page, usage):
    for device in usb_hid.devices:
        if device.usage_page == usage_page and device.usage == usage:
            return device
    raise ValueError("Custom HID device not found")

device = find_device(usage_page=0xFF22, usage=0x01)

# ----- BUTTON SETUP -----

button = digitalio.DigitalInOut(pins.BUTTON_PIN)
button.switch_to_input(pull=digitalio.Pull.UP)

# ----- STATE TRACKING -----

last_pressed = False

# ----- NEOPIXEL SETUP -----

num_pixels = 6
pixel_pin = pins.PIXEL_PIN
pixels = neopixel.NeoPixel(pixel_pin, num_pixels, brightness=0.2, auto_write=False)

def show_color(r, g, b, brightness_percent):
    for i in range(num_pixels):
        pixels.brightness = brightness_percent / 100.0
        pixels[i] = (r, g, b)
    pixels.show()

# ----- STARTUP ANIMATION -----

def spin(color=(0, 255, 0), laps=3, speed=num_pixels, fade_laps=1): # speed = pixels/sec
    r, g, b = color
    total_distance = num_pixels * laps
    fade_start = total_distance - (num_pixels * fade_laps)
    pos = 0.0

    while pos < total_distance:
        # how far into the fade zone are we? 0.0 -> 1.0
        if pos >= fade_start:
            t = 1.0 - (pos - fade_start) / (num_pixels * fade_laps)
        else:
            t = 1.0

        lo = int(pos) % num_pixels
        hi = (lo + 1) % num_pixels
        frac = pos - math.floor(pos)

        pixels.fill((0, 0, 0))
        pixels[lo] = (round(r * (1.0 - frac) * t), round(g * (1.0 - frac) * t), round(b * (1.0 - frac) * t))
        pixels[hi] = (round(r * frac * t),          round(g * frac * t),          round(b * frac * t))
        pixels.show()

        UPDATE_RATE = 0.02 # seconds per frame
        pos += speed * UPDATE_RATE
        time.sleep(UPDATE_RATE)

    pixels.fill((0, 0, 0))
    pixels.show()


spin((0,0,255), 2)

# ----- MAIN LOOP -----

while True:

    # Buttons are active LOW
    pressed = not button.value

    # Only send report when state changes
    if pressed != last_pressed:

        report = bytearray(1)
        report[0] = 0x01 if pressed else 0x00

        device.send_report(report, report_id=1)
        time.sleep(0.05)

        last_pressed = pressed

    # --- Receive output report from host ---
    output = device.get_last_received_report(report_id=1)
    if output is not None and len(output) == 4:
        r, g, b, brightness_percent = output[0], output[1], output[2], output[3]
        show_color(r, g, b, brightness_percent)
