# TeamSpot

Hardware Mute Control for Microsoft Teams

## Why does this exist?

This project was created because it seemed like a fun idea for a small electronics project to try some new things. In my professional life, I use Microsoft Teams for meetings and calls. During most meetings I try to have good microphone etiquette, muting myself when I'm not speaking to minimize background noise and distractions for others. When I wanted to speak, I didn't like having to grab my mouse and find the un-mute button. Sometimes that meant extra seconds of silence before I could respond. Sometimes I'd forget I'm on mute because I had another appication active. Keyboard shortcuts didn't seem like the right solution either, as sometimes I'm using another application that's active in front of MS Teams. I was in the mood to build some hardware, so a physical button seemed like the right way to handle this.

The other key objective was to get good visual feedback of my microphone's current mute status. I wanted the device to reflect the actual status of the microphone in Teams, so that if I muted myself using the software controls, the device would show that as well.

## Teams Integration

My initial research revealed a local websocket API that published realtime events from Teams, and had the ability to send commands to Teams. This was originally released (I think) around 2023 and was heavily used by the Elgato Stream Deck to allow similar control and status display integration with Teams.

Some related links:
- https://github.com/malkstar/ms_teams_websockets
- https://github.com/svrooij/teams-monitor/issues/15
- https://github.com/svrooij/teams-monitor
- https://support.microsoft.com/en-us/office/connect-to-third-party-devices-in-microsoft-teams-aabca9f2-47bb-407f-9f9b-81a104a883d6?wt.mc_id=SEC-MVP-5004985

When I discovered this, I also found articles/posts that revealed that Microsoft was planning on deprecating and/or removing this API, announced in late 2025. The Teams instance on my personal computer did not have this API enabled. However, my Teams installation on my work laptop did have it enabled. They were both on the same version, so something about my corporate account had this feature enabled. It could still go away someday, but for now it works and that's good enough for my little project.

## Hardware

The hardware is based off of a RP2040-Zero microcontroller, running [CircuitPython](https://circuitpython.org/). I chose this device because:

- Supports USB HID, and I wanted to learn USB communications. USB is also simpler setup on the host PC, as I don't need to worry about choosing and configuring COM ports for serial communication
- Small footprint, so I can easily fit this into a small 3D-printable enclosure
- Low-cost. I found a 3-pack on Amazon for around $10
- CircuitPython support, which I have some past experience with. I really like the easy development-loop experience of just updating script files directly on the device.
- Includes a built-in RGB LED, which I can use for visual feedback (unless I find it's not bright enough on its own)