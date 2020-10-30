using GoogleCast.Models.Media;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GoogleCast.Messages.Media
{
    /// <summary>
    /// A request to to retrieve the information of the given list of queue item ids
    /// </summary>
    [DataContract]
    class QueueGetItemsMessage : MediaSessionMessage
    {
        /// <summary>
        /// Gets or sets the array of item ids of which to retrieve the info 
        /// </summary>
        /// <remarks>must not be null or empty</remarks>
        [DataMember(Name = "itemIds")]
        public IEnumerable<int> ItemIds { get; set; }
    }
}
