using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace GoogleCast.Messages.Media

{    /// <summary>
     /// Custom Message
     /// </summary>
    [DataContract]
    [ReceptionMessage]
    class PreviousMessage : Message
    {
    }
}
