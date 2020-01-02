# MusicBeeChromecast
Adds Cast functionality to MusicBee

**Disclaimer:** I originally didn't intend to release this plugin as I won't have more time to work on it other than bare functionality, however maybe someone else will find this useful so here I am. As a result, this plugin is here "AS-IS". That being said, the plugin is fairly easy to follow along, so feel free to fork this repo and make changes if you like. Just make sure you credit the people who made the packages, as well as me.

# What it Can Do

The initial reason for creating this plugin was to use the features of MusicBee (primarily auto DJ) to act as a hub and send the currently playing song to a chromecast device. For this reason, the musicbee interface when connected to the chromecast acts as a partial remote, and dosen't include all the things you would expect. 

If casting to a display enabled chromecast (e.g a CC hooked up to a TV), there won't be any pretty things to look at. Theres no album art, no fancy name (It displays "Default Reciever Application"), and no duration. What will be there however is the Album name, artist name, and song name. 

### This plugin CANNOT:
1. Control the volume on the chromecast
2. Reflect changes made in the musicbee interfaces' timeline to the chromecast (i.e no scrubbing) 

I originally had these two features implemented, however I was unhappy in how it was implemented as it both looked ugly and I felt didn't perform well enough.

### The plugin CAN: 

play and pause from the musicbee interface

### What you can do from the chromecast:
If you use a remote such as the Google Home App, you can:
1. Scrub on the timeline and changes will be reflected in musicbee
2. Play and pause
3. Disconnect the player

# Installation

1. Go to releases and download the zipped file
2. Extract the zip and copy the contents (the two .dll files) to your musicbee plugins folder

# Setup
**PLEASE READ CAREFULLY**

For this plugin to work correctly, there are few key things that need to be followed:

First: you need to properly set your music library path. You MUST set it the same way you did in the musicbee setup. For example, if I go to File -> Library -> Add Library, I see that my library is "E:\Users\Troy\Music". When I set up the plugin, I have to set the same exact path.

A byproduct of this is that for the plugin to work, all your music must be in a single top level directory and not on different drives for example. The plugin will partially work, but only play the songs which are under the directory you chose.

Second: You need to enter this command in command prompt (admin) for the webserver to run properly:

``netsh http add urlacl url=http://*:8080/ user=Everyone``

 What this command does is allow the webserver to run on an address containing that machines IP address. e.g ``http://192.168.1.27:8080``. Without it, the only address the webserver can listen on is ``http://localhost:8080``, which when sent to the chromecast, it won't be able to resolve the address.
 
Third: You need to port forward whichever port # you choose in step 2 below. As a troubleshooting test, you should be able to hop onto your phone and type in ``<machine IP>:<port #>`` when the webserver is running, and be able to view the webpage.

1. Go to Edit -> Edit Preferences -> Plugins -> Click the "Settings" button under Musicbee Chromecast
2. Enter a web server port (I recommend 8080)
3. Click the "Browse" Button to browse your musicbee library, and browse to the folder which contains your music
4. Click save, and restart musicbee
5. Righclick the toolbar (or on the "Arrange Panels") icon and click "Configure Toolbar"
6. Add a new Toolbar button with an icon of your choosing, or simply type in "Chromecast" for example. Under the "Command" dropdown, choose "Chromecast". Then click update.

# How to Use
The plugin will find all available devices/speaker groups you've created. To use the plugin, click the Toolbar Icon/text you created in the previous steps, a window with all the available devices will pop up. Simply choose one you want to connect to and it should connect successfully. You can go to Tools -> MB Chromecast -> Check status, to see if everything is running. 

# Features
