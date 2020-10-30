using GoogleCast.Models.Media;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCast.Channels
{
    /// <summary>
    /// Interface for the media channel
    /// </summary>
    public interface IMediaChannel : IStatusChannel<IEnumerable<MediaStatus>>, IApplicationChannel
    {
        /// <summary>
        /// Raised when the status has changed
        /// </summary>
        event EventHandler QueueStatusChanged;

        event EventHandler NextRequested;
        event EventHandler PreviousRequested;

        /// <summary>
        /// Gets the status
        /// </summary>
        QueueStatus QueueStatus { get; }

        /// <summary>
        /// Retrieves the media status
        /// </summary>
        /// <returns>the media status</returns>
        Task<MediaStatus> GetStatusAsync();

        /// <summary>
        /// Loads a media
        /// </summary>
        /// <param name="media">media to load</param>
        /// <param name="autoPlay">true to play the media directly, false otherwise</param>
        /// <param name="activeTrackIds">tracks identifiers that should be active</param>
        /// <returns>media status</returns>
        Task<MediaStatus> LoadAsync(MediaInformation media, bool autoPlay = true, params int[] activeTrackIds);

        /// <summary>
        /// Loads a queue items
        /// </summary>
        /// <param name="repeatMode">queue repeat mode</param>
        /// <param name="medias">media items</param>
        /// <returns>media status</returns>
        Task<MediaStatus> QueueLoadAsync(RepeatMode repeatMode, params MediaInformation[] medias);

        /// <summary>
        /// Loads a queue items
        /// </summary>
        /// <param name="repeatMode">queue repeat mode</param>
        /// <param name="queueItems">items to load</param>
        /// <returns>media status</returns>
        Task<MediaStatus> QueueLoadAsync(RepeatMode repeatMode, params QueueItem[] queueItems);

        /// <summary>
        /// Inserts queue items into the queue
        /// </summary>
        /// <param name="queueItems">items to insert</param>
        /// <returns>media status</returns>
        Task<MediaStatus> QueueInsertAsync(QueueItem[] queueItems);

        /// <summary>
        /// Removes queue items from the queue
        /// </summary>
        /// <param name="queueItemIds">item ids to remove</param>
        /// <returns>media status</returns>
        Task<MediaStatus> QueueRemoveAsync(int[] queueItemIds);

        /// <summary>
        /// Updates the queue with new currently playing media and shuffle
        /// </summary>
        /// <param name="currentItemId">item id to set currently playing media</param>
        /// <param name="shuffle">bool </param>
        /// <returns>media status</returns>
        Task<MediaStatus> QueueUpdateAsync(int? currentItemId = null, bool? shuffle = null);

        /// <summary>
        /// Reorders queue items in the queue
        /// </summary>
        /// <param name="queueItemIds">item ids to reorder</param>
        /// <returns>media status</returns>
        Task<MediaStatus> QueueReorderAsync(int[] queueItemIds);

        /// <summary>
        /// Get the given item ids' info
        /// </summary>
        /// <param name="itemIds">item ids to retrieve info</param>
        /// <returns>media status</returns>
        Task<QueueItem[]> QueueGetItemsMessage(int[] itemIds);

        /// <summary>
        /// Get all currently queued item ids
        /// </summary>
        /// <returns>int array</returns>
        Task<int[]> QueueGetItemIdsMessage();

        /// <summary>
        /// Edits tracks info
        /// </summary>
        /// <param name="enabledTextTracks">true to enable text tracks, false otherwise</param>
        /// <param name="language">language for the tracks that should be active</param>
        /// <param name="activeTrackIds">track identifiers that should be active</param>
        /// <returns>media status</returns>
        Task<MediaStatus> EditTracksInfoAsync(string language = null, bool enabledTextTracks = true, params int[] activeTrackIds);

        /// <summary>
        /// Plays the media
        /// </summary>
        /// <returns>media status</returns>
        Task<MediaStatus> PlayAsync();

        /// <summary>
        /// Pauses the media
        /// </summary>
        /// <returns>media status</returns>
        Task<MediaStatus> PauseAsync();

        /// <summary>
        /// Stops the media
        /// </summary>
        /// <returns>media status</returns>
        Task<MediaStatus> StopAsync();

        /// <summary>
        /// Seeks to the specified time
        /// </summary>
        /// <param name="seconds">time in seconds</param>
        /// <returns>media status</returns>
        Task<MediaStatus> SeekAsync(double seconds);

        /// <summary>
        /// Sets the current playback rate of the stream
        /// </summary>
        /// <param name="playbackRate">playback rate</param>
        /// <returns>media status</returns>
        Task<MediaStatus> SetPlaybackRateMessage(double playbackRate);        

        /// <summary>
        /// Skips to the next media in the queue
        /// </summary>
        /// <returns>media status</returns>
        Task<MediaStatus> NextAsync();

        /// <summary>
        /// Skips to the previous media in the queue
        /// </summary>
        /// <returns>media status</returns>
        Task<MediaStatus> PreviousAsync();
    }
}
