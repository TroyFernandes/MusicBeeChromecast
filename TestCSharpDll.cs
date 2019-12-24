using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Drawing;
using System.Windows.Forms;
using GoogleCast;
using Nito.AsyncEx.Synchronous;
using GoogleCast;
using GoogleCast.Channels;
using GoogleCast.Models;
using GoogleCast.Models.Media;
using System.Collections;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();
        private Stack queue = new Stack();
        private Sender sender;
        private IMediaChannel mediaChannel;
        IReceiver device = null;


        const string host = "192.168.1.47"; // ChromeCast host
        const string contentUrl = "http://192.168.1.232:8080";
        const string contentType = "audio/mp3";
        const string library = @"E:\Users\Troy\Music";


        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            mbApiInterface = new MusicBeeApiInterface();
            mbApiInterface.Initialise(apiInterfacePtr);
            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = "Chromecast";
            about.Description = "A brief description of what this plugin does";
            about.Author = "Troy";
            about.TargetApplication = "";   // current only applies to artwork, lyrics or instant messenger name that appears in the provider drop down selector or target Instant Messenger
            about.Type = PluginType.General;
            about.VersionMajor = 1;  // your plugin version
            about.VersionMinor = 0;
            about.Revision = 1;
            about.MinInterfaceVersion = MinInterfaceVersion;
            about.MinApiRevision = MinApiRevision;
            about.ReceiveNotifications = (ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents);
            about.ConfigurationPanelHeight = 0;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function


            //client = new Player(host);
            //client.Connect();
            //MessageBox.Show("Connection Successful");


            return about;
        }

        public bool Configure(IntPtr panelHandle)
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            // panelHandle will only be set if you set about.ConfigurationPanelHeight to a non-zero value
            // keep in mind the panel width is scaled according to the font the user has selected
            // if about.ConfigurationPanelHeight is set to 0, you can display your own popup window
            if (panelHandle != IntPtr.Zero)
            {
                //Panel configPanel = (Panel)Panel.FromHandle(panelHandle);
                //Label prompt = new Label
                //{
                //    AutoSize = true,
                //    Location = new Point(0, 0),
                //    Text = "Hue Settings:"
                //};
                //configPanel.Controls.AddRange(new Control[] { prompt });
            }
            return false;
        }

        // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
        // its up to you to figure out whether anything has changed and needs updating
        public void SaveSettings()
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
        }

        // MusicBee is closing the plugin (plugin is being disabled by user or MusicBee is shutting down)
        public void Close(PluginCloseReason reason)
        {
            try
            {


            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);

            }

        }

        // uninstall this plugin - clean up any persisted files
        public void Uninstall()
        {
        }

        // receive event notifications from MusicBee
        // you need to set about.ReceiveNotificationFlags = PlayerEvents to receive all notifications, and not just the startup event
        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            // perform some action depending on the notification type
            switch (type)
            {
                case NotificationType.PluginStartup:
                    // perform startup initialisation
                    mbApiInterface.MB_RegisterCommand("Testing: TESTING", DoNothingAsync);

                    IEnumerable<IReceiver> receiver = new DeviceLocator().FindReceiversAsync().WaitAndUnwrapException();


                    foreach (var x in receiver)
                    {
                        if (x.FriendlyName == "PC")
                        {
                            device = x;
                        }
                    }

                    sender = new Sender();

                    //Connect to the device
                    sender.ConnectAsync(device).WaitAndUnwrapException();


                    //Launch the default media receiver application
                    mediaChannel = sender.GetChannel<IMediaChannel>();
                    sender.LaunchAsync(mediaChannel).WaitAndUnwrapException();



                    switch (mbApiInterface.Player_GetPlayState())
                    {
                        case PlayState.Playing:
                        case PlayState.Paused:
                            // ...
                            break;
                    }
                    break;
                case NotificationType.TrackChanged:


                    string artist = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Artist);


                    string songName = @mbApiInterface.NowPlaying_GetFileUrl().Replace(library, "");
                    songName = songName.Replace(@"\", @"/");

                    string combined = contentUrl + songName;

                    string directory = mbApiInterface.NowPlaying_GetArtworkUrl();

                    string artwork = Path.GetFileName(directory);

                    string artUrl = "http://192.168.1.232:8080/" + artwork;


                    GenericMediaMetadata metadata = new GenericMediaMetadata
                    {

                        Subtitle = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Artist),
                        Title = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.TrackTitle),
                        Images = new[] {
                            new GoogleCast.Models.Image
                            {
                                Url = artUrl
                            }},

                    };


                    try
                    {
                        var mediaStatus = mediaChannel.LoadAsync(
                            new MediaInformation()
                            {
                                ContentId = combined,
                                StreamType = StreamType.Buffered,
                                Metadata = metadata

                            }).WaitAndUnwrapException();


                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }


                    break;
            }
        }

        // return an array of lyric or artwork provider names this plugin supports
        // the providers will be iterated through one by one and passed to the RetrieveLyrics/ RetrieveArtwork function in order set by the user in the MusicBee Tags(2) preferences screen until a match is found
        public string[] GetProviders()
        {
            return null;
        }

        // return lyrics for the requested artist/title from the requested provider
        // only required if PluginType = LyricsRetrieval
        // return null if no lyrics are found
        public string RetrieveLyrics(string sourceFileUrl, string artist, string trackTitle, string album, bool synchronisedPreferred, string provider)
        {
            return null;
        }

        // return Base64 string representation of the artwork binary data from the requested provider
        // only required if PluginType = ArtworkRetrieval
        // return null if no artwork is found
        public string RetrieveArtwork(string sourceFileUrl, string albumArtist, string album, string provider)
        {
            //Return Convert.ToBase64String(artworkBinaryData)
            return null;
        }

        public void HandleDoNothing()
        {
        }

        public void DoNothingAsync(object sender, EventArgs e)
        {

            Window window = new Window();

            // This is your color to convert from
            System.Drawing.Color color = Color.FromArgb(mbApiInterface.Setting_GetSkinElementColour(SkinElement.SkinInputPanel, ElementState.ElementStateDefault, ElementComponent.ComponentBackground)); ;
            System.Windows.Media.Color newColor = System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
            var brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
            window.Background = brush;


            window.Show();


            //var receiver = (await new DeviceLocator().FindReceiversAsync());

            //List<IReceiver> newList = receiver.ToList();
            string devices = "Test\n";
            //foreach (IReceiver x in newList)
            //{
            //    devices += x.FriendlyName + " - " + x.IPEndPoint + "\n";
            //}



        }


        public void Queue()
        {

        }


    }


}