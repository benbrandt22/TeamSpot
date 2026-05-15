# type: ignore
import time
import board
import digitalio
import neopixel
import usb_hid

# RP2040-Zero board members: (dir(board))
# ['__class__', '__name__', 'A0', 'A1', 'A2', 'A3', 'GP0', 'GP1', 'GP10', 'GP11', 'GP12', 'GP13', 'GP14', 'GP15', 'GP16', 'GP17', 'GP18', 'GP19', 'GP2', 'GP20', 'GP21', 'GP22', 'GP23', 'GP24', 'GP25', 'GP26', 'GP26_A0', 'GP27', 'GP27_A1', 'GP28', 'GP28_A2', 'GP29', 'GP29_A3', 'GP3', 'GP4', 'GP5', 'GP6', 'GP7', 'GP8', 'GP9', 'NEOPIXEL', 'RX', 'TX', 'UART', '__dict__', 'board_id']

# Get our custom HID device
device = None

def find_device(usage_page, usage):
    for device in usb_hid.devices:
        if device.usage_page == usage_page and device.usage == usage:
            return device
    raise ValueError("Custom HID device not found")

device = find_device(usage_page=0xFF22, usage=0x01)

# ----- BUTTON SETUP -----

button = digitalio.DigitalInOut(board.GP0)
button.switch_to_input(pull=digitalio.Pull.UP)

# ----- STATE TRACKING -----

last_pressed = False

# ----- NEOPIXEL SETUP -----

pixel = neopixel.NeoPixel(board.NEOPIXEL, 1, brightness=0.2)

def show_color(r, g, b, brightness_percent):
    pixel.brightness = brightness_percent / 100.0
    pixel[0] = (r, g, b)

# ----- STARTUP ANIMATION -----

fadeTimeSec = 0.2
for i in range(0, 101, 2): # fade in
    show_color(0, 255, 0, i)
    time.sleep(fadeTimeSec/50)
time.sleep(1) #hold
for i in range(100, -1, -2): # fade out
    show_color(0, 255, 0, i)
    time.sleep(fadeTimeSec/50)

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
    if output is not None:
        print(len(output));
        print(output);
    if output is not None and len(output) == 4:
        r, g, b, brightness_percent = output[0], output[1], output[2], output[3]
        show_color(r, g, b, brightness_percent)
