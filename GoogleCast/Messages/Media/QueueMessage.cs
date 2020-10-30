using System.Runtime.Serialization;

namespace GoogleCast.Messages.Media
{
    /// <summary>
    /// Queue message
    /// </summary>
    [DataContract]
    abstract class QueueMessage : MediaSessionMessage
    {
    }
}
