using GoogleCast.Models.Media;
using System.Runtime.Serialization;

namespace GoogleCast.Messages.Media
{
    /// <summary>
    /// Message to retrieve all changed queued item ids
    /// </summary>
    [DataContract]
    [ReceptionMessage]
    class PositionChangeMessage : MessageWithId
    {
        [DataMember(Name = "position")]
        public double Position { get; set; }

    }
}
