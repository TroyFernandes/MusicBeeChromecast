using System.Runtime.Serialization;

namespace GoogleCast.Messages.Media
{
    /// <summary>
    /// Message to set the current playback rate of the stream
    /// </summary>
    [DataContract]
    class SetPlaybackRateMessage : MediaSessionMessage
    {
        /// <summary>
        /// Gets or sets the relative playback rate
        /// </summary>
        /// <remarks>the given playback will affect the current playback rate</remarks>
        [DataMember(Name = "playbackRate")]
        public double PlaybackRate { get; set; }
    }
}
