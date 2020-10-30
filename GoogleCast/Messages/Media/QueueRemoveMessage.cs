using System.Runtime.Serialization;

namespace GoogleCast.Messages.Media
{
    /// <summary>
    /// Message to remove given queued item ids
    /// </summary>
    [DataContract]
    class QueueRemoveMessage : QueueMessage
    {
        /// <summary>
        /// Gets or sets the item id array
        /// </summary>        
        [DataMember(Name = "itemIds")]
        public int[] ItemIds { get; set; }
    }
}
