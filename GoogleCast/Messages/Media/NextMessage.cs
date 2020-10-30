using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace GoogleCast.Messages.Media
{    /// <summary>
     /// Chromecast Next Command
     /// </summary>
    [DataContract]
    [ReceptionMessage]
    class NextMessage : Message
    {
    }
}
