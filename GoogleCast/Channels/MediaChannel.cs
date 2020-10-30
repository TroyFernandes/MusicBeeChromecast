using GoogleCast.Messages;
using GoogleCast.Messages.Media;
using GoogleCast.Models.Media;
using GoogleCast.Models.Receiver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCast.Channels
{
    /// <summary>
    /// Media channel
    /// </summary>
    class MediaChannel : StatusChannel<IEnumerable<MediaStatus>, MediaStatusMessage>, IMediaChannel
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MediaChannel"/> class
        /// </summary>
        public MediaChannel() : base("media")
        {
        }

        private int[] ItemIds { get; set; }
        private QueueItem[] Items { get; set; }

        public QueueStatus QueueStatus { get; set; }

        public event EventHandler QueueStatusChanged;

        public event EventHandler NextRequested;
        public event EventHandler PreviousRequested;


        /// <summary>
        /// Gets the application identifier
        /// </summary>
        public string ApplicationId { get; } = "C202D291";

        private Task<Application> GetApplicationAsync()
        {
            return Sender.GetChannel<IReceiverChannel>().EnsureConnectionAsync(Namespace);
        }

        private Task<MediaStatus> SendAsync(MediaSessionMessage message, bool mediaSessionIdRequired = true)
        {
            var mediaSessionId = Status?.FirstOrDefault().MediaSessionId;
            if (mediaSessionIdRequired && mediaSessionId == null)
            {
                throw new ArgumentNullException("MediaSessionId");
            }
            message.MediaSessionId = mediaSessionId;
            return SendAsync((IMessageWithId)message);
        }

        private async Task<MediaStatus> SendAsync(IMessageWithId message)
        {
            try
            {
                return (await SendAsync<MediaStatusMessage>(message, (await GetApplicationAsync()).TransportId)).Status?.FirstOrDefault();
            }
            catch (Exception)
            {
                Status = null;
                throw;
            }
        }

        private async Task<int[]> SendAsync(QueueGetItemIdsMessage message)
        {
            var mediaSessionId = Status?.FirstOrDefault().MediaSessionId;
            message.MediaSessionId = mediaSessionId ?? throw new ArgumentNullException("MediaSessionId");
            await SendAsync<QueueItemIdsMessage>(message, (await GetApplicationAsync()).TransportId);
            return ItemIds;
        }

        private async Task<QueueItem[]> SendAsync(QueueGetItemsMessage message)
        {
            var mediaSessionId = Status?.FirstOrDefault().MediaSessionId;
            message.MediaSessionId = mediaSessionId ?? throw new ArgumentNullException("MediaSessionId");
            await SendAsync<QueueItemsMessage>(message, (await GetApplicationAsync()).TransportId);
            return Items;
        }

        /// <summary>
        /// Retrieves the status
        /// </summary>
        /// <returns>the status</returns>
        public Task<MediaStatus> GetStatusAsync()
        {
            return SendAsync(new GetStatusMessage() { MediaSessionId = Status?.FirstOrDefault().MediaSessionId }, false);
        }

        /// <summary>
        /// Loads a media
        /// </summary>
        /// <param name="media">media to load</param>
        /// <param name="autoPlay">true to play the media directly, false otherwise</param>
        /// <param name="activeTrackIds">track identifiers that should be active</param>
        /// <returns>media status</returns>
        public async Task<MediaStatus> LoadAsync(MediaInformation media, bool autoPlay = true, params int[] activeTrackIds)
        {
            return await SendAsync(new LoadMessage()
            {
                Media = media,
                AutoPlay = autoPlay,
                ActiveTrackIds = activeTrackIds,
                SessionId = (await GetApplicationAsync()).SessionId
            });
        }

        /// <summary>
        /// Loads a queue items
        /// </summary>
        /// <param name="repeatMode">queue repeat mode</param>
        /// <param name="medias">media items</param>
        /// <returns>media status</returns>
        public Task<MediaStatus> QueueLoadAsync(RepeatMode repeatMode, params MediaInformation[] medias)
        {
            return QueueLoadAsync(repeatMode, medias.Select(mi => new QueueItem() { Media = mi }));
        }

        /// <summary>
        /// Loads a queue items
        /// </summary>
        /// <param name="repeatMode">queue repeat mode</param>
        /// <param name="queueItems">items to load</param>
        /// <returns>media status</returns>
        public Task<MediaStatus> QueueLoadAsync(RepeatMode repeatMode, params QueueItem[] queueItems)
        {
            return QueueLoadAsync(repeatMode, queueItems as IEnumerable<QueueItem>);
        }

        /// <summary>
        /// Loads a queue items
        /// </summary>
        /// <param name="repeatMode">queue repeat mode</param>
        /// <param name="queueItems">items to load</param>
        /// <returns>media status</returns>
        private async Task<MediaStatus> QueueLoadAsync(RepeatMode repeatMode, IEnumerable<QueueItem> queueItems)
        {
            return await SendAsync(new QueueLoadMessage()
            {
                RepeatMode = repeatMode,
                Items = queueItems
            });
        }

        /// <summary>
        /// Inserts queue items into the queue
        /// </summary>
        /// <param name="queueItems">items to insert</param>
        /// <returns>media status</returns>
        public async Task<MediaStatus> QueueInsertAsync(QueueItem[] queueItems)
        {
            return await SendAsync(new QueueInsertMessage()
            {
                Items = queueItems
            });
        }

        /// <summary>
        /// Removes queue items from the queue
        /// </summary>
        /// <param name="queueItemIds">item ids to remove</param>
        /// <returns>media status</returns>
        public async Task<MediaStatus> QueueRemoveAsync(int[] queueItemIds)
        {
            return await SendAsync(new QueueRemoveMessage()
            {
                ItemIds = queueItemIds
            });
        }

        /// <summary>
        /// Updates the queue with new currently playing media and shuffle
        /// </summary>
        /// <param name="currentItemId">item id to set currently playing media</param>
        /// <param name="shuffle">bool </param>
        /// <returns>media status</returns>
        public async Task<MediaStatus> QueueUpdateAsync(int? currentItemId = null, bool? shuffle = null)
        {
            return await SendAsync(new QueueUpdateMessage()
            {
                CurrentItemId = currentItemId,
                Shuffle = shuffle
            });
        }

        /// <summary>
        /// Reorder queue items in the queue
        /// </summary>
        /// <param name="queueItemIds">item ids to reorder</param>
        /// <returns>media status</returns>
        public async Task<MediaStatus> QueueReorderAsync(int[] queueItemIds)
        {
            return await SendAsync(new QueueReorderMessage()
            {
                ItemIds = queueItemIds
            });
        }

        /// <summary>
        /// Get the given item ids' info
        /// </summary>
        /// <param name="itemIds">item ids to retrieve info</param>
        /// <returns>media status</returns>
        public async Task<QueueItem[]> QueueGetItemsMessage(int[] itemIds)
        {
            return await SendAsync(new QueueGetItemsMessage()
            {
                ItemIds = itemIds
            });
        }

        /// <summary>
        /// Get all currently queued item ids
        /// </summary>
        /// <returns>int array</returns>
        public async Task<int[]> QueueGetItemIdsMessage()
        {
            return await SendAsync(new QueueGetItemIdsMessage());
        }

        /// <summary>
        /// Edits tracks info
        /// </summary>
        /// <param name="enabledTextTracks">true to enable text tracks, false otherwise</param>
        /// <param name="language">language for the tracks that should be active</param>
        /// <param name="activeTrackIds">track identifiers that should be active</param>
        /// <returns>media status</returns>
        public async Task<MediaStatus> EditTracksInfoAsync(string language = null, bool enabledTextTracks = true, params int[] activeTrackIds)
        {
            return await SendAsync(new EditTracksInfoMessage()
            {
                Language = language,
                EnableTextTracks = enabledTextTracks,
                ActiveTrackIds = activeTrackIds
            });
        }

        /// <summary>
        /// Plays the media
        /// </summary>
        /// <returns>media status</returns>
        public async Task<MediaStatus> PlayAsync()
        {
            return await SendAsync(new PlayMessage());
        }

        /// <summary>
        /// Pauses the media
        /// </summary>
        /// <returns>media status</returns>
        public async Task<MediaStatus> PauseAsync()
        {
            return await SendAsync(new PauseMessage());
        }

        /// <summary>
        /// Stops the media
        /// </summary>
        /// <returns>media status</returns>
        public async Task<MediaStatus> StopAsync()
        {
            return await SendAsync(new StopMessage());
        }

        /// <summary>
        /// Seeks to the specified time
        /// </summary>
        /// <param name="seconds">time in seconds</param>
        /// <returns>media status</returns>
        public async Task<MediaStatus> SeekAsync(double seconds)
        {
            return await SendAsync(new SeekMessage() { CurrentTime = seconds });
        }

        /// <summary>
        /// Sets the current playback rate of the stream
        /// </summary>
        /// <param name="playbackRate">playback rate</param>
        /// <returns>media status</returns>
        public async Task<MediaStatus> SetPlaybackRateMessage(double playbackRate)
        {
            return await SendAsync(new SetPlaybackRateMessage() { PlaybackRate = playbackRate });
        }

        /// <summary>
        /// Skips to the next media in the queue
        /// </summary>
        /// <returns>media status</returns>
        public async Task<MediaStatus> NextAsync()
        {
            return await SendAsync(new QueueNextMessage());
        }


        /// <summary>
        /// Skips to the previous media in the queue
        /// </summary>
        /// <returns>media status</returns>
        public async Task<MediaStatus> PreviousAsync()
        {
            return await SendAsync(new QueuePrevMessage());
        }

        /// <summary>
        /// Called when a message for this channel is received
        /// </summary>
        /// <param name="message">message to process</param>
        public override Task OnMessageReceivedAsync(IMessage message)
        {
            switch (message)
            {
                case NextMessage nextMessage:
                    NextRequested?.Invoke(this, EventArgs.Empty);
                    break;

                case PreviousMessage prevMessage:
                    PreviousRequested?.Invoke(this, EventArgs.Empty);
                    break;

                case QueueItemIdsMessage itemIdsMessage:
                    ItemIds = itemIdsMessage.ItemIds;
                    break;

                case QueueItemsMessage itemsMessage:
                    Items = itemsMessage.Items;
                    break;

                case QueueChangeMessage changeMessage:
                    QueueStatus = new QueueStatus() { ChangeTypeString = changeMessage.ChangeType, ItemIds = changeMessage.ItemIds };
                    QueueStatusChanged?.Invoke(this, EventArgs.Empty);
                    break;
            }

            return base.OnMessageReceivedAsync(message);
        }

    }
}
