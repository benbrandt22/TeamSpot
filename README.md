# TeamSpot

Hardware Mute Control for Microsoft Teams

(Currently a work in progress)

## Why does this exist?

This project was created because it seemed like a fun idea for a small electronics project to try some new things. In my professional life, I use Microsoft Teams for meetings and calls. During most meetings I try to have good microphone etiquette, muting myself when I'm not speaking to minimize background noise and distractions for others. When I wanted to speak, I didn't like having to grab my mouse and find the un-mute button. Sometimes that meant extra seconds of silence before I could respond. Sometimes I'd forget I'm on mute because I had another appication active. Keyboard shortcuts didn't seem like the right solution either, as sometimes I'm using another application that's active in front of MS Teams. I was in the mood to build some hardware, so a physical button seemed like the right way to handle this.

The other key objective was to get good visual feedback of my microphone's current mute status. I wanted the device to reflect the actual status of the microphone in Teams, so that if I muted myself using the software controls, the device would show that as well.

## Hardware

- [Firmware ReadMe](src/firmware/README.md)
- [Circuit Board ReadMe](src/board/README.md)

## Teams Integration

My initial research revealed a local websocket API that published realtime events from Teams, and had the ability to send commands to Teams. This was originally released (I think) around 2023 and was heavily used by the Elgato Stream Deck to allow similar control and status display integration with Teams.

This local Teams API can be seen in MS Teams under Settings > Privacy > Third-Party app API.

Some related links:
- https://techcommunity.microsoft.com/blog/microsoftteamsblog/delivering-new-webinar-experiences-with-microsoft-teams/3725145
- https://github.com/malkstar/ms_teams_websockets
- https://github.com/svrooij/teams-monitor/issues/15
- https://github.com/svrooij/teams-monitor
- https://support.microsoft.com/en-us/office/connect-to-third-party-devices-in-microsoft-teams-aabca9f2-47bb-407f-9f9b-81a104a883d6?wt.mc_id=SEC-MVP-5004985
- https://lostdomain.notion.site/Microsoft-Teams-WebSocket-API-5c042838bc3e4731bdfe679e864ab52a

When I discovered this, I also found articles/posts that revealed that Microsoft was planning on deprecating and/or removing this API, announced in late 2025. The Teams instance on my personal computer did not have this API enabled. However, my Teams installation on my work laptop did have it enabled. They were both on the same version, so something about my corporate account had this feature enabled. It could still go away someday, but for now it works and that's good enough for my little project.

