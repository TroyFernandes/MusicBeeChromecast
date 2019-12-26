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

namespace MusicBeePlugin
{
    public partial class Plugin
    {


        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();
        private Stack queue = new Stack();
        private Sender csSender = null;
        private IMediaChannel mediaChannel = null;

        private string activeAudioDevice = null;

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
            //Disconnect here maybe?



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


                    switch (mbApiInterface.Player_GetPlayState())
                    {
                        case PlayState.Playing:
                        case PlayState.Paused:

                            // ...
                            break;
                    }
                    break;

                case NotificationType.VolumeLevelChanged:
                    if (csSender == null)
                    {
                        break;
                    }
                    try
                    {

                        csSender.GetChannel<IReceiverChannel>().SetVolumeAsync(mbApiInterface.Player_GetVolume()).WaitAndUnwrapException();
                    }
                    catch (Exception e)
                    {

                    }

                    break;

                case NotificationType.TrackChanged:

                    if (mediaChannel == null)
                    {
                        break;
                    }

                    //mbApiInterface.Player_PlayPause();
                    mbApiInterface.Player_SetOutputDevice("");

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

            //Window window = new Window();
            //// This is your color to convert from
            //System.Drawing.Color color = Color.FromArgb(mbApiInterface.Setting_GetSkinElementColour(SkinElement.SkinInputPanel, ElementState.ElementStateDefault, ElementComponent.ComponentBackground)); ;
            //System.Windows.Media.Color newColor = System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
            //var brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
            //window.Background = brush;
            //window.Show();

            using (var cs = new ChromecastSelction())
            {
                cs.StartPosition = FormStartPosition.CenterParent;

                cs.ShowDialog();

                mediaChannel = cs.ChromecastMediaChannel;
                csSender = cs.ChromecastSender;

            }



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
                //Previous button
                PictureBox previous = new PictureBox
                {
                    Location = new Point(2, 0),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    ClientSize = new Size(25, 25),
                    Image = Properties.Resources.back

                };
                panel.Controls.Add(previous);

                //Play button
                PictureBox play = new PictureBox
                {
                    Location = new Point(29, 0),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    ClientSize = new Size(25, 25),
                    Image = Properties.Resources.play

                };

                panel.Controls.Add(play);

                //Next song button
                PictureBox next = new PictureBox
                {
                    Location = new Point(59, 0),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    ClientSize = new Size(25, 25),
                    Image = Properties.Resources.forward

                };
                panel.Controls.Add(next);

                //Chromecast icon
                PictureBox chromecastSelect = new PictureBox
                {
                    Location = new Point(200, 0),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    ClientSize = new Size(25, 21),
                    Image = Properties.Resources.chromecast_icon_connect

                };
                panel.Controls.Add(chromecastSelect);

                TrackbarEx trackbar = new TrackbarEx
                {
                    AutoSize = false,
                    Width = 100,
                    Minimum = 0,
                    Maximum = 100,
                    TickStyle = TickStyle.None,
                    Location = new Point(86, 0)
                };

                panel.Controls.Add(trackbar);


            });

            return 0;
        }

        // presence of this function indicates to MusicBee that the dockable panel created above will show menu items when the panel header is clicked
        // return the list of ToolStripMenuItems that will be displayed
        public List<ToolStripItem> GetHeaderMenuItems()
        {
            List<ToolStripItem> list = new List<ToolStripItem>();
            list.Add(new ToolStripMenuItem("A menu item"));
            return list;
        }

        private void panel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Red);
            //TextRenderer.DrawText(e.Graphics, "hello", SystemFonts.CaptionFont, new Point(10, 10), Color.Blue);
        }
        #endregion





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