using System.Runtime.Serialization;

namespace GoogleCast.Messages.Media
{
    /// <summary>
    /// Message to skip to the previous item in the queue
    /// </summary>
    [DataContract]
    class QueuePrevMessage : MediaSessionMessage
    {
    }
}
