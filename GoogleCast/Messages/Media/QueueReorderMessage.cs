using System.Runtime.Serialization;

namespace GoogleCast.Messages.Media
{
    /// <summary>
    /// Message to reorder the given queued item ids
    /// </summary>
    [DataContract]
    class QueueReorderMessage : QueueMessage
    {
        /// <summary>
        /// Gets or sets the item id array
        /// </summary>        
        [DataMember(Name = "itemIds")]
        public int[] ItemIds { get; set; }
    }
}
