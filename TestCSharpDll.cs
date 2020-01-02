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
using System.Web;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using System.Linq;
using System.Xml;
using Microsoft.Owin.Host.HttpListener;
using System.Threading.Tasks;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        #region WebServer Variables
        private int? WebserverPort;
        private WebServer mediaWebServer;
        string mediaContentURL = null;
        #endregion WebServer Variables

        #region GoogleCast Chromecast Variables
        private Sender csSender = null;
        private IMediaChannel mediaChannel = null;
        const string contentType = "audio/flac";
        #endregion GoogleCast Chromecast Variables

        #region Musicbee API Variables
        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();
        #endregion Musicbee API Variables

        #region Misc Variables
        private PictureBox serverIcon, libraryIcon, connectionIcon;
        private bool crossfade;
        string library = null;
        #endregion Misc Variables

        #region Musicbee API Methods

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
            about.ConfigurationPanelHeight = 25;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function

            ToolStripMenuItem mainMenuItem = (ToolStripMenuItem)mbApiInterface.MB_AddMenuItem("mnuTools/MB Chromecast", null, null);

            //TODO
            mainMenuItem.DropDown.Items.Add("Disconnect from Chromecast", null, (sender, e) => DisconnectFromChromecast(sender, e, false));
            mainMenuItem.DropDown.Items.Add("Stop Server", null, StopWebserver);
            mainMenuItem.DropDown.Items.Add("Restart Server", null, null);
            mainMenuItem.DropDown.Items.Add("Stop Plugin", null, UserClosingPlugin);

            ReadSettings();


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
                Panel configPanel = (Panel)Control.FromHandle(panelHandle);
                Button prompt = new Button
                {
                    AutoSize = true,
                    Location = new Point(0, 0),
                    Text = "Settings"
                };
                prompt.Click += ShowSettings;
                configPanel.Controls.AddRange(new Control[] { prompt });
            }
            return false;
        }

        // MusicBee is closing the plugin (plugin is being disabled by user or MusicBee is shutting down)
        public void Close(PluginCloseReason reason)
        {
            PauseIfPlaying();

            StopWebserver();

            //DisconnectFromChromecast here maybe?
            if (csSender != null && csSender.TcpClient != null)
            {
                csSender.Disconnect();
                csSender = null;
            }

            UpdateStatus();
            RevertSettings();

        }

        public void Uninstall()
        {

        }

        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            switch (type)
            {
                case NotificationType.PlayStateChanged:


                    //Play and pause the chromecast from the MB player
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

                        //csSender.GetChannel<IMediaChannel>().SeekAsync(mbApiInterface.Player_GetPosition() / 1000).WaitWithoutException();
                    }

                    break;

                case NotificationType.PluginStartup:
                    break;

                case NotificationType.VolumeLevelChanged:
                    break;

                case NotificationType.TrackChanged:

                    if (!PrerequisitesMet())
                    {
                        return;
                    }


                    //Get the songname and format it into half of the url
                    StringBuilder songName = new StringBuilder(@mbApiInterface.NowPlaying_GetFileUrl());
                    songName.Replace(library, "");
                    songName.Replace(@"\", @"/");


                    try
                    {
                        Task.Run(() => mediaChannel.LoadAsync(
                            new MediaInformation()
                            {
                                ContentType = contentType,
                                ContentId = mediaContentURL + HttpUtility.UrlPathEncode(songName.ToString()), //Where the media is located
                                StreamType = StreamType.Buffered,
                                Metadata = new MusicTrackMediaMetadata
                                {
                                    Artist = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Artist), //Shows the Artist
                                    Title = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.TrackTitle), //Shows the Track Title
                                    AlbumName = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Album),
                                },


                            }));
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }



                    break;
            }
        }

        #endregion Musicbee API Methods

        #region User Saved Settings
        //Read the settings file
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

        //Fired when the user clicks Apply/Save in the preferences panel
        public void SaveSettings()
        {
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            ReadSettings();
        }

        //Show the Settings Form
        private void ShowSettings(object sender, EventArgs e)
        {
            using (var settingsForm = new Settings(mbApiInterface.Setting_GetPersistentStoragePath()))
            {
                settingsForm.ShowDialog();
            }
        }
        #endregion User Saved Settings

        #region MB Chromecast UI Elements
        public int OnDockablePanelCreated(Control panel)
        {
            panel.UIThread(() =>
            {
                //Chromecast icon
                PictureBox chromecastSelect = new PictureBox
                {
                    Location = new Point(100, 0),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    ClientSize = new Size(25, 21),
                    Image = Properties.Resources.chromecast_icon_connect

                };
                chromecastSelect.Click += new EventHandler(OnChromecastSelection);
                panel.Controls.Add(chromecastSelect);

                //icon
                serverIcon = new PictureBox
                {
                    Location = new Point(150, 0),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    ClientSize = new Size(15, 15),
                    Image = Properties.Resources.server_icon

                };
                panel.Controls.Add(serverIcon);

                //icon
                libraryIcon = new PictureBox
                {
                    Location = new Point(175, 0),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    ClientSize = new Size(15, 15),
                    Image = Properties.Resources.library_icon

                };
                panel.Controls.Add(libraryIcon);

                //icon
                connectionIcon = new PictureBox
                {
                    Location = new Point(200, -5),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    ClientSize = new Size(20, 20),
                    Image = Properties.Resources.connected_icon

                };
                panel.Controls.Add(connectionIcon);

                UpdateStatus();

            });


            return 0;
        }

        #endregion MB Chromecast UI Elements

        #region Core Methods
        protected void OnChromecastSelection(object sender, EventArgs e)
        {
            //If the webserver started with no issues
            try
            {
                StartWebserver();
                //Change some musicbee settings
                ChangeSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show("The webserver could not be started. Cancelling");
                return;
            }


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

                PauseIfPlaying();

                //Maybe move this somewhere else?
                csSender.GetChannel<IMediaChannel>().StatusChanged += Synchronize_Reciever;
                csSender.Disconnected += ChromecastDisconnect;

            }
            UpdateStatus();

        }



        //Synchronize changes made directly to the chromecast (i.e by some other remote) to the musicbee player
        private void Synchronize_Reciever(object sender, EventArgs e)
        {

            var obj = (sender as IMediaChannel).Status;
            if (obj == null)
            {
                return;
            }

            var chromecastTime = obj.First().CurrentTime;
            var playerState = obj.First().PlayerState;

            //Reflect changes made in the songs timeline to the musicbee player
            mbApiInterface.Player_SetPosition((int)(chromecastTime * 1000));

            var musicbeePlayerState = mbApiInterface.Player_GetPlayState();

            //Reflect the changes in the play state on the chromecast to the musicbee player
            if (playerState == "PAUSED" && musicbeePlayerState == PlayState.Playing)
            {
                mbApiInterface.Player_PlayPause();
            }
            if (playerState == "PLAYING" && musicbeePlayerState == PlayState.Paused)
            {
                mbApiInterface.Player_PlayPause();
            }
        }
        public void ChromecastDisconnect(object sender, EventArgs e)
        {
            Debug.WriteLine("Disconnected from chromecast");
            StopIfPlaying();
            csSender.Disconnect();
            StopWebserver();
            UpdateStatus();
            RevertSettings();
            MessageBox.Show("Chromecast was Disconnected, Closing all resources");
        }

        #endregion Core Methods

        #region WebServer 
        private int StartWebserver()
        {
            //If theres a web server already running, then theres no need to start a new one
            if (mediaWebServer != null)
            {
                return 1;
            }
            try
            {
                //Create the web server
                mediaWebServer = new WebServer(library, WebserverPort);
                //Save the web server url
                mediaContentURL = "http://" + GetLocalIPAddress() + ":" + WebserverPort;
                return 0;
            }
            catch (Exception e)
            {
                //Catch error here: TODO
                return -1;
            }

        }

        private void StopWebserver(object sender = null, EventArgs e = null)
        {
            try
            {
                mediaWebServer.Stop();
                mediaWebServer = null;
                UpdateStatus();
                MessageBox.Show("Stopped web server successfully");
            }
            catch (NullReferenceException ex)
            {
                //Nothing to do since there was no web server initialized in the first place
            }
        }

        #endregion WebServer

        #region MB Settings
        private void ChangeSettings()
        {
            //Save the users settings
            crossfade = mbApiInterface.Player_GetCrossfade();

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
            mbApiInterface.Player_SetCrossfade(crossfade);
        }

        private void UpdateStatus()
        {
            if (csSender != null && (csSender as Sender).TcpClient != null)
            {
                connectionIcon.Image = Properties.Resources.connect_icon_OK;
            }
            else
            {
                connectionIcon.Image = Properties.Resources.connected_icon;

            }

            if (library != null)
            {
                libraryIcon.Image = Properties.Resources.library_icon_OK;
            }
            else
            {
                libraryIcon.Image = Properties.Resources.library_icon;

            }

            if (mediaWebServer != null)
            {
                serverIcon.Image = Properties.Resources.server_icon_OK;
            }
            else
            {
                serverIcon.Image = Properties.Resources.server_icon;

            }

        }
        #endregion MB Settings

        #region Helper Functions
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

        //The chromecast plugin won't work if these items aren't initialized
        private bool PrerequisitesMet()
        {
            //The csSender must not be null
            //The server must be running
            //The library path must be set 
            return csSender != null && mediaWebServer != null && !string.IsNullOrEmpty(library);
        }

        #endregion Helper Functions

        public void DisconnectFromChromecast(object sender, EventArgs e, bool userCalled)
        {
            try
            {
                PauseIfPlaying();
                //csSender.GetChannel<IReceiverChannel>().StopAsync().WaitWithoutException();
                csSender.Disconnect();
                csSender = null;
                UpdateStatus();
            }
            catch (NullReferenceException ex)
            {
                Debug.WriteLine("");
            }

        }



        public void UserClosingPlugin(object sender, EventArgs e)
        {
            Close(PluginCloseReason.UserDisabled);
        }

        public void PauseIfPlaying()
        {
            if (mbApiInterface.Player_GetPlayState() == PlayState.Playing)
            {
                mbApiInterface.Player_PlayPause();
            }
        }
        public void StopIfPlaying()
        {
            if (mbApiInterface.Player_GetPlayState() == PlayState.Playing)
            {
                mbApiInterface.Player_Stop();
            }
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

}