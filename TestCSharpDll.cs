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

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private int? WebserverPort;
        private WebServer mediaWebServer;
        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();
        private Stack queue = new Stack();
        private Sender csSender = null;
        private IMediaChannel mediaChannel = null;

        private PictureBox serverIcon, libraryIcon, connectionIcon;

        private bool crossfade;


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
            about.ConfigurationPanelHeight = 25;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function

            ToolStripMenuItem mainMenuItem = (ToolStripMenuItem)mbApiInterface.MB_AddMenuItem("mnuTools/MB Chromecast", null, null);
            mainMenuItem.DropDown.Items.Add("Restart Server", null, null);
            mainMenuItem.DropDown.Items.Add("Stop Plugin", null, null);

            ReadSettings();


            return about;
        }

        //Show the Settings Form
        private void ShowSettings(object sender, EventArgs e)
        {
            using (var settingsForm = new Settings(mbApiInterface.Setting_GetPersistentStoragePath()))
            {
                settingsForm.ShowDialog();
            }
        }

        //Synchronize changes made directly to the chromecast (i.e by some other remote) to the musicbee player
        private void Synchronize_Reciever(object sender, EventArgs e)
        {
            var obj = (sender as IMediaChannel).Status.First();
            if (obj == null)
            {
                return;
            }
            var chromecastTime = obj.CurrentTime;
            var playerState = obj.PlayerState;

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

        private void UpdateStatus()
        {
            if (csSender != null)
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



        //The chromecast plugin won't work if these items aren't initialized
        private bool PrerequisitesMet()
        {
            //The csSender must not be null
            //The server must be running
            //The library path must be set 
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

        public void SaveSettings()
        {
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            ReadSettings();
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
                        break;
                    }


                    //Get the songname and format it into half of the url
                    StringBuilder songName = new StringBuilder(@mbApiInterface.NowPlaying_GetFileUrl());
                    songName.Replace(library, "");
                    songName.Replace(@"\", @"/");


                    try
                    {

                        var mediaStatus = mediaChannel.LoadAsync(
                            new MediaInformation()
                            {
                                ContentId = mediaContentURL + HttpUtility.UrlPathEncode(songName.ToString()), //Where the media is located
                                StreamType = StreamType.Buffered,
                                Metadata = new GenericMediaMetadata
                                {
                                    Subtitle = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Artist), //Shows the Artist
                                    Title = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.TrackTitle), //Shows the Track Title
                                }
                            }).WaitAndUnwrapException();


                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }


                    break;
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
                trackbar.ValueChanged += new EventHandler(trackbar1_ValueChanged);

                panel.Controls.Add(trackbar);

                UpdateStatus();

            });


            return 0;
        }





        #endregion


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

                //Maybe move this somewhere else
                csSender.GetChannel<IMediaChannel>().StatusChanged += Synchronize_Reciever;

            }
            UpdateStatus();

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

        private void StopWebserver()
        {
            try
            {
                mediaWebServer.Stop();
            }
            catch (NullReferenceException e)
            {
                //Nothing to do since there was no web server initialized in the first place
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