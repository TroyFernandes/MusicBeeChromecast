using GoogleCast.Models.Media;
using System.Runtime.Serialization;

namespace GoogleCast.Messages.Media
{
    /// <summary>
    /// Message to retrieve all queued item ids
    /// </summary>
    [DataContract]
    [ReceptionMessage]
    class QueueItemsMessage : MessageWithId
    {
        /// <summary>
        /// Gets or sets the item id array
        /// </summary>        
        [DataMember(Name = "items")]
        public QueueItem[] Items { get; set; }
    }
}
