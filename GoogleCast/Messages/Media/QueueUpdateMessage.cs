using GoogleCast.Models.Media;
using System.Runtime.Serialization;

namespace GoogleCast.Messages.Media
{
    /// <summary>
    /// Message to update the queue
    /// </summary>
    [DataContract]
    class QueueUpdateMessage : MediaSessionMessage
    {
        /// <summary>
        /// Gets or sets the item id of the currently playing media
        /// </summary>        
        [DataMember(Name = "currentItemId")]
        public int? CurrentItemId { get; set; }

        /// <summary>
        /// Gets or sets the shuffle state
        /// </summary>        
        [DataMember(Name = "shuffle")]
        public bool? Shuffle { get; set; }        
    }
}
