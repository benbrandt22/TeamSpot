# TeamSpot Windows Service

This project is a Windows Service application designed to connect to Microsoft Teams as well as the TeamSpot USB device. Based on the currect state of MS Teams, this service will set the LED color on the device. This service also receives button press events from the device and reacts accordingly by sending messages to Teams to Mute or Unmute.

This service supports two types of button presses on the device:

- Short button press (1 second or less): This will toggle the mute state in Microsoft Teams. Useful for quickly muting or unmuting during a call.
- Long button press (over 1 second): This will temporarily toggle the mute state only while the button is held down. Useful when you want to temporarily mute yourself to cough, or briefly unmute for a short comment.

## How to Build/Compile

This project is set up to publish to a single executable file which can then be used as a Windows Service. To produce this executable, do one of the following:

- Using Visual Studio, right click on the `TeamSpot.Service` project and select "Publish". The Folder publish profile will be selected by default. Click the "Publish" button to create the output.
- Form a command line, navigate to the `TeamSpot.Service` project folder and run the following command:
  ```
  dotnet publish -p:PublishProfile=FolderProfile
  ```

The executable and accompanying files will be placed in the `TeamSpot.Service\bin\Release\net10.0-windows\win-x64\publish\win-x64` folder of the project. You can then use this output to install the service on your Windows machine.

## How to Install

I haven't created a proper installer for this service. Since this is currently just for me it's simple enough to manually install the executable as a Windows Service. I've simplified the process by including a script. Simply run the `install-service.bat` script included in the build output folder. You may need Administrator privileges to run this script. Within Windows, right-click on the script and select "*Run as Administrator*". This script will install the service and start it automatically. If you wish to uninstall, run the `uninstall-service.bat` script in the same manner.

## LED Color Adjustment

If you wish to change to default LED colors shown on the button for each possible state, edit the color values in the `appsettings.json` file. Colors are represented in RGB values (0-255 for each channel) as well as a brightness value of 0 to 100. The application is set up to auto-update settings if they are changed, so you can edit the file while the service is running and it will pick up the changes automatically and update the LED within a few seconds.

## Development Notes

The windows service itself is composed of a series of C# BackgroundService implementations. The main jobs of the service (listening to a Websocket API, and listening for USB device messages) are asynchronous operations that block until a message comes in. This is a challenge for communicating back and forth between the various internal services. The communication challenge is handled using ChannelReader and ChannelWriter objects to allow these async services to communicate across threads. Admittedly this is a fairly new pattern to me, so I had some help from AI to get it scaffolded out, but after working with it and refactoring it, it's been working well for this project.

Windows service project fundamentals:
- https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service
