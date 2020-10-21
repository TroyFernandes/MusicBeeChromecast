using System;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using GoogleCast;
using Nito.AsyncEx.Synchronous;
using GoogleCast.Channels;
using GoogleCast.Models.Media;
using System.Drawing;
using System.Web;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using System.Xml;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Collections.ObjectModel;
using NetFwTypeLib;
using System.Security.Permissions;
using System.Collections.Specialized;
using System.Transactions;
using System.Timers;
using System.Collections;
using System.ComponentModel;
using Nito.AsyncEx;
using System.Reflection;

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
        private IMediaChannel mediaChannel = null;
        #endregion GoogleCast Chromecast Variables

        #region Musicbee API Variables
        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();
        #endregion Musicbee API Variables

        #region Misc Variables

        #endregion Misc Variables

        #region Musicbee API Methods

        System.Timers.Timer aTimer;

        IterableStack<string> myStack;

        SongHash songHash;
        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            mbApiInterface = new MusicBeeApiInterface();
            mbApiInterface.Initialise(apiInterfacePtr);
            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = "MusicBee Chromecast";
            about.Description = "Adds cast functionality to MusicBee";
            about.Author = "Troy Fernandes";
            about.TargetApplication = "";   // current only applies to artwork, lyrics or instant messenger name that appears in the provider drop down selector or target Instant Messenger
            about.Type = PluginType.General;
            about.VersionMajor = 1;  // your plugin version
            about.VersionMinor = 0;
            about.Revision = 1;
            about.MinInterfaceVersion = MinInterfaceVersion;
            about.MinApiRevision = MinApiRevision;
            about.ReceiveNotifications = (ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents);
            about.ConfigurationPanelHeight = 25;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function

            mbApiInterface.MB_RegisterCommand("Chromecast", OnChromecastSelection);


            ToolStripMenuItem mainMenuItem = (ToolStripMenuItem)mbApiInterface.MB_AddMenuItem("mnuTools/MB Chromecast", null, null);

            mainMenuItem.DropDown.Items.Add("Check Status", null, ShowStatusInMessagebox);
            mainMenuItem.DropDown.Items.Add("Disconnect from Chromecast", null, (sender, e) => DisconnectFromChromecast(sender, e, false));

            ReadSettings();

            EmptyDirectory();

            aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = 10000;

            myStack = new IterableStack<string>();

            songHash = new SongHash();

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
            StopChromecast();
            StopWebserver();

            EmptyDirectory().WaitWithoutException();

            RevertSettings();


        }

        public void Uninstall()
        {

        }

        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            if (mediaChannel != null) {

                switch (type)
                {

                    case NotificationType.PlayStateChanged:

                        //Play and pause the chromecast from the MB player
                        if (mediaChannel.Status != null)
                        {
                            switch (mbApiInterface.Player_GetPlayState())
                            {
                                case PlayState.Paused:
                                    mediaChannel.PauseAsync().WaitWithoutException();
                                    break;

                                case PlayState.Playing:
                                    mediaChannel.PlayAsync().WaitWithoutException();
                                    break;
                            }

                        }
                        
                        break;


                    case NotificationType.NowPlayingListChanged:

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

                        aTimer.Enabled = false;
                        aTimer.Enabled = true;

                        CalculateHash(mbApiInterface.NowPlaying_GetFileUrl(), "current").WaitWithoutException();

                        var info = CopySong(sourceFileUrl, songHash.Current).WaitAndUnwrapException();
                        LoadSong(info.Item1, info.Item2);

                        break;
                }



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
            }
        }

        //Fired when the user clicks Apply/Save in the preferences panel
        public void SaveSettings()
        {
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

        #endregion MB Chromecast UI Elements

        #region Core Methods

        protected void OnChromecastSelection(object sender, EventArgs e)
        {
            //If the webserver started with no issues
            try
            {
                //If there's already an active connection
                if (mediaChannel != null)
                {
                    MessageBox.Show("There is already an active connection to a device. Please Disconnect then try again");
                    return;
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show("The webserver could not be started. Cancelling\n Error: " + ex.Message);
                return;
            }


            using (var cs = new ChromecastPanel(
                Color.FromArgb(mbApiInterface.Setting_GetSkinElementColour(SkinElement.SkinInputPanel, ElementState.ElementStateDefault, ElementComponent.ComponentBackground))))
            {
                try
                {
                    cs.StartPosition = FormStartPosition.CenterParent;
                    cs.ShowDialog();


                    mediaChannel = cs.ChromecastMediaChannel;
                    if (mediaChannel == null)
                    {
                        RevertSettings();
                        return;
                    }
    
                    //Change some musicbee settings
                    PauseIfPlaying();
                    ChangeSettings();

                    AttatchChromecastHandlers();

                    //If the webserver started with an issue 
                    if (StartWebserver() == -1)
                    {
                        return;
                    }

                }
                catch (NullReferenceException ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }

        }

        //Synchronize changes made directly to the chromecast (i.e by some other remote) to the musicbee player
        private void Synchronize_Reciever(object sender, EventArgs e)
        {
            if (mediaChannel == null)
            {
                return;
            }
            var obj = mediaChannel.Status;
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
            mediaChannel = null;
            StopIfPlaying();
            StopWebserver();
            RevertSettings();
        }

        public void DisconnectFromChromecast(object sender, EventArgs e, bool userCalled)
        {
            try
            {
                StopChromecast();
            }
            catch (NullReferenceException)
            {
            }

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
                mediaWebServer = new WebServer(WebserverPort);
                //Save the web server url
                mediaContentURL = "http://" + GetLocalIPAddress() + ":" + WebserverPort + "/";
                return 0;
            }
            catch (Exception e)
            {
                MessageBox.Show("Error starting the webserver. \n " + e.Message);
                return -1;
            }

        }

        private void StopWebserver(object sender = null, EventArgs e = null)
        {
            try
            {
                StopChromecast();

                if (mediaWebServer == null)
                {
                    return;
                }
                mediaWebServer.Stop();
                mediaWebServer = null;
            }
            catch (NullReferenceException)
            {
                //Nothing to do since there was no web server initialized in the first place
            }
        }

        #endregion WebServer

        #region MB Settings

        private void ChangeSettings()
        {
            //Settings need to be changed because they might change how the player interacts with chromecast.
            //These settings get reverted back to their original settings after
            mbApiInterface.Player_SetMute(true);

        }

        private void RevertSettings()
        {
            if (mbApiInterface.Player_GetMute())
            {
                mbApiInterface.Player_SetMute(false);
            }
        }

        #endregion MB Settings

        #region Helper Functions

        public bool AttatchChromecastHandlers()
        {
            mediaChannel.StatusChanged += Synchronize_Reciever;
            mediaChannel.Sender.Disconnected += ChromecastDisconnect;
            return true;
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

        private bool PrerequisitesMet()
        {
            //The mediaChannel must not be null
            //The server must be running
            return mediaChannel != null && mediaWebServer != null;
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

        private void ShowStatusInMessagebox(object sender, EventArgs e)
        {
            StringBuilder status = new StringBuilder();
            if (mediaChannel != null)
            {
                status.Append("Chromecast: Connected\n");
            }
            else
            {
                status.Append("Chromecast: Not Connected\n");
            }


            if (mediaWebServer != null)
            {
                status.Append("Server Status: Running @ " + mediaContentURL + "\n");
            }
            else
            {
                status.Append("Server Status: Not Running\n");
            }
            MessageBox.Show(status.ToString());
        }

        #endregion Helper Functions


        public void DeleteFiles(string hashed)
        {
            try
            {
                System.IO.DirectoryInfo di = new DirectoryInfo(@System.IO.Path.GetTempPath() + @"\\MusicBeeChromecast");

                foreach (FileInfo file in di.GetFiles())
                {

                    if (Path.GetFileNameWithoutExtension(file.Name) == hashed)
                    {
                        file.Delete();
                    }
                }

            }
            catch (System.IO.IOException)
            {

            }

        }

        public async Task<Tuple<string, string, string>> CopySong(string songFile, string hashed)
        {

            string songFileExt = Path.GetExtension(songFile);
            File.Copy(songFile, @System.IO.Path.GetTempPath() + @"\\MusicBeeChromecast\" + hashed + songFileExt, true);

            string imageFile = mbApiInterface.Library_GetArtworkUrl(songFile, 0);

            string imageFileExt = ".jpg";

            if (imageFile != null)
            {
                File.Copy(imageFile, @System.IO.Path.GetTempPath() + @"\\MusicBeeChromecast\" + hashed + imageFileExt, true);
            }

            return Tuple.Create(hashed, songFileExt, imageFileExt);
        }


        public async Task LoadSong(string hashed, string songFileExt)
        {
            try {
                await mediaChannel.LoadAsync(
                new MediaInformation()
                {

                    ContentId = HttpUtility.UrlPathEncode(mediaContentURL + hashed + songFileExt),
                    StreamType = StreamType.Buffered,
                    Duration = mbApiInterface.NowPlaying_GetDuration() / 1000,
                    Metadata = new MusicTrackMediaMetadata
                    {
                        Artist = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Artist),
                        Title = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.TrackTitle),
                        AlbumName = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Album),
                        Images = new[] {
                        new GoogleCast.Models.Image
                        {
                            Url = mediaContentURL + hashed + ".jpg"
                        }},
                    },


                });

                myStack.Push(hashed.ToString());

            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Requested to close");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public async Task ProcessNextAndQueue(int index)
        {
            songHash.Current = songHash.Next;
            var nextFileUrl = mbApiInterface.NowPlayingList_GetListFileUrl(index+1);

            await CalculateHash(nextFileUrl, "next");

            string[] res = null;
            mbApiInterface.Library_GetFileTags(nextFileUrl, new[] { MetaDataType.Artist, MetaDataType.TrackTitle, MetaDataType.Album }, ref res);
            await QueueItem(songHash.Next,
                Path.GetExtension(nextFileUrl),
                0,
                res[0],
                res[1],
                res[2]
                );


            myStack.Push(songHash.Next);

            await CopySong(nextFileUrl, songHash.Next);
        }

        public string NextFileURL()
        {
            var nowPlayingIndex = mbApiInterface.NowPlayingList_GetCurrentIndex();

            return mbApiInterface.NowPlayingList_GetListFileUrl(nowPlayingIndex + 1);

        }


        public async Task QueueItem(string hashedName, string songFileExt, int duration, string artist, string title, string album)
        {

            QueueItem[] i = new QueueItem[]
            {
                new QueueItem
                {

                    Media = new MediaInformation()
                    {
                        ContentId = HttpUtility.UrlPathEncode(mediaContentURL + hashedName + songFileExt),
                        StreamType = StreamType.Buffered,
                        Duration = duration / 1000,
                        Metadata = new MusicTrackMediaMetadata
                        {
                            Artist = artist,
                            Title = title,
                            AlbumName = album,
                            Images = new[] {
                            new GoogleCast.Models.Image
                            {
                                Url = mediaContentURL + hashedName + ".jpg"
                            }},
                        }
                    },
                    Autoplay = true,
                 }
            };
            await mediaChannel.QueueInsertAsync(i);

        }


        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            DeleteOld().WaitWithoutException();
            source.GetType().GetProperty("Enabled").SetValue(source, false, null);

        }

        public async Task CalculateHash(string songName, string which)
        {
            switch (which)
            {
                case "previous":
                    songHash.Previous = Math.Abs(songName.GetHashCode()).ToString();
                    return;

                case "current":
                    songHash.Current = Math.Abs(songName.GetHashCode()).ToString();
                    return;

                case "next":
                    songHash.Next = Math.Abs(songName.GetHashCode()).ToString();
                    return;
            }


        }

        public async Task DeleteOld()
        {
            if (myStack.Count() > 1)
            {
                for (int i = 0; i < myStack.Count(); i++)
                {
                    var element = myStack.ElementAt(i);
                    if (element != songHash.Current && element != songHash.Next)
                    {
                        DeleteFiles(element.ToString());
                        myStack.Remove(i);
                    }
                }
            }
        }

        public async Task EmptyDirectory()
        {
            try
            {
                System.IO.DirectoryInfo di = new DirectoryInfo(@System.IO.Path.GetTempPath() + @"\\MusicBeeChromecast");

                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }

            }
            catch (System.IO.IOException)
            {

            }

        }

        private void GetPosition(object source, ElapsedEventArgs e)
        {

        }

        private void DoSomething(object sender, EventArgs e)
        {
            var count = sender.GetType().GetProperty("Count").GetValue(sender, null);
            if ((int)count == 8)
            {
                var index = mbApiInterface.NowPlayingList_GetCurrentIndex();
                ProcessNextAndQueue(index+1).WaitWithoutException();
            }
        }


        public async Task LoadSongs(List<SongInfo> songs)
        {
            try
            {
                MediaInformation[] info = new MediaInformation[songs.Count];

                for (int i = 0; i < info.Count(); i++)
                {
                    string[] res = null;
                    mbApiInterface.Library_GetFileTags(songs[i].FileURL, new[] { MetaDataType.Artist, MetaDataType.TrackTitle, MetaDataType.Album}, ref res);
                    info[i] = new MediaInformation
                    {
                        ContentId = HttpUtility.UrlPathEncode(mediaContentURL + songs[i].Hashed + songs[i].SongFileExt),
                        StreamType = StreamType.Buffered,
                        Duration = mbApiInterface.NowPlaying_GetDuration() / 1000,
                        Metadata = new MusicTrackMediaMetadata
                        {
                            Artist = res[0],
                            Title = res[1],
                            AlbumName = res[2],
                            Images = new[] {
                            new GoogleCast.Models.Image
                            {
                                Url = mediaContentURL + songs[i].Hashed + ".jpg"
                            }},

                        }
                    };
                    myStack.Push(songs[i].Hashed.ToString());
                }


                await mediaChannel.QueueLoadAsync(GoogleCast.Models.Media.RepeatMode.RepeatOff, info);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Requested to close");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public void StopChromecast()
        {
            if (mediaChannel != null)
            {
                mediaChannel.StopAsync();
                mediaChannel = null;
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

    public class SongHash {
        public string Previous { get; set; }
        public string Current { get; set; }
        public string Next { get; set; }
    }


    public class IterableStack<T> : IEnumerable<string>
    {
        private List<T> items = new List<T>();

        #region Enumerator

        private IEnumerable<string> GetValues()
        {
            foreach (var s in items)
            {
                yield return s.ToString();
            }
        }

        #endregion Enumerator

        #region IEnumerable implementation

        public IEnumerator<string> GetEnumerator()
        {
            return GetValues().GetEnumerator();
        }

        #endregion

        #region IEnumerable implementation

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion


        public void Push(T item)
        {
            items.Add(item);
        }
        public T Pop()
        {
            if (items.Count > 0)
            {
                T temp = items[items.Count - 1];
                items.RemoveAt(items.Count - 1);
                return temp;
            }
            else
                return default(T);
        }
        public void Remove(int itemAtPosition)
        {
            items.RemoveAt(itemAtPosition);
        }

        public int Count()
        {
            return items.Count;
        }
    }

    public class Listener
    {
        public event EventHandler CountChanged;

        public int Duration { get; set; }
        public System.Timers.Timer Timer { get; set; }

        public int Count { get; set; }

        public Listener(int Duration, ElapsedEventHandler handler)
        {
            Timer = new System.Timers.Timer();
            Timer.Elapsed += new ElapsedEventHandler(handler);

            Timer.Elapsed += new ElapsedEventHandler(Increment);
            Timer.Interval = Duration / 10;
            Timer.Enabled = true;
            Count = 1;
        }

        public void Enable()
        {
            Timer.Enabled = false;
            Timer.Enabled = true;
        }

        public void Disable()
        {
            Timer.Enabled = false;
        }

        private void Increment(object source, ElapsedEventArgs e)
        {
            Count++;
            if (CountChanged != null)
            {
                CountChanged(this, null);
            }
        }

    }

    public class SongInfo 
    { 
        public string Hashed { get; set; }
        public string SongFileExt { get; set; }
        public string FileURL { get; set; }
    }

}