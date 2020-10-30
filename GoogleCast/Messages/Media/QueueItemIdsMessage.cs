using System.Runtime.Serialization;

namespace GoogleCast.Messages.Media
{
    /// <summary>
    /// Message to retrieve all queued item ids
    /// </summary>
    [DataContract]
    [ReceptionMessage]
    class QueueItemIdsMessage : MessageWithId
    {
        /// <summary>
        /// Gets or sets the item id array
        /// </summary>        
        [DataMember(Name = "itemIds")]
        public int[] ItemIds { get; set; }
    }
}
