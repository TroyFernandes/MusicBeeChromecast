using GoogleCast.Models.Media;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GoogleCast.Messages.Media
{
    /// <summary>
    /// A request to load and optionally start playback of a new ordered list of media items
    /// </summary>
    [DataContract]
    class QueueInsertMessage : QueueMessage
    {
        /// <summary>
        /// Gets or sets the array of items to insert into the queue. It is sorted (first element will be played first)
        /// </summary>
        /// <remarks>must not be null or empty</remarks>
        [DataMember(Name = "items")]
        public IEnumerable<QueueItem> Items { get; set; }
    }
}