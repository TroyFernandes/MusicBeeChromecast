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
using System.Net.NetworkInformation;
using FlacLibSharp;

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

        System.Timers.Timer fileDeletionTimer;
        System.Timers.Timer progressTimer;
        IterableStack<string> filenameStack;
        SongHash songHash;
        bool natural = false;

        #endregion Misc Variables

        #region Musicbee API Methods

        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            mbApiInterface = new MusicBeeApiInterface();
            mbApiInterface.Initialise(apiInterfacePtr);
            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = "MusicBee Chromecast";
            about.Description = "Adds casting functionality to MusicBee";
            about.Author = "Troy Fernandes";
            about.TargetApplication = "";   // current only applies to artwork, lyrics or instant messenger name that appears in the provider drop down selector or target Instant Messenger
            about.Type = PluginType.General;
            about.VersionMajor = 2;  // your plugin version
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

            _ = EmptyDirectory();

            fileDeletionTimer = new System.Timers.Timer();
            fileDeletionTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            fileDeletionTimer.Interval = 10000;

            filenameStack = new IterableStack<string>();

            songHash = new SongHash();

            progressTimer = new System.Timers.Timer();
            progressTimer.Elapsed += new ElapsedEventHandler(DoSomething);

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
                        //natural = false;
                        //progressTimer.Enabled = false;
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

                        fileDeletionTimer.Enabled = false;
                        fileDeletionTimer.Enabled = true;

                        CalculateHash(mbApiInterface.NowPlaying_GetFileUrl(), "current").WaitWithoutException();

                        var info = CopySong(sourceFileUrl, songHash.Current).WaitAndUnwrapException();
                        _ = LoadSong(info.Item1, info.Item2);

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

        private void CCNext(object sender, EventArgs e)
        {
            mbApiInterface.Player_PlayNextTrack();
        }

        private void CCPrevious(object sender, EventArgs e)
        {
            mbApiInterface.Player_PlayPreviousTrack();
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
                mediaContentURL = "http://" + GetLocalIP() + ":" + WebserverPort + "/";
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
            mediaChannel.NextRequested += CCNext;
            mediaChannel.PreviousRequested += CCPrevious;
            return true;
        }

        public string GetLocalIP()
        {
            UnicastIPAddressInformation mostSuitableIp = null;

            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var network in networkInterfaces)
            {
                if (network.OperationalStatus != OperationalStatus.Up)
                    continue;

                var properties = network.GetIPProperties();

                if (properties.GatewayAddresses.Count == 0)
                    continue;

                foreach (var address in properties.UnicastAddresses)
                {
                    if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    if (IPAddress.IsLoopback(address.Address))
                        continue;

                    if (!address.IsDnsEligible)
                    {
                        if (mostSuitableIp == null)
                            mostSuitableIp = address;
                        continue;
                    }

                    // The best IP is the IP got from DHCP server
                    if (address.PrefixOrigin != PrefixOrigin.Dhcp)
                    {
                        if (mostSuitableIp == null || !mostSuitableIp.IsDnsEligible)
                            mostSuitableIp = address;
                        continue;
                    }

                    return address.Address.ToString();
                }
            }

            return mostSuitableIp != null
                ? mostSuitableIp.Address.ToString()
                : "";
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
            string filetype = mbApiInterface.NowPlaying_GetFileProperty(FilePropertyType.Kind).Replace(" audio file", "");
            string samplerate = mbApiInterface.NowPlaying_GetFileProperty(FilePropertyType.SampleRate);
            string bitrate = mbApiInterface.NowPlaying_GetFileProperty(FilePropertyType.Bitrate);
            string channels = mbApiInterface.NowPlaying_GetFileProperty(FilePropertyType.Channels);
            string properties = "";
            string nextSong = mbApiInterface.NowPlayingList_GetFileTag(mbApiInterface.NowPlayingList_GetNextIndex(1), MetaDataType.TrackTitle)
                + " by " +mbApiInterface.NowPlayingList_GetFileTag(mbApiInterface.NowPlayingList_GetNextIndex(1), MetaDataType.Artist);
            nextSong = nextSong == " by " || nextSong == null ? "End of List" : nextSong;

            if (filetype == "FLAC")
            {
                using (FlacFile file = new FlacFile(mbApiInterface.NowPlaying_GetFileUrl()))
                {
                    properties = filetype + " " + file.StreamInfo.BitsPerSample.ToString() + " bit, " + samplerate + ", " + bitrate + ", " + channels;
                }
            }
            else
            {
                properties = filetype + " " + samplerate + ", " + bitrate + ", " + channels;

            }


            string[] temp = null;
            mbApiInterface.NowPlayingList_QueryFilesEx("", ref temp);
            int size = temp.Count();

            try
            {
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

                    CustomData = new Dictionary<string, string>()
                    {
                       { "Properties", properties},
                       { "Position", (mbApiInterface.NowPlayingList_GetCurrentIndex()+1).ToString()+ " / " + size.ToString()},
                       { "Next", nextSong }

                    }

                }) ;

                filenameStack.Push(hashed.ToString());
                
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

        public async Task ProcessNextAndQueue(int currentPos)
        {
            var nextFileUrl = mbApiInterface.NowPlayingList_GetListFileUrl(currentPos+1);

            await CalculateHash(nextFileUrl, "next");

            var curr = songHash.Current;
            var ne = songHash.Next;

            string[] res = null;
            mbApiInterface.Library_GetFileTags(nextFileUrl, new[] { MetaDataType.Artist, MetaDataType.TrackTitle, MetaDataType.Album }, ref res);
            await QueueItem(songHash.Next,
                Path.GetExtension(nextFileUrl),
                0,
                res[0],
                res[1],
                res[2]
                );


            filenameStack.Push(songHash.Next);

            await CopySong(nextFileUrl, songHash.Next);
            natural = true;
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
            if (filenameStack.Count() > 1)
            {
                for (int i = 0; i < filenameStack.Count(); i++)
                {
                    var element = filenameStack.ElementAt(i);
                    if (element != songHash.Current)
                    {
                        DeleteFiles(element.ToString());
                        filenameStack.Remove(i);
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
            var index = mbApiInterface.NowPlayingList_GetCurrentIndex();
            ProcessNextAndQueue(index).WaitWithoutException();
            progressTimer.Enabled = false;
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
                    filenameStack.Push(songs[i].Hashed.ToString());
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
            if (mediaChannel != null && mediaChannel.Status != null)
            {
                mediaChannel.StopAsync();
            }
            ChromecastDisconnect(null, null);

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

        public void NewCurrent()
        {
            Current = Next;
            Next = null;
        }
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

    public sealed class Listener : IDisposable
    {
        public event EventHandler CountChanged;

        public int Duration { get; set; }
        public System.Timers.Timer Timer { get; set; }

        public int Count { get; set; }

        public Listener()
        {
            Timer = new System.Timers.Timer();
            Timer.Elapsed += new ElapsedEventHandler(Increment);
            Count = 1;
        }

        public void SetDuration(int Duration)
        {
            Timer.Interval = Duration / 10;
        }

        public void Enable()
        {
            AddListener();
            Timer.Enabled = false;
            Timer.Enabled = true;
        }

        public void Disable()
        {
            Timer.Enabled = false;
        }

        public void AddListener()
        {
            Timer.Elapsed += Increment;
        }

        public void RemoveListener()
        {
            Timer.Elapsed -= Increment;
        }


        public void Reset()
        {

        }

        private void Increment(object source, ElapsedEventArgs e)
        {
            Debug.WriteLine("Interval: " + Timer.Interval);
            Debug.WriteLine("Count: " + Count);
            Count++;
            if (Count == 8)
            {
                CountChanged?.Invoke(this, null);
                RemoveListener();
                Count = 1;
            }
        }

        public void Dispose()
        {
            CountChanged = null;
            Timer = null;

        }
    }

    public class SongInfo 
    { 
        public string Hashed { get; set; }
        public string SongFileExt { get; set; }
        public string FileURL { get; set; }
    }

}