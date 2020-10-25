# MusicBeeChromecast
Adds Cast functionality to MusicBee

**Disclaimer:** I originally didn't intend to release this plugin as I won't have more time to work on it other than bare functionality, however maybe someone else will find this useful so here I am. As a result, this plugin is here "AS-IS". That being said, the plugin is fairly easy to follow along, so feel free to fork this repo and make changes if you like. Just make sure you credit the people who made the packages, as well as me.

# What it Can Do

The initial reason for creating this plugin was to use the features of MusicBee (primarily auto DJ) to act as a hub and send the currently playing song to a chromecast device. For this reason, the musicbee interface when connected to the chromecast acts as a partial remote, and dosen't include all the things you would expect. 

### This plugin CANNOT:
1. Control the volume on the chromecast
2. Reflect changes made in the musicbee interfaces' timeline to the chromecast (i.e no scrubbing) 

I originally had these two features implemented, however I was unhappy in how it was implemented as it both looked ugly and I felt didn't perform well enough.

### The plugin CAN: 

1. play and pause the chromecast from the musicbee interface
2. Play the full quality from your music files (e.g FLAC).

### What you can do from the chromecast:
If you use a remote such as the Google Home App, you can:
1. Scrub on the timeline and changes will be reflected in musicbee
2. Play and pause
3. Disconnect the player

# Installation

1. Go to releases and download the zipped file (MB_Chromecast.zip)
2. Extract the zip to somewhere temporary (i.e Desktop)
3. Copy the two .dll files found in Plugins in the extracted folder to your musicbee plugins folder
4. Open a command prompt window and navigate to the MBCCRules folder in the extracted zip
5. Run the .exe from the command prompt passing in the port you wish to use eg. ``MBCCRules.exe 8080``

The ``MBCCRules.exe`` requires admin privileges, creates an inbound firewall rule for you local network and runs the following command: ``netsh http add urlacl url=http://*:[PORT]/ user=Everyone`` which allows the chromecast to connect to your PC by it's IP

# Setup
**PLEASE READ CAREFULLY**

1. Go to Edit -> Edit Preferences -> Plugins -> Click the "Settings" button under Musicbee Chromecast
2. Enter a web server port. Use the same port you used for the ``MBCCRules.exe`` NOTE: you must choose a port between 1025-65535 (I recommend 8080)
4. Click save, and restart musicbee
5. Right-click the toolbar (or on the "Arrange Panels") icon and click "Configure Toolbar"
6. Add a new Toolbar button with an icon of your choosing, or simply type in "Chromecast" for example. Under the "Command" dropdown, choose "Chromecast". Then click update.

# How to Use
The plugin will find all available devices/speaker groups you've created. To use the plugin, click the Toolbar Icon/text you created in the previous steps, a window with all the available devices will pop up. Simply choose one you want to connect to and it should connect successfully. You can go to Tools -> MB Chromecast -> Check status, to see if everything is running. 

# Troubleshooting

1. You can go to Tools -> MB Chromecast -> Check status to check the status of the web server and the chromecast connection
2. If you hear the connection sound, but no music is playing, you most likely have an issue with your port forwarding on the webserver. A good tip to try is after connecting to a chromecast, go on your phone and enter in a browser the 
``<machines IP>:<Port#>`` e.g ``192.168.1.27:8080``. (You can also find this address under Check Status) If you're able to see the files, then the chromecast will be able to as well
3. If you get an error when trying to save settings. ``Navigate to C:\Users\<YourName>\Appdata\Roaming\MusicBee\MB_Chromecast_Settings.xml`` and delete the file. 

# Libraries Used

- [GoogleCast](https://github.com/kakone/GoogleCast/tree/master/GoogleCast) + [DayCast](https://github.com/MathieuCyr/DayCast) + [CastIt](https://github.com/Wolfteam/CastIt), with [hig-dev](https://github.com/hig-dev) for his fork 
- [Nito.AsyncEx](https://www.nuget.org/packages/Nito.AsyncEx)
- [Microsoft.Owin](https://www.nuget.org/packages/Microsoft.Owin/)
