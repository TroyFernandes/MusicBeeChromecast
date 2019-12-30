using System;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using GoogleCast;
using Nito.AsyncEx.Synchronous;
using GoogleCast.Channels;
using GoogleCast.Models.Media;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms.VisualStyles;
using System.Web;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using System.Linq;
using System.Xml;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private int? WebserverPort = 23614;
        private WebServer mediaWebServer;
        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();
        private Stack queue = new Stack();
        private Sender csSender = null;
        private IMediaChannel mediaChannel = null;

        private Tuple<int, int> playerLengthWidth { get; set; }

        string mediaContentURL = null;

        const string contentType = "audio/mp3";
        string library = null;


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
            about.ConfigurationPanelHeight = 100;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function

            mbApiInterface.MB_AddMenuItem("mnuTools/Chromecast Settings", "", ShowSettings);

            ReadSettings();


            return about;
        }

        private void ShowSettings(object sender, EventArgs e)
        {
            using (var settingsForm = new Settings(mbApiInterface.Setting_GetPersistentStoragePath()))
            {
                settingsForm.ShowDialog();
            }
        }

        private bool CheckPrerequisites()
        {
            //The csSender must not be null
            //The server must be running
            //The library path must be set 
            var sender = csSender;
            var webserv = mediaWebServer;
            var lib = library;
            return csSender != null && mediaWebServer != null && !string.IsNullOrEmpty(library);
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
            }
            return false;
        }

        private void ReadSettings()
        {
            var fullFilePath = @mbApiInterface.Setting_GetPersistentStoragePath() + @"\MB_Chromecast_Settings.xml";
            if (File.Exists(fullFilePath))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(fullFilePath);
                var temp = doc.GetElementsByTagName("server_port")[0].InnerText;
                if (!string.IsNullOrEmpty(temp))
                {
                    WebserverPort = Convert.ToInt16(temp);
                }
                library = doc.GetElementsByTagName("library_path")[0].InnerText;
            }
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
            StopWebserver();

            //Disconnect here maybe?
            if (csSender != null)
            {
                csSender.Disconnect();
            }

            RevertSettings();

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
                case NotificationType.PlayStateChanged:

                    if (csSender != null)
                    {
                        switch (mbApiInterface.Player_GetPlayState())
                        {
                            case PlayState.Paused:
                                csSender.GetChannel<IMediaChannel>().PauseAsync().WaitWithoutException();
                                break;

                            case PlayState.Playing:
                                csSender.GetChannel<IMediaChannel>().PlayAsync().WaitWithoutException();
                                break;
                        }
                    }


                    break;

                case NotificationType.PluginStartup:

                    switch (mbApiInterface.Player_GetPlayState())
                    {
                        case PlayState.Playing:
                        case PlayState.Paused:
                            break;
                    }
                    break;

                case NotificationType.VolumeLevelChanged:


                    break;

                case NotificationType.TrackChanged:

                    if (!CheckPrerequisites())
                    {
                        break;
                    }

                    StringBuilder songName = new StringBuilder(@mbApiInterface.NowPlaying_GetFileUrl());
                    songName.Replace(library, "");
                    songName.Replace(@"\", @"/");

                    GenericMediaMetadata metadata = new GenericMediaMetadata
                    {
                        Subtitle = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Artist),
                        Title = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.TrackTitle),
                    };

                    try
                    {
                        var mediaStatus = mediaChannel.LoadAsync(
                            new MediaInformation()
                            {
                                ContentId = mediaContentURL + HttpUtility.UrlPathEncode(songName.ToString()),
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



        #region DockablePanel
        //  presence of this function indicates to MusicBee that this plugin has a dockable panel. MusicBee will create the control and pass it as the panel parameter
        //  you can add your own controls to the panel if needed
        //  you can control the scrollable area of the panel using the mbApiInterface.MB_SetPanelScrollableArea function
        //  to set a MusicBee header for the panel, set about.TargetApplication in the Initialise function above to the panel header text
        public int OnDockablePanelCreated(Control panel)
        {


            //    return the height of the panel and perform any initialisation here
            //    MusicBee will call panel.Dispose() when the user removes this panel from the layout configuration
            //    < 0 indicates to MusicBee this control is resizable and should be sized to fill the panel it is docked to in MusicBee
            //    = 0 indicates to MusicBee this control resizeable
            //    > 0 indicates to MusicBee the fixed height for the control.Note it is recommended you scale the height for high DPI screens(create a graphics object and get the DpiY value)
            panel.UIThread(() =>
            {
                //Chromecast icon
                PictureBox chromecastSelect = new PictureBox
                {
                    Location = new Point(200, 0),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    ClientSize = new Size(25, 21),
                    Image = Properties.Resources.chromecast_icon_connect

                };
                chromecastSelect.Click += new EventHandler(OnChromecastSelection);
                panel.Controls.Add(chromecastSelect);

                TrackbarEx trackbar = new TrackbarEx
                {

                    Capture = true,
                    AutoSize = false,
                    Width = 100,
                    Minimum = 0,
                    Maximum = 100,
                    TickStyle = TickStyle.None,
                    Location = new Point(0, 0)
                };

                //Get the player volume
                //csSender.GetChannel<IReceiverChannel>().Status;
                trackbar.ValueChanged += new EventHandler(trackbar1_ValueChanged);

                panel.Controls.Add(trackbar);


            });

            return 0;
        }

        #endregion


        protected void OnChromecastSelection(object sender, EventArgs e)
        {
            using (var cs = new ChromecastPanel(
                Color.FromArgb(mbApiInterface.Setting_GetSkinElementColour(SkinElement.SkinInputPanel, ElementState.ElementStateDefault, ElementComponent.ComponentBackground))))
            {
                cs.StartPosition = FormStartPosition.CenterParent;


                cs.ShowDialog();

                mediaChannel = cs.ChromecastMediaChannel;
                csSender = cs.ChromecastSender;
                if (csSender == null)
                {
                    return;
                }
                if (StartWebserver() != -1)
                {
                    //Success
                    ChangeSettings();


                }
                else
                {
                    //Error
                }

            }

        }

        private IEnumerable<Component> EnumerateComponents()
        {
            return from field in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                   where typeof(Component).IsAssignableFrom(field.FieldType)
                   let component = (Component)field.GetValue(this)
                   where component != null
                   select component;
        }


        private async void trackbar1_ValueChanged(object sender, EventArgs e)
        {
            if (csSender != null)
            {
                try
                {

                    await csSender.GetChannel<IReceiverChannel>().SetVolumeAsync((float)(sender as TrackbarEx).Value / 100);

                }
                catch (Exception e2)
                {

                }

            }
        }

        private void ChangeSettings()
        {
            //TODO: maybe create an array, initialized on startup, then just flip each bool value

            //Settings need to be changed because they might change how the player interacts with chromecast.
            //These settings get reverted back to their original settings after
            mbApiInterface.Player_SetMute(true);
            mbApiInterface.Player_SetCrossfade(false);


        }

        private void RevertSettings()
        {
            if (mbApiInterface.Player_GetMute())
            {
                mbApiInterface.Player_SetMute(false);
            }
        }

        private int StartWebserver()
        {
            if (mediaWebServer != null)
            {
                return 1;
            }
            try
            {


                mediaWebServer = new WebServer(library, Path.GetDirectoryName(mbApiInterface.NowPlaying_GetArtworkUrl()));
                mediaContentURL = "http://" + GetLocalIPAddress() + ":" + WebserverPort;

                return 0;
            }
            catch (Exception e)
            {
                //Catch error here: TODO
                return -1;
            }

        }

        private void StopWebserver()
        {
            try
            {
                mediaWebServer.Stop();
            }
            catch (Exception e)
            {
                //TODO
            }
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }


    }

    public static class ControlExtensions
    {
        public static void UIThread(this Control @this, Action code)
        {
            if (null != @this && (!@this.Disposing || !@this.IsDisposed))
            {
                if (@this.InvokeRequired)
                {
                    @this.BeginInvoke(code);
                }
                else
                {
                    code.Invoke();
                }
            }
        }
    }

    #region Chromecast Volume Trackbar
    internal partial class TrackbarEx : TrackBar
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public extern static int SendMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        private static int MakeParam(int loWord, int hiWord)
        {
            return (hiWord << 16) | (loWord & 0xffff);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            SendMessage(this.Handle, 0x0128, MakeParam(1, 0x1), 0);
        }



    }

    #endregion
}