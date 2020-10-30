using System.Runtime.Serialization;

namespace GoogleCast.Messages.Media
{
    /// <summary>
    /// Message to skip to the next item in the queue
    /// </summary>
    [DataContract]
    class QueueNextMessage : MediaSessionMessage
    {
    }
}
