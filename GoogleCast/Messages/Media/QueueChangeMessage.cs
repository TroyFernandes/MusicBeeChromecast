using GoogleCast.Models.Media;
using System.Runtime.Serialization;

namespace GoogleCast.Messages.Media
{
    /// <summary>
    /// Message to retrieve all changed queued item ids
    /// </summary>
    [DataContract]
    [ReceptionMessage]
    class QueueChangeMessage : MessageWithId
    {
        /// <summary>
        /// Gets or sets the item id array
        /// </summary>        
        [DataMember(Name = "itemIds")]
        public int[] ItemIds { get; set; }

        /// <summary>
        /// Gets or sets the item id array
        /// </summary>        
        [DataMember(Name = "changeType")]
        public string ChangeType { get; set; }        
    }
}
