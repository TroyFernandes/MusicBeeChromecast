# MusicBeeChromecast
Adds Cast functionality to MusicBee

# Installation

1. Go to releases and download the zipped file
2. Extract the zip and copy the contents (the two .dll files) to your musicbee plugins folder

# Setup
**PLEASE READ CAREFULLY**

For this plugin to work correctly, there are few key things that need to be followed:

First: you need to properly set your music library path. You MUST set it the same way you did in the musicbee setup. For example, if I go to File -> Library -> Add Library, I see that my library is "E:\Users\Troy\Music". When I set up the plugin, I have to set the same exact path.

Second: You need to enter this command for the webserver to run properly:

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
