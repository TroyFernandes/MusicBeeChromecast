using GoogleCast.Messages.Media;
using System.Runtime.Serialization;

namespace GoogleCast.Messages.Media
{
    /// <summary>
    /// A request to retrieve all of the item ids currently in the queue
    /// </summary>
    [DataContract]
    class QueueGetItemIdsMessage : MediaSessionMessage
    {
    }
}
