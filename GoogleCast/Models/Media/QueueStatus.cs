using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GoogleCast.Models.Media
{
    /// <summary>
    /// Queue status
    /// </summary>
    [DataContract]
    public class QueueStatus
    {
        /// <summary>
        /// Gets or sets the item id array
        /// </summary>        
        [DataMember(Name = "itemIds")]
        public int[] ItemIds { get; set; }

        /// <summary>
        /// Gets or sets the type of media artifact
        /// </summary>
        [IgnoreDataMember]
        public QueueChangeType ChangeType { get; set; }

        /// <summary>
        /// Gets or sets the item id array
        /// </summary>        
        [DataMember(Name = "changeType")]
        public string ChangeTypeString
        {
            get { return ChangeType.GetName(); }
            set { ChangeType = EnumHelper.Parse<QueueChangeType>(value); }
        }
    }
}
